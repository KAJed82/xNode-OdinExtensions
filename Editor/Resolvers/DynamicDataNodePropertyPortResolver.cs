using System.Collections.Generic;
using System.Linq;

using Sirenix.OdinInspector.Editor;

using UnityEngine;

using XNode;

namespace XNodeEditor.Odin
{
	public interface IDynamicDataNodePropertyPortResolver : INodePortResolver
	{
		DynamicPortInfo DynamicPortInfo { get; }
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

			NodePortInfo portInfo = resolver.GetNodePortInfo( property.Info );
			return portInfo != null; // I am a port!
		}

		protected override bool AllowNullValues => true;

		public Node Node => nodePortInfo.Node;

		protected INodePortResolver portResolver;
		protected NodePortInfo nodePortInfo;

		protected IDynamicNoDataNodePropertyPortResolver noDataResolver;

		public DynamicPortInfo DynamicPortInfo { get; private set; }
		protected Dictionary<int, InspectorPropertyInfo> childPortInfos = new Dictionary<int, InspectorPropertyInfo>();

		protected Dictionary<InspectorPropertyInfo, NodePortInfo> propertyInfoToNodePropertyInfo = new Dictionary<InspectorPropertyInfo, NodePortInfo>();
		protected Dictionary<InspectorPropertyInfo, NodePortInfo> childInfoToNodePropertyInfo = new Dictionary<InspectorPropertyInfo, NodePortInfo>();

		protected override void Initialize()
		{
			// Port is already resolved for the base
			var parent = Property.ParentValueProperty;
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;

			portResolver = parent.ChildResolver as INodePortResolver;
			nodePortInfo = portResolver.GetNodePortInfo( Property.Info );

			noDataResolver = Property.ParentValueProperty == null ? null : parent.ChildResolver as IDynamicNoDataNodePropertyPortResolver;

			base.Initialize();

			UpdateDynamicPorts();
		}

		public void UpdateDynamicPorts()
		{
			DynamicPortInfo = DynamicPortHelper.GetDynamicPortData( nodePortInfo.Node, nodePortInfo.Port.fieldName );

			if ( noDataResolver != null )
				noDataResolver.UpdateDynamicPorts();
		}

		public override int ChildNameToIndex( string name )
		{
			if ( name.EndsWith( ":port" ) )
				return CollectionResolverUtilities.DefaultChildNameToIndex( name ) + DynamicPortInfo.ports.Count;

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

				var childNodePortInfo = new NodePortInfo(
					sourceChildInfo,
					portName,
					typeof(TElement),
					node, // Needed?
					nodePortInfo.ShowBackingValue,
					nodePortInfo.ConnectionType,
					nodePortInfo.TypeConstraint,
					nodePortInfo.IsDynamicPortList,
					true,
					nodePortInfo.IsInput,
					noDataResolver == null
				);

				childPortInfo = InspectorPropertyInfo.CreateValue(
					$"{CollectionResolverUtilities.DefaultIndexToChildName( index )}:port",
					0,
					Property.ValueEntry.SerializationBackend,
					new GetterSetter<TList, NodePort>(
						( ref TList owner ) => childNodePortInfo.Port,
						( ref TList owner, NodePort value ) => { }
					)
					, new HideInInspector()
				);

				propertyInfoToNodePropertyInfo[sourceChildInfo] = childNodePortInfo;
				childInfoToNodePropertyInfo[childPortInfo] = childNodePortInfo;

				childPortInfos[index] = childPortInfo;
			}
			return childPortInfo;
		}

		public override InspectorPropertyInfo GetChildInfo( int childIndex )
		{
			if ( childIndex >= DynamicPortInfo.ports.Count )
				return GetInfoForPortAtIndex( childIndex - DynamicPortInfo.ports.Count );

			return base.GetChildInfo( childIndex );
		}

		protected override int GetChildCount( TList value )
		{
			return base.GetChildCount( value );
		}

		public NodePortInfo GetNodePortInfo( InspectorPropertyInfo sourceProperty )
		{
			var index = CollectionResolverUtilities.DefaultChildNameToIndex( sourceProperty.PropertyName );
			var portInfo = GetInfoForPortAtIndex( index );
			if ( portInfo == null )
				return null;

			NodePortInfo nodePortInfo;
			propertyInfoToNodePropertyInfo.TryGetValue( sourceProperty, out nodePortInfo );
			return nodePortInfo;
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

			// Move everything down to make space
			for ( int k = DynamicPortInfo.ports.Count - 1; k > index; --k )
			{
				for ( int j = 0; j < DynamicPortInfo.ports[k - 1].ConnectionCount; j++ )
				{
					NodePort other = DynamicPortInfo.ports[k - 1].GetConnection( j );
					DynamicPortInfo.ports[k - 1].Disconnect( other );
					DynamicPortInfo.ports[k].Connect( other );
				}
			}

			// Let's just re-add connections to this node that were probably his
			foreach ( var c in lastRemovedConnections )
				DynamicPortInfo.ports[index].Connect( c );

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
			if ( DynamicPortInfo.ports[index] == null )
			{
				Debug.LogWarning( "No port found at index " + index + " - Skipped" );
			}
			else if ( DynamicPortInfo.ports.Count <= index )
			{
				Debug.LogWarning( "DynamicPorts[" + index + "] out of range. Length was " + DynamicPortInfo.ports.Count + " - Skipped" );
			}
			else
			{
				lastRemovedConnections.Clear();
				lastRemovedConnections.AddRange( DynamicPortInfo.ports[index].GetConnections() );

				// Clear the removed ports connections
				DynamicPortInfo.ports[index].ClearConnections();
				// Move following connections one step up to replace the missing connection
				for ( int k = index + 1; k < DynamicPortInfo.ports.Count; k++ )
				{
					for ( int j = 0; j < DynamicPortInfo.ports[k].ConnectionCount; j++ )
					{
						NodePort other = DynamicPortInfo.ports[k].GetConnection( j );
						DynamicPortInfo.ports[k].Disconnect( other );
						DynamicPortInfo.ports[k - 1].Connect( other );
					}
				}

				// Remove the last dynamic port, to avoid messing up the indexing
				nodePortInfo.Node.RemoveDynamicPort( DynamicPortInfo.ports[DynamicPortInfo.ports.Count() - 1].fieldName );
				UpdateDynamicPorts();
			}

			if ( noDataResolver == null )
				base.RemoveAt( collection, index );
		}

		protected override void Clear( TList collection )
		{
			foreach ( var port in DynamicPortInfo.ports )
				nodePortInfo.Node.RemoveDynamicPort( port );

			lastRemovedConnections.Clear();
			UpdateDynamicPorts();

			if ( noDataResolver == null )
				base.Clear( collection );
		}
		#endregion
	}
}
