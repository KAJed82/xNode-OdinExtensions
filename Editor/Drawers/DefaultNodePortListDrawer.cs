
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using XNode;
using static XNode.Node;

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
