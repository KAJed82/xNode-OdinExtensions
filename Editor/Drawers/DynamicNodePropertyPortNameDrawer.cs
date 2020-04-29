using Sirenix.OdinInspector.Editor;

using UnityEngine;

namespace XNodeEditor.Odin
{
	[DrawerPriority( 1.1, 0, 0 )]
	public class DynamicNodePropertyPortNameDrawer<T> : OdinValueDrawer<T>
	{
		protected override bool CanDrawValueProperty( InspectorProperty property )
		{
			if ( property.GetAttribute<HideConnectionLabelAttribute>() != null )
				return false;

			return property.Parent != null && property.Parent.ChildResolver is IDynamicPortListNodePropertyResolverWithPorts;
		}

		protected override void DrawPropertyLayout( GUIContent label )
		{
			if ( label == null )
			{
				// Maybe we don't want labels?
				if ( Property.Parent.Parent != null && Property.Parent.Parent.ChildResolver is IDynamicNoDataNodePropertyPortResolver )
					label = new GUIContent( $"{Property.Parent.Parent.NiceName} {Property.Index}" );
				else
					label = new GUIContent( $"{Property.Parent.NiceName} {Property.Index}" );
			}

			CallNextDrawer( label );
		}
	}
}
