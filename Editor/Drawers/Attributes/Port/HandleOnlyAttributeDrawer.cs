using UnityEngine;
using XNode.Odin;

namespace XNodeEditor.Odin
{
	[NodePortDrawerPriority( 1 )]
	public class HandleOnlyAttributeDrawer : NodePortAttributeDrawer<HandleOnlyAttribute>
	{
		protected override void DrawPort( GUIContent label )
		{
			NodePortDrawerHelper.DrawPortHandle( NodePortInfo, 0 );
		}
	}
}
