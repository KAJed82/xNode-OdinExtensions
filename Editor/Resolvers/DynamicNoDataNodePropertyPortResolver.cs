
using System.Collections.Generic;
using System.Linq;

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;

using UnityEngine;

using XNode;

using static XNode.Node;

namespace XNodeEditor.Odin
{
	public interface IDynamicNoDataNodePropertyPortResolver : INodePortResolver
	{
		void UpdateDynamicPorts();
	}

	[ResolverPriority( 30 )]
	public class DynamicNoDataNodePropertyPortResolver<TValue> : OdinPropertyResolver<TValue>, IDynamicNoDataNodePropertyPortResolver
	{
		public override bool CanResolveForPropertyFilter( InspectorProperty property )
		{
			if ( !NodeEditor.InNodeEditor )
				return false;

			var parent = property.ParentValueProperty;
			if ( parent == null ) // Root only
			{
				parent = property.Tree.SecretRootProperty;

				var resolver = parent.ChildResolver as INodePortResolver;
				if ( resolver == null )
					return false;

				NodePortInfo portInfo = resolver.GetNodePortInfo( property.Info );
				return portInfo != null && portInfo.IsDynamicPortList && !typeof( TValue ).ImplementsOrInherits( typeof( System.Collections.IList ) );
			}

			return false;
		}

		protected override bool AllowNullValues => true;

		public Node Node => nodePortInfo.Node;

		protected INodePortResolver portResolver;
		protected NodePortInfo nodePortInfo;

		protected InspectorPropertyInfo fakeListInfo;
		protected List<TValue> dynamicPorts;

		protected List<TValue> GetDynamicPorts( ref TValue owner )
		{
			return dynamicPorts;
		}

		protected override void Initialize()
		{
			// Port is already resolved for the base
			var parent = Property.ParentValueProperty;
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;

			portResolver = parent.ChildResolver as INodePortResolver;
			nodePortInfo = portResolver.GetNodePortInfo( Property.Info );

			UpdateDynamicPorts();

			fakeListInfo = InspectorPropertyInfo.CreateValue(
				NodePropertyPort.NodePortListPropertyName,
				0,
				Property.ValueEntry.SerializationBackend,
				new GetterSetter<TValue, List<TValue>>(
				GetDynamicPorts,
				( ref TValue owner, List<TValue> value ) => { }
				),
				Property.Attributes
				.Where( x => !( x is PropertyGroupAttribute ) )
				.Where( x => !( x is InputAttribute ) )
				.Where( x => !( x is OutputAttribute ) )
			);
		}

		public void UpdateDynamicPorts()
		{
			if ( dynamicPorts == null )
				dynamicPorts = new List<TValue>();
			dynamicPorts.Clear();

			DynamicPortInfo dynamicPortInfo = DynamicPortHelper.GetDynamicPortData( nodePortInfo.Node, nodePortInfo.Port.fieldName );
			for ( int i = dynamicPortInfo.min; i <= dynamicPortInfo.max; ++i )
				dynamicPorts.Add( default( TValue ) );
		}

		public NodePortInfo GetNodePortInfo( NodePort port )
		{
			Debug.Assert( nodePortInfo.Port == port, "Ports are not equal, how?" );
			return nodePortInfo;
		}

		public NodePortInfo GetNodePortInfo( InspectorPropertyInfo sourceProperty )
		{
			return nodePortInfo;
		}

		public override int ChildNameToIndex( string name )
		{
			return 0;
		}

		public override InspectorPropertyInfo GetChildInfo( int childIndex )
		{
			return fakeListInfo;
		}

		protected override int GetChildCount( TValue value )
		{
			return 1;
		}
	}
}
