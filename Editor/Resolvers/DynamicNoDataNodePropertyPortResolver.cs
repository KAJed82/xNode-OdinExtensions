
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

		protected INodePortResolver portResolver;
		protected NodePortInfo nodePortInfo;

		protected InspectorPropertyInfo fakeListInfo;
		protected List<int> dynamicPorts;

		protected List<int> GetDynamicPorts( ref TValue owner ) => dynamicPorts;

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
				new GetterSetter<TValue, List<int>>(
				GetDynamicPorts,
				( ref TValue owner, List<int> value ) => { }
				)
				//, new ShowPropertyResolverAttribute()
			);
		}

		public void UpdateDynamicPorts()
		{
			if ( dynamicPorts == null )
				dynamicPorts = new List<int>();
			dynamicPorts.Clear();

			IEnumerable<NodePort> ports = Enumerable.Range( 0, int.MaxValue ).Select( x => nodePortInfo.Node.GetPort( $"{nodePortInfo.Port.fieldName} {x}" ) );
			foreach ( var port in ports )
			{
				if ( port == null ) // End on the first null port as well
					break;

				dynamicPorts.Add( default( int ) );
			}
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
