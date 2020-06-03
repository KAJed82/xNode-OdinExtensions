
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

using UnityEngine;
using XNode.Odin;
using static XNode.Node;

namespace XNodeEditor.Odin
{
	[DrawerPriority( 0.99, 0, 0 )]
	public class DrawConnectionNameAttributeDrawer<T> : NodePortAttributeDrawer<DrawConnectionNameAttribute, T>
	{
		protected GUIContent connectionName = GUIContent.none;

		protected override void DrawPort( GUIContent label )
		{
			// Extra sanity checks
			if ( Event.current.type == EventType.Layout )
			{
				if ( ( NodePortInfo.ConnectionType == ConnectionType.Override || NodePortInfo.Port.ConnectionCount == 1 ) &&
						NodePortInfo.Port.IsConnected && NodePortInfo.Port.Connection != null && NodePortInfo.Port.Connection.node != null )
					connectionName = new GUIContent( NodePortInfo.Port.Connection.node.name );
				else
					connectionName = label;
			}

			CallNextDrawer( connectionName );
		}
	}
}
