using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;

using UnityEngine;

using XNode;

using static XNode.Node;

namespace XNodeEditor.Odin
{
    public interface IDynamicDataNodePropertyPortResolver : INodePropertyPortResolver
    {
        string FieldName { get; }

        void UpdateDynamicPorts();

        List<NodePort> DynamicPorts { get; }
	}

	[ResolverPriority( 20 )]
	// I want this to pass through sometimes
	public class DynamicDataNodePropertyPortResolver<TList, TElement> : StrongListPropertyResolver<TList, TElement>, IDynamicDataNodePropertyPortResolver
		where TList : IList<TElement>
	{
		public override bool CanResolveForPropertyFilter( InspectorProperty property )
		{
			if ( !NodeEditor.InNodeEditor )
				return false;

			// Base rule to find input / outputs
			if ( property.ParentValueProperty == null && property.ParentType != null && property.ParentType.ImplementsOrInherits( typeof( Node ) ) ) // Base fields
			{
				var inputAttribute = property.GetAttribute<InputAttribute>();
				if ( inputAttribute != null )
					return inputAttribute.dynamicPortList && base.CanResolveForPropertyFilter( property );

				var outputAttribute = property.GetAttribute<OutputAttribute>();
				if ( outputAttribute != null )
					return outputAttribute.dynamicPortList && base.CanResolveForPropertyFilter( property );

				return false;
			}

			// Also allow no data list to pass through
			if ( property.ParentValueProperty != null && property.ParentValueProperty.ChildResolver is IDynamicNoDataNodePropertyPortResolver )
				return true;

			return false;
		}

		[MethodImpl( MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining )]
		protected override void Initialize()
		{
			Node = Property.Tree.WeakTargets.FirstOrDefault() as Node;
			// If this was provided by another no data set then let's do something different
			if ( Property.ParentValueProperty != null && Property.ParentValueProperty.ChildResolver is IDynamicNoDataNodePropertyPortResolver )
			{
				ParentNoDataResolver = ( Property.ParentValueProperty.ChildResolver as IDynamicNoDataNodePropertyPortResolver );
				FieldName = ParentNoDataResolver.FieldName;

				ShowBackingValue = ParentNoDataResolver.ShowBackingValue;
				ConnectionType = ParentNoDataResolver.ConnectionType;
				TypeConstraint = ParentNoDataResolver.TypeConstraint;
				IsDynamicPortList = true;
				IsInput = ParentNoDataResolver.IsInput;
			}
			else
			{
				FieldName = Property.Name;

				var inputAttribute = Property.GetAttribute<InputAttribute>();
				var outputAttribute = Property.GetAttribute<OutputAttribute>();
				if ( inputAttribute != null )
				{
					ShowBackingValue = inputAttribute.backingValue;
					ConnectionType = inputAttribute.connectionType;
					TypeConstraint = inputAttribute.typeConstraint;
					IsDynamicPortList = inputAttribute.dynamicPortList;
					IsInput = true;
				}
				else if ( outputAttribute != null )
				{
					ShowBackingValue = outputAttribute.backingValue;
					ConnectionType = outputAttribute.connectionType;
					TypeConstraint = outputAttribute.typeConstraint;
					IsDynamicPortList = outputAttribute.dynamicPortList;
					IsInput = false;
				}
			}

			Port = Node.GetPort( FieldName );

			UpdateDynamicPorts();

			PortInfo = InspectorPropertyInfo.CreateValue(
				NodePropertyPort.NodePortPropertyName,
				0,
				Property.ValueEntry.SerializationBackend,
				new GetterSetter<TList, NodePort>(
				( ref TList owner ) => Port,
				( ref TList owner, NodePort value ) => { }
				)
				// propagate attributes that arent input / output and groups
				, Property.Attributes
				.Where( x => !( x is InputAttribute ) )
				.Where( x => !( x is OutputAttribute ) )
				.Where( x => !( x is PropertyGroupAttribute ) )
			);

			base.Initialize();
		}

		public Node Node { get; private set; }
		public NodePort Port { get; private set; }

		public ShowBackingValue ShowBackingValue { get; private set; }
		public ConnectionType ConnectionType { get; private set; }
		public TypeConstraint TypeConstraint { get; private set; }
		public bool IsDynamicPortList { get; private set; }

		public bool IsInput { get; private set; }

		public string FieldName { get; private set; }

		public InspectorPropertyInfo PortInfo { get; private set; }

		public IDynamicNoDataNodePropertyPortResolver ParentNoDataResolver { get; private set; }

		public List<NodePort> DynamicPorts { get; private set; }

		public void UpdateDynamicPorts()
		{
			if ( DynamicPorts == null )
				DynamicPorts = new List<NodePort>();
			DynamicPorts.Clear();

			IEnumerable<NodePort> ports = Enumerable.Range( 0, int.MaxValue ).Select( x => Node.GetPort( $"{FieldName} {x}" ) );
			foreach ( var port in ports )
			{
				if ( port == null ) // End on the first null port as well
					break;

				DynamicPorts.Add( port );
			}

			if ( ParentNoDataResolver != null )
				ParentNoDataResolver.UpdateDynamicPorts();
		}

		public override int ChildNameToIndex( string name )
		{
			switch ( name )
			{
				case NodePropertyPort.NodePortPropertyName:
					return -1;
			}

			return base.ChildNameToIndex( name );
		}

		public override InspectorPropertyInfo GetChildInfo( int childIndex )
		{
			if ( childIndex < 0 )
				return PortInfo;

			return base.GetChildInfo( childIndex );
		}

		protected override int GetChildCount( TList value )
		{
			return base.GetChildCount( value );
		}

		#region Collection Handlers
		protected override void Add( TList collection, object value )
		{
			int nextId = this.ChildCount;

			if ( Port.IsInput )
				Node.AddDynamicInput( typeof( TElement ), ConnectionType, TypeConstraint, string.Format( "{0} {1}", FieldName, nextId ) );
			else
				Node.AddDynamicOutput( typeof( TElement ), ConnectionType, TypeConstraint, string.Format( "{0} {1}", FieldName, nextId ) );

			UpdateDynamicPorts();

			lastRemovedConnections.Clear();

			if ( ParentNoDataResolver == null )
				base.Add( collection, value );
		}

		protected override void InsertAt( TList collection, int index, object value )
		{
			int nextId = this.ChildCount;

			// Remove happens before insert and we lose all the connections
			// Add a new port at the end
			if ( Port.IsInput )
				Node.AddDynamicInput( typeof( TElement ), ConnectionType, TypeConstraint, string.Format( "{0} {1}", FieldName, nextId ) );
			else
				Node.AddDynamicOutput( typeof( TElement ), ConnectionType, TypeConstraint, string.Format( "{0} {1}", FieldName, nextId ) );

			UpdateDynamicPorts();

			// Move everything down to make space
			for ( int k = DynamicPorts.Count - 1; k > index; --k )
			{
				for ( int j = 0; j < DynamicPorts[k - 1].ConnectionCount; j++ )
				{
					NodePort other = DynamicPorts[k - 1].GetConnection( j );
					DynamicPorts[k - 1].Disconnect( other );
					DynamicPorts[k].Connect( other );
				}
			}

			// Let's just re-add connections to this node that were probably his
			foreach ( var c in lastRemovedConnections )
				DynamicPorts[index].Connect( c );

			lastRemovedConnections.Clear();

			if ( ParentNoDataResolver == null )
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
			if ( DynamicPorts[index] == null )
			{
				Debug.LogWarning( "No port found at index " + index + " - Skipped" );
			}
			else if ( DynamicPorts.Count <= index )
			{
				Debug.LogWarning( "DynamicPorts[" + index + "] out of range. Length was " + DynamicPorts.Count + " - Skipped" );
			}
			else
			{
				lastRemovedConnections.Clear();
				lastRemovedConnections.AddRange( DynamicPorts[index].GetConnections() );

				// Clear the removed ports connections
				DynamicPorts[index].ClearConnections();
				// Move following connections one step up to replace the missing connection
				for ( int k = index + 1; k < DynamicPorts.Count; k++ )
				{
					for ( int j = 0; j < DynamicPorts[k].ConnectionCount; j++ )
					{
						NodePort other = DynamicPorts[k].GetConnection( j );
						DynamicPorts[k].Disconnect( other );
						DynamicPorts[k - 1].Connect( other );
					}
				}

				// Remove the last dynamic port, to avoid messing up the indexing
				Node.RemoveDynamicPort( DynamicPorts[DynamicPorts.Count() - 1].fieldName );
				UpdateDynamicPorts();
			}

			if ( ParentNoDataResolver == null )
				base.RemoveAt( collection, index );
		}

		protected override void Clear( TList collection )
		{
			foreach ( var port in DynamicPorts )
				Node.RemoveDynamicPort( port );

			lastRemovedConnections.Clear();
			UpdateDynamicPorts();

			if ( ParentNoDataResolver == null )
				base.Clear( collection );
		}
		#endregion
	}
}
