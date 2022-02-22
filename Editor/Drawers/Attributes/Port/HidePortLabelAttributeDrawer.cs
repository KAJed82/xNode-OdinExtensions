using UnityEngine;
using XNode.Odin;

namespace XNodeEditor.Odin
{
	[NodePortDrawerPriority( 2 )]
	public class HidePortLabelAttributeDrawer : NodePortAttributeDrawer<HidePortLabelAttribute>
	{
		protected override void DrawPort( GUIContent label )
		{
			this.CallNextDrawer( null );
		}
	}
}
