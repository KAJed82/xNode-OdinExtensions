using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using XNode.Odin;

namespace XNodeEditor.Odin
{
	public class BoxedAttributeDrawer<T> : NodePortAttributeDrawer<BoxedPortAttribute, T>
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

			CallNextDrawer( null );

			SirenixEditorGUI.EndBox();
		}
	}
}
