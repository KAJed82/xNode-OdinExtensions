using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;
using XNode.Odin;

namespace XNodeEditor.Odin
{
	[DrawerPriority( 1.1, 0, 0 )]
	public class DynamicNodePropertyPortNameDrawer<T> : NodePortDrawer<T>
	{
		protected override bool CanDrawNodePort( INodePortResolver portResolver, NodePortInfo nodePortInfo, InspectorProperty property )
		{
			if ( !base.CanDrawNodePort( portResolver, nodePortInfo, property ) )
				return false;

			if ( property.GetAttribute<HideConnectionLabelAttribute>() != null )
				return false;

			return property.Parent != null && property.Parent.ChildResolver is IDynamicDataNodePropertyPortResolver;
		}

		protected override void DrawPort( GUIContent label )
		{
			if ( label == null )
			{
				// Maybe we don't want labels?
				var resolver = Property.Parent.ChildResolver as IDynamicDataNodePropertyPortResolver;
				var nodePortInfo = resolver.GetNodePortInfo( Property.Name );

				label = new GUIContent( $"{nodePortInfo.BaseFieldName}" );
			}

			CallNextDrawer( label );
		}
	}
}
