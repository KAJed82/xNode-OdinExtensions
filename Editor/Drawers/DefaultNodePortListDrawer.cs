
using System.Collections.Generic;

using Sirenix.OdinInspector.Editor;

using UnityEngine;

using XNode;

namespace XNodeEditor.Odin
{
	[NodePortDrawerPriority( 1 )]
	public class DefaultNodePortListDrawer<T> : NodePortListDrawer<T>
	{
		protected override void DrawPortList( GUIContent label )
		{
			CallNextDrawer( label );
		}
	}
}
