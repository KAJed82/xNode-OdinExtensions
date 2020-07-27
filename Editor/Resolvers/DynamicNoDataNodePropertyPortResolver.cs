
using System.Collections.Generic;
using System.Linq;

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;

using UnityEngine;

using XNode;
using XNode.Odin;

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
#if ODIN_INSPECTOR_3
			if ( parent == null || parent == property.Tree.RootProperty ) // Root only
			{
				parent = property.Tree.RootProperty;
#else
			if ( parent == null ) // Root only
			{
				parent = property.Tree.SecretRootProperty;
#endif
				var resolver = parent.ChildResolver as INodePortResolver;
				if ( resolver == null )
					return false;

				NodePortInfo portInfo = resolver.GetNodePortInfo( property.Name );
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
#if ODIN_INSPECTOR_3
			if ( parent == null )
				parent = Property.Tree.RootProperty;
#else
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;
#endif

			portResolver = parent.ChildResolver as INodePortResolver;
			nodePortInfo = portResolver.GetNodePortInfo( Property.Name );

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
				.Where( x =>
					x is ListDrawerSettingsAttribute ||
					x is NodePortAttribute ||
					x is ShowDrawerChainAttribute ||
					x is ShowPropertyResolverAttribute
				)
				.ToArray()
			);
		}

		protected virtual TValue GenerateDefaultValue()
		{
			return default( TValue );
		}

		public void UpdateDynamicPorts()
		{
			if ( dynamicPorts == null )
				dynamicPorts = new List<TValue>();
			dynamicPorts.Clear();

			DynamicPortInfo dynamicPortInfo = DynamicPortHelper.GetDynamicPortData( nodePortInfo.Node, nodePortInfo.Port.fieldName );
			for ( int i = 0; i <= dynamicPortInfo.max; ++i )
				dynamicPorts.Add( GenerateDefaultValue() );
		}

		public NodePortInfo GetNodePortInfo( NodePort port )
		{
			Debug.Assert( nodePortInfo.Port == port, "Ports are not equal, how?" );
			return nodePortInfo;
		}

		public NodePortInfo GetNodePortInfo( string propertyName )
		{
			return nodePortInfo;
		}

		public void RememberDynamicPort( InspectorProperty property )
		{
			throw new System.NotImplementedException();
		}

		public void ForgetDynamicPort( InspectorProperty property )
		{
			throw new System.NotImplementedException();
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

	[ResolverPriority( 31 )]
	public class DynamicNoDataNodePropertyPortResolverForNullabel<TValue> : DynamicNoDataNodePropertyPortResolver<TValue> where TValue : new()
	{
		protected override TValue GenerateDefaultValue()
		{
			return new TValue();
		}
	}
}
