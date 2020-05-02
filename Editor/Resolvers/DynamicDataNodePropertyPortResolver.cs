using System.Collections.Generic;
using System.Linq;

using Sirenix.OdinInspector.Editor;

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

			if ( parent == null )
				parent = property.Tree.SecretRootProperty;

			var resolver = parent.ChildResolver as INodePortResolver;
			if ( resolver == null )
				return false;

			NodePortInfo portInfo = resolver.GetNodePortInfo( property.Name );
			return portInfo != null; // I am a port!
		}

		protected override bool AllowNullValues => true;

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
			// Port is already resolved for the base
			var parent = Property.ParentValueProperty;
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;

			portResolver = parent.ChildResolver as INodePortResolver;
			nodePortInfo = portResolver.GetNodePortInfo( Property.Name );

			noDataResolver = Property.ParentValueProperty == null ? null : parent.ChildResolver as IDynamicNoDataNodePropertyPortResolver;

			base.Initialize();

			UpdateDynamicPorts();
		}

		public bool AnyConnected => nameToNodePropertyInfo.Select( x => x.Value ).Any( x => x.Port == null || x.Port.IsConnected );

		public void UpdateDynamicPorts()
		{
			if ( noDataResolver != null )
				noDataResolver.UpdateDynamicPorts();
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

				// The port didn't exist... let's just make it exist again?
				if ( port == null )
				{
					if ( nodePortInfo.IsInput )
						port = node.AddDynamicInput( typeof( TElement ), nodePortInfo.ConnectionType, nodePortInfo.TypeConstraint, portName );
					else
						port = node.AddDynamicOutput( typeof( TElement ), nodePortInfo.ConnectionType, nodePortInfo.TypeConstraint, portName );

					UpdateDynamicPorts();
				}

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
			return base.GetChildCount( value );
		}

		public void RememberDynamicPort( NodePortInfo nodePortInfo )
		{
			throw new System.NotImplementedException();
		}

		public void ForgetDynamicPort( NodePortInfo nodePortInfo )
		{
			throw new System.NotImplementedException();
		}

		#region Collection Handlers
		protected override void Add( TList collection, object value )
		{
			int nextId = this.ChildCount;

			if ( nodePortInfo.Port.IsInput )
				nodePortInfo.Node.AddDynamicInput( typeof( TElement ), nodePortInfo.ConnectionType, nodePortInfo.TypeConstraint, string.Format( "{0} {1}", nodePortInfo.BaseFieldName, nextId ) );
			else
				nodePortInfo.Node.AddDynamicOutput( typeof( TElement ), nodePortInfo.ConnectionType, nodePortInfo.TypeConstraint, string.Format( "{0} {1}", nodePortInfo.BaseFieldName, nextId ) );

			UpdateDynamicPorts();

			lastRemovedConnections.Clear();

			if ( noDataResolver == null )
				base.Add( collection, value );
		}

		protected NodePort GetNodePort( int index )
		{
			NodePort port;
			if ( !childPortInfos.TryGetValue( index, out var kInfo ) || ( port = nameToNodePropertyInfo[kInfo.PropertyName].Port ) == null )
				return null;

			return port;
		}

		protected NodePortInfo GetNodePortInfo( int index )
		{
			if ( childPortInfos.TryGetValue( index, out var kInfo ) )
				return nameToNodePropertyInfo[kInfo.PropertyName];

			return null;
		}

		protected override void InsertAt( TList collection, int index, object value )
		{
			int nextId = this.ChildCount;

			// Remove happens before insert and we lose all the connections
			// Add a new port at the end
			if ( nodePortInfo.Port.IsInput )
				nodePortInfo.Node.AddDynamicInput( typeof( TElement ), nodePortInfo.ConnectionType, nodePortInfo.TypeConstraint, string.Format( "{0} {1}", nodePortInfo.BaseFieldName, nextId ) );
			else
				nodePortInfo.Node.AddDynamicOutput( typeof( TElement ), nodePortInfo.ConnectionType, nodePortInfo.TypeConstraint, string.Format( "{0} {1}", nodePortInfo.BaseFieldName, nextId ) );

			UpdateDynamicPorts();

			// Move everything down to make space - if something is missing just pretend we moved it?
			for ( int k = ChildCount - 1; k > index; --k )
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

			if ( noDataResolver == null )
				base.InsertAt( collection, index, value );
		}

		protected override void Remove( TList collection, object value )
		{
			int index = collection.IndexOf( (TElement)value );
			RemoveAt( collection, index );
			UpdateDynamicPorts();
		}

		protected List<NodePort> lastRemovedConnections = new List<NodePort>();

		protected override void RemoveAt( TList collection, int index )
		{
			NodePort indexPort = GetNodePort( index );

			if ( indexPort == null )
			{
				Debug.LogWarning( "No port found at index " + index + " - Restore" );
				var childPortInfo = GetNodePortInfo( index ); // Info still exists when this happens (probably)
				if ( childPortInfo.IsInput )
					childPortInfo.Node.AddDynamicInput( childPortInfo.Type, childPortInfo.ConnectionType, childPortInfo.TypeConstraint, childPortInfo.BaseFieldName );
				else
					childPortInfo.Node.AddDynamicOutput( childPortInfo.Type, childPortInfo.ConnectionType, childPortInfo.TypeConstraint, childPortInfo.BaseFieldName );
			}
			//else
			{
				lastRemovedConnections.Clear();
				if ( indexPort != null )
				{
					lastRemovedConnections.AddRange( indexPort.GetConnections() );

					// Clear the removed ports connections
					indexPort.ClearConnections();
				}

				// Move following connections one step up to replace the missing connection
				for ( int k = index + 1; k < ChildCount; k++ )
				{
					NodePort kPort = GetNodePort( k );
					if ( kPort == null )
						continue;

					for ( int j = 0; j < kPort.ConnectionCount; j++ )
					{
						NodePort other = kPort.GetConnection( j );
						kPort.Disconnect( other );

						NodePort k1Port = GetNodePort( k - 1 );
						if ( k1Port == null )
							continue;

						k1Port.Connect( other );
					}
				}

				// Remove the last dynamic port, to avoid messing up the indexing
				//NodePort lastPort = GetNodePort( ChildCount - 1 );
				InspectorPropertyInfo childPortInfo = null;
				NodePortInfo lastNodePortInfo = null;
				childPortInfos.TryGetValue( ChildCount - 1, out childPortInfo );
				if ( childPortInfo != null )
					nameToNodePropertyInfo.TryGetValue( childPortInfo.PropertyName, out lastNodePortInfo );

				if ( childPortInfo != null )
					childPortInfos.Remove( ChildCount - 1 );
				if ( lastNodePortInfo != null )
				{
					nameToNodePropertyInfo.Remove( childPortInfo.PropertyName );
					propertyToNodeProperty.Remove( lastNodePortInfo.SourcePropertyInfo.PropertyName );

					if ( lastNodePortInfo.Port != null )
						lastNodePortInfo.Node.RemoveDynamicPort( lastNodePortInfo.Port );
				}
			}

			UpdateDynamicPorts();

			if ( noDataResolver == null )
				base.RemoveAt( collection, index );
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

			UpdateDynamicPorts();

			if ( noDataResolver == null )
				base.Clear( collection );
		}
		#endregion
	}
}
