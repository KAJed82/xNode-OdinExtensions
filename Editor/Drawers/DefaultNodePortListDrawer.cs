
using System.Collections.Generic;

using Sirenix.OdinInspector.Editor;

using UnityEngine;

using XNode;

namespace XNodeEditor.Odin
{
	[DrawerPriority( 0, 1, 0 )]
	public class DefaultNodePortListDrawer<T> : NodePortListDrawer<T>
	{
		protected override void DrawPortList( GUIContent label, NodePortInfo nodePortInfo, List<NodePort> nodePorts )
		{
			CallNextDrawer( label );
		}
	}
}
