using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;

using UnityEngine;

namespace XNodeEditor.Odin
{
	[DrawerPriority( 91, 0, 0 )]
	public class DynamicNoDataNodePropertyPortDrawer<T> : OdinValueDrawer<T>
	{
		protected override bool CanDrawValueProperty( InspectorProperty property )
		{
			return property.ChildResolver != null && property.ChildResolver.GetType().ImplementsOpenGenericClass( typeof( DynamicNoDataNodePropertyPortResolver<> ) );
		}

		protected override void DrawPropertyLayout( GUIContent label )
		{
			var portListProp = Property.Children[NodePropertyPort.NodePortListPropertyName];
			if ( portListProp != null )
				portListProp.Draw( label );
			else
				CallNextDrawer( label );
		}
	}
}
