using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

using XNode;

namespace XNodeEditor.Odin
{
	public interface IDynamicDataNodePropertyPortResolver : INodePortResolver
	{
		//DynamicPortInfo DynamicPortInfo { get; }
		bool AnyConnected { get; }
	}

	[ResolverPriority( 20 )] // No data at 30
							 // I want this to pass through sometimes
	public class DynamicDataNodePropertyPortResolver<TList, TElement> : StrongListPropertyResolver<TList, TElement>, IDynamicDataNodePropertyPortResolver
		where TList : IList<TElement>
	{
		public override bool CanResolveForPropertyFilter( InspectorProperty property )
		{
			if ( !NodeEditor.InNodeEditor )
				return false;

			var parent = property.ParentValueProperty;
			if ( parent != null ) // Parent value property *should* only be valid for something living under the NoData dynamic list
			{
				if ( parent.ChildResolver is IDynamicNoDataNodePropertyPortResolver )
					return true;
			}

#if ODIN_INSPECTOR_3
			if ( parent == null )
				parent = property.Tree.RootProperty;
#else
			if ( parent == null )
				parent = property.Tree.SecretRootProperty;
#endif

			var resolver = parent.ChildResolver as INodePortResolver;
			if ( resolver == null )
				return false;

			NodePortInfo portInfo = resolver.GetNodePortInfo( property.Name );
			return portInfo != null; // I am a port!
		}

		protected override bool AllowNullValues => false;

		public Node Node => nodePortInfo.Node;

		protected INodePortResolver portResolver;
		protected NodePortInfo nodePortInfo;

		protected IDynamicNoDataNodePropertyPortResolver noDataResolver;

		//public DynamicPortInfo DynamicPortInfo { get; private set; }
		protected Dictionary<int, InspectorPropertyInfo> childPortInfos = new Dictionary<int, InspectorPropertyInfo>();

		protected Dictionary<string, NodePortInfo> nameToNodePropertyInfo = new Dictionary<string, NodePortInfo>();
		protected Dictionary<string, string> propertyToNodeProperty = new Dictionary<string, string>();

		public NodePortInfo GetNodePortInfo( string propertyName )
		{
			// Ensure the properties exist
			var index = CollectionResolverUtilities.DefaultChildNameToIndex( propertyName );
			var portInfo = GetInfoForPortAtIndex( index );
			if ( portInfo == null )
				return null;

			if ( propertyToNodeProperty.TryGetValue( propertyName, out var portPropertyName ) )
			{
				if ( nameToNodePropertyInfo.TryGetValue( portPropertyName, out var nodePortInfo ) )
				{
					return nodePortInfo;
				}
			}

			return null;
		}

		protected override void Initialize()
		{
			Property.ValueEntry.Update();

			// Port is already resolved for the base
			var parent = Property.ParentValueProperty;
#if ODIN_INSPECTOR_3
			if ( parent == null )
				parent = Property.Tree.RootProperty;
#else
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;
#endif

			portResolver = parent.ChildResolver as INodePortResolver;
			nodePortInfo = portResolver.GetNodePortInfo( Property.Name );

			noDataResolver = Property.ParentValueProperty == null ? null : parent.ChildResolver as IDynamicNoDataNodePropertyPortResolver;

			base.Initialize();

			for ( int i = 0; i < ChildCount; ++i )
			{
				var info = GetInfoForPortAtIndex( i );
			}
		}

		public bool AnyConnected
		{
			get
			{
				return nameToNodePropertyInfo.Select( x => x.Value ).Any( x => x.Port == null || x.Port.IsConnected );
			}
		}

		public override int ChildNameToIndex( string name )
		{
			if ( name.EndsWith( ":port" ) )
				return CollectionResolverUtilities.DefaultChildNameToIndex( name ) + base.ChildCount;

			return base.ChildNameToIndex( name );
		}

		protected InspectorPropertyInfo GetInfoForPortAtIndex( int index )
		{
			InspectorPropertyInfo childPortInfo;
			if ( !childPortInfos.TryGetValue( index, out childPortInfo ) )
			{
				InspectorPropertyInfo sourceChildInfo = base.GetChildInfo( index );

				string portName = $"{nodePortInfo.BaseFieldName} {index}";
				Node node = nodePortInfo.Node;
				NodePort port = node.GetPort( portName );

				childPortInfo = InspectorPropertyInfo.CreateValue(
					$"{CollectionResolverUtilities.DefaultIndexToChildName( index )}:port",
					0,
					Property.ValueEntry.SerializationBackend,
					new GetterSetter<TList, NodePort>(
						( ref TList owner ) => port,
						( ref TList owner, NodePort value ) => { }
					)
					, new HideInInspector()
				);

				var childNodePortInfo = new NodePortInfo(
					childPortInfo,
					sourceChildInfo,
					portName,
					typeof( TElement ),
					node, // Needed?
					nodePortInfo.ShowBackingValue,
					nodePortInfo.ConnectionType,
					nodePortInfo.TypeConstraint,
					nodePortInfo.IsDynamicPortList,
					true,
					nodePortInfo.IsInput,
					noDataResolver == null
				);

				propertyToNodeProperty[sourceChildInfo.PropertyName] = childPortInfo.PropertyName;
				nameToNodePropertyInfo[childPortInfo.PropertyName] = childNodePortInfo;

				childPortInfos[index] = childPortInfo;
			}
			return childPortInfo;
		}

		public override InspectorPropertyInfo GetChildInfo( int childIndex )
		{
			if ( childIndex >= base.ChildCount )
				return GetInfoForPortAtIndex( childIndex - base.ChildCount );

			return base.GetChildInfo( childIndex );
		}

		protected override int GetChildCount( TList value )
		{
			if ( value == null )
				return 0;
			return base.GetChildCount( value );
		}

		public void RememberDynamicPort( InspectorProperty property )
		{
			var nodePortInfo = GetNodePortInfo( property.Name );
			if ( nodePortInfo.IsInput )
				nodePortInfo.Node.AddDynamicInput( nodePortInfo.Type, nodePortInfo.ConnectionType, nodePortInfo.TypeConstraint, nodePortInfo.BaseFieldName );
			else
				nodePortInfo.Node.AddDynamicOutput( nodePortInfo.Type, nodePortInfo.ConnectionType, nodePortInfo.TypeConstraint, nodePortInfo.BaseFieldName );
		}

		public void ForgetDynamicPort( InspectorProperty property )
		{
			var nodePortInfo = GetNodePortInfo( property.Name );

			// What index does he think he is?
			int index = CollectionResolverUtilities.DefaultChildNameToIndex( property.Name );
			if ( index >= 0 && index < ChildCount )
			{
				var currentNodeWindow = NodeEditorWindow.current;
				EditorApplication.delayCall += () =>
				{
					RemoveAt( (TList)Property.ValueEntry.WeakValues[0], index ); // Note: This really shouldn't be index 0 but it's impossible to multi edit
					currentNodeWindow.Repaint();
				};
			}
		}

		#region Collection Handlers
		protected override void Add( TList collection, object value )
		{
			int nextId = this.ChildCount;

			if ( nodePortInfo.Port.IsInput )
				nodePortInfo.Node.AddDynamicInput( typeof( TElement ), nodePortInfo.ConnectionType, nodePortInfo.TypeConstraint, string.Format( "{0} {1}", nodePortInfo.BaseFieldName, nextId ) );
			else
				nodePortInfo.Node.AddDynamicOutput( typeof( TElement ), nodePortInfo.ConnectionType, nodePortInfo.TypeConstraint, string.Format( "{0} {1}", nodePortInfo.BaseFieldName, nextId ) );

			lastRemovedConnections.Clear();

			base.Add( collection, value );
		}

		protected NodePort GetNodePort( int index )
		{
			NodePortInfo nodePortInfo = GetNodePortInfo( index );
			if ( nodePortInfo != null )
				return nodePortInfo.Port;

			return null;
		}

		protected NodePortInfo GetNodePortInfo( int index )
		{
			if ( childPortInfos.TryGetValue( index, out var kInfo ) )
				return nameToNodePropertyInfo[kInfo.PropertyName];

			return null;
		}

		protected override void InsertAt( TList collection, int index, object value )
		{
			int newChildCount = this.ChildCount + 1;
			int nextId = this.ChildCount;

			// Remove happens before insert and we lose all the connections
			// Add a new port at the end
			if ( nodePortInfo.Port.IsInput )
				nodePortInfo.Node.AddDynamicInput( typeof( TElement ), nodePortInfo.ConnectionType, nodePortInfo.TypeConstraint, string.Format( "{0} {1}", nodePortInfo.BaseFieldName, nextId ) );
			else
				nodePortInfo.Node.AddDynamicOutput( typeof( TElement ), nodePortInfo.ConnectionType, nodePortInfo.TypeConstraint, string.Format( "{0} {1}", nodePortInfo.BaseFieldName, nextId ) );

			// Move everything down to make space - if something is missing just pretend we moved it?
			for ( int k = newChildCount - 1; k > index; --k )
			{
				NodePort k1Port = GetNodePort( k - 1 );
				if ( k1Port == null ) // It is missing, I have nothing to move
					continue;

				for ( int j = 0; j < k1Port.ConnectionCount; j++ )
				{
					NodePort other = k1Port.GetConnection( j );
					k1Port.Disconnect( other );

					NodePort kPort = GetNodePort( k );
					if ( kPort == null )
						continue;

					kPort.Connect( other );
				}
			}

			// Let's just re-add connections to this node that were probably his
			foreach ( var c in lastRemovedConnections )
			{
				NodePort indexPort = GetNodePort( index );
				if ( indexPort != null )
					indexPort.Connect( c );
			}

			lastRemovedConnections.Clear();

			//if ( noDataResolver == null )
			base.InsertAt( collection, index, value );

			this.ForceUpdateChildCount();
		}

		protected override void Remove( TList collection, object value )
		{
			int index = collection.IndexOf( (TElement)value );
			RemoveAt( collection, index );
		}

		protected NodePort ReplaceNodeForRemove( int index )
		{
			var childPortInfo = GetNodePortInfo( index ); // Info still exists when this happens (probably)
			if ( childPortInfo.IsInput )
				return childPortInfo.Node.AddDynamicInput( childPortInfo.Type, childPortInfo.ConnectionType, childPortInfo.TypeConstraint, childPortInfo.BaseFieldName );
			else
				return childPortInfo.Node.AddDynamicOutput( childPortInfo.Type, childPortInfo.ConnectionType, childPortInfo.TypeConstraint, childPortInfo.BaseFieldName );
		}

		protected List<NodePort> lastRemovedConnections = new List<NodePort>();

		protected override void RemoveAt( TList collection, int index )
		{
			NodePort indexPort = GetNodePort( index );

			if ( indexPort == null )
			{
				Debug.LogWarning( $"No port found at index {index}" );
			}

			lastRemovedConnections.Clear();
			if ( indexPort != null )
			{
				lastRemovedConnections.AddRange( indexPort.GetConnections() );

				// Clear the removed ports connections
				indexPort.ClearConnections();
			}

			// Cache the last port because I'm about to remove it
			NodePort lastPort = GetNodePort( ChildCount - 1 );

			// Move following connections one step up to replace the missing connection
			for ( int k = index + 1; k < ChildCount; k++ )
			{
				NodePort kPort = GetNodePort( k );
				if ( kPort == null )
					continue;

				NodePort k1Port = GetNodePort( k - 1 );
				// Port k - 1 missing means I need to actually rename a port instead
				// Create k -1, add all the correct connections ... leave K alone because he existed
				if ( k1Port == null )
					k1Port = ReplaceNodeForRemove( k - 1 );

				for ( int j = 0; j < kPort.ConnectionCount; j++ )
				{
					NodePort other = kPort.GetConnection( j );
					kPort.Disconnect( other );

					k1Port.Connect( other );
				}
			}

			// Remove the last dynamic port, to avoid messing up the indexing
			if ( lastPort != null )
				lastPort.node.RemoveDynamicPort( lastPort );

			base.RemoveAt( collection, index );

			this.ForceUpdateChildCount();
		}

		protected override void Clear( TList collection )
		{
			for ( int i = 0; i < ChildCount; ++i )
			{
				NodePort port = GetNodePort( i );
				if ( port != null )
					nodePortInfo.Node.RemoveDynamicPort( port );
			}

			lastRemovedConnections.Clear();

			base.Clear( collection );
		}
#endregion
	}
}
