using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEngine;

using XNode;

using static XNode.Node;

namespace XNodeEditor.Odin
{
	public interface IDynamicDataNodePropertyPortResolver : INodePortResolver
	{
		//string FieldName { get; }

		//void UpdateDynamicPorts();

		//List<NodePort> DynamicPorts { get; }
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

		protected INodePortResolver portResolver;
		protected NodePortInfo nodePortInfo;

		protected IDynamicNoDataNodePropertyPortResolver noDataResolver;

		protected List<NodePort> dynamicPorts;
		protected Dictionary<int, InspectorPropertyInfo> childPortInfos = new Dictionary<int, InspectorPropertyInfo>();

		protected Dictionary<InspectorPropertyInfo, NodePortInfo> propertyInfoToNodePropertyInfo = new Dictionary<InspectorPropertyInfo, NodePortInfo>();
		protected Dictionary<NodePort, NodePortInfo> nodePortToNodePortInfo = new Dictionary<NodePort, NodePortInfo>();

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
			if ( dynamicPorts == null )
				dynamicPorts = new List<NodePort>();
			dynamicPorts.Clear();

			IEnumerable<NodePort> ports = Enumerable.Range( 0, int.MaxValue ).Select( x => nodePortInfo.Node.GetPort( $"{nodePortInfo.BaseFieldName} {x}" ) );
			foreach ( var port in ports )
			{
				if ( port == null ) // End on the first null port as well
					break;

				dynamicPorts.Add( port );
			}

			if ( noDataResolver != null )
				noDataResolver.UpdateDynamicPorts();
		}

		public override int ChildNameToIndex( string name )
		{
			if ( name.EndsWith( ":port" ) )
			{
				Debug.Log( "PORT" );
				return CollectionResolverUtilities.DefaultChildNameToIndex( name ) + dynamicPorts.Count;
			}

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

				var childNodePortInfo = new NodePortInfo(
					sourceChildInfo,
					portName,
					node, // Needed?
					port,
					nodePortInfo.ShowBackingValue,
					nodePortInfo.ConnectionType,
					nodePortInfo.TypeConstraint,
					nodePortInfo.IsDynamicPortList,
					nodePortInfo.IsInput
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
				nodePortToNodePortInfo[port] = childNodePortInfo;

				childPortInfos[index] = childPortInfo;
			}
			return childPortInfo;
		}

		public override InspectorPropertyInfo GetChildInfo( int childIndex )
		{
			if ( childIndex >= dynamicPorts.Count )
				return GetInfoForPortAtIndex( childIndex - dynamicPorts.Count );

			return base.GetChildInfo( childIndex );
		}

		protected override int GetChildCount( TList value )
		{
			return base.GetChildCount( value );
		}

		public NodePortInfo GetNodePortInfo( NodePort port )
		{
			throw new NotImplementedException();
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
			for ( int k = dynamicPorts.Count - 1; k > index; --k )
			{
				for ( int j = 0; j < dynamicPorts[k - 1].ConnectionCount; j++ )
				{
					NodePort other = dynamicPorts[k - 1].GetConnection( j );
					dynamicPorts[k - 1].Disconnect( other );
					dynamicPorts[k].Connect( other );
				}
			}

			// Let's just re-add connections to this node that were probably his
			foreach ( var c in lastRemovedConnections )
				dynamicPorts[index].Connect( c );

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
			if ( dynamicPorts[index] == null )
			{
				Debug.LogWarning( "No port found at index " + index + " - Skipped" );
			}
			else if ( dynamicPorts.Count <= index )
			{
				Debug.LogWarning( "DynamicPorts[" + index + "] out of range. Length was " + dynamicPorts.Count + " - Skipped" );
			}
			else
			{
				lastRemovedConnections.Clear();
				lastRemovedConnections.AddRange( dynamicPorts[index].GetConnections() );

				// Clear the removed ports connections
				dynamicPorts[index].ClearConnections();
				// Move following connections one step up to replace the missing connection
				for ( int k = index + 1; k < dynamicPorts.Count; k++ )
				{
					for ( int j = 0; j < dynamicPorts[k].ConnectionCount; j++ )
					{
						NodePort other = dynamicPorts[k].GetConnection( j );
						dynamicPorts[k].Disconnect( other );
						dynamicPorts[k - 1].Connect( other );
					}
				}

				// Remove the last dynamic port, to avoid messing up the indexing
				nodePortInfo.Node.RemoveDynamicPort( dynamicPorts[dynamicPorts.Count() - 1].fieldName );
				UpdateDynamicPorts();
			}

			if ( noDataResolver == null )
				base.RemoveAt( collection, index );
		}

		protected override void Clear( TList collection )
		{
			foreach ( var port in dynamicPorts )
				nodePortInfo.Node.RemoveDynamicPort( port );

			lastRemovedConnections.Clear();
			UpdateDynamicPorts();

			if ( noDataResolver == null )
				base.Clear( collection );
		}
		#endregion
	}

	//[DrawerPriority(1,0,0)]
	//public class DynamicPortDrawer<T> : OdinValueDrawer<T>
	//{
	//	protected override bool CanDrawValueProperty( InspectorProperty property )
	//	{
	//		return property.Parent != null && property.Parent.ChildResolver is IDynamicDataNodePropertyPortResolver;
	//	}

	//	protected override void DrawPropertyLayout( GUIContent label )
	//	{
	//		SirenixEditorGUI.MessageBox( Property.Name );
	//		if ( Property.Parent != null )
	//			SirenixEditorGUI.MessageBox( Property.Parent.ChildResolver.ToString() );

	//		var resolver = Property.Parent.ChildResolver as IDynamicDataNodePropertyPortResolver;
	//		var nodePortInfo = resolver.GetNodePortInfo( Property.Info );
	//		if ( nodePortInfo != null )
	//			SirenixEditorGUI.MessageBox( $"Port: {nodePortInfo.BaseFieldName} {nodePortInfo.Port.ValueType.Name}" );

	//		CallNextDrawer( label );
	//	}
	//}

	//[ResolverPriority( 10 )]
	//public class DynamicPortPropertyResolver<T> : NodePropertyPortResolver<T>
	//{
	//	public override bool CanResolveForPropertyFilter( InspectorProperty property )
	//	{
	//		return property.Parent != null && property.Parent.ChildResolver is IDynamicDataNodePropertyPortResolver;
	//	}
	//}
}
