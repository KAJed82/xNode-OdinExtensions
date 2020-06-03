using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using XNode.Odin;

namespace XNodeEditor.Odin
{
	[NodePortAttributeDrawerPriority]
	public class BoxedPortAttributeDrawer<T> : NodePortAttributeDrawer<BoxedPortAttribute, T>
	{
		protected override void Initialize()
		{
			base.Initialize();

			NodePortDrawerHelper.DisableDefaultPortDrawer( this );
		}

		protected override void DrawPort( GUIContent label )
		{
			SirenixEditorGUI.BeginBox();
			SirenixEditorGUI.BeginBoxHeader();
			if ( label != null )
				EditorGUILayout.LabelField( label );
			NodePortDrawerHelper.DrawPortHandle( NodePortInfo );
			SirenixEditorGUI.EndBoxHeader();

			if ( DrawValue )
				CallNextDrawer( null );
			else
				GUILayout.Space( -3.5f );

			SirenixEditorGUI.EndBox();
		}
	}
}
