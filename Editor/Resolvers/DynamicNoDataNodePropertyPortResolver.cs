
using System.Collections.Generic;
using System.Linq;

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;

using XNode;

using static XNode.Node;

namespace XNodeEditor.Odin
{
	public interface IDynamicNoDataNodePropertyPortResolver : INodePropertyPortResolver
	{
		string FieldName { get; }

		void UpdateDynamicPorts();
	}

	[OdinDontRegister]
	[ResolverPriority( 15 )]
	public class DynamicNoDataNodePropertyPortResolver<TValue> : OdinPropertyResolver<TValue>, IDynamicNoDataNodePropertyPortResolver
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
					return inputAttribute.dynamicPortList && !property.ValueEntry.TypeOfValue.ImplementsOpenGenericInterface( typeof( IList<> ) );

				var outputAttribute = property.GetAttribute<OutputAttribute>();
				if ( outputAttribute != null )
					return outputAttribute.dynamicPortList && !property.ValueEntry.TypeOfValue.ImplementsOpenGenericInterface( typeof( IList<> ) );

				return false;
			}

			return false;
		}

		public Node Node { get; private set; }
		public List<int> dynamicPorts { get; private set; }

		public NodePort Port { get; private set; }
		public ShowBackingValue ShowBackingValue { get; private set; }
		public ConnectionType ConnectionType { get; private set; }
		public TypeConstraint TypeConstraint { get; private set; }
		public bool IsDynamicPortList { get; private set; }

		public bool IsInput { get; private set; }

		public string FieldName { get; private set; }

		public InspectorPropertyInfo PortInfo { get; private set; }

		public void UpdateDynamicPorts()
		{
			if ( dynamicPorts == null )
				dynamicPorts = new List<int>();
			dynamicPorts.Clear();

			IEnumerable<NodePort> ports = Enumerable.Range( 0, int.MaxValue ).Select( x => Node.GetPort( $"{Property.Name} {x}" ) );
			foreach ( var port in ports )
			{
				if ( port == null ) // End on the first null port as well
					break;

				dynamicPorts.Add( default( int ) );
			}
		}

		protected override void Initialize()
		{
			Node = Property.Tree.WeakTargets.FirstOrDefault() as Node;
			FieldName = Property.Name;
			Port = Node.GetPort( FieldName );

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

			ShowBackingValue = ShowBackingValue.Never;

			UpdateDynamicPorts();

			PortInfo = InspectorPropertyInfo.CreateValue
			(
				NodePropertyPort.NodePortListPropertyName,
				0,
				Property.ValueEntry.SerializationBackend,
				new GetterSetter<TValue, List<int>>(
					() => dynamicPorts,
					( List<int> ports ) => { }
				)
				, Property.Attributes
				.Where( x => !( x is InputAttribute ) )
				.Where( x => !( x is OutputAttribute ) )
				.Where( x => !( x is PropertyGroupAttribute ) )
			);

			var attributes = PortInfo.GetEditableAttributesList();
			var listDrawerAttributes = attributes.GetAttribute<ListDrawerSettingsAttribute>();
			if ( listDrawerAttributes == null )
			{
				listDrawerAttributes = new ListDrawerSettingsAttribute();
				attributes.Add( listDrawerAttributes );
			}

			listDrawerAttributes.AlwaysAddDefaultValue = true;
			listDrawerAttributes.Expanded = true;
			listDrawerAttributes.ShowPaging = false;

			base.Initialize();
		}

		public override int ChildNameToIndex( string name )
		{
			return 0;
		}

		public override InspectorPropertyInfo GetChildInfo( int childIndex )
		{
			return PortInfo;
		}

		protected override int GetChildCount( TValue value )
		{
			return 1;
		}
	}
}
