using Sirenix.Utilities.Editor;
using UnityEngine;
using XNode.Odin;

namespace XNodeEditor.Odin
{
	public class FoldoutAttributeDrawer<T> : NodePortAttributeDrawer<FoldoutPortAttribute, T>
	{
		protected override void Initialize()
		{
			base.Initialize();

			NodePortDrawerHelper.DisableDefaultPortDrawer( this );
		}

		protected bool isVisible;

		protected override void DrawPort( GUIContent label )
		{
			SirenixEditorGUI.BeginBox();
			SirenixEditorGUI.BeginBoxHeader();
			isVisible = SirenixEditorGUI.Foldout( isVisible, label == null ? GUIContent.none : label );
			NodePortDrawerHelper.DrawPortHandle( NodePortInfo );
			SirenixEditorGUI.EndBoxHeader();

			if ( SirenixEditorGUI.BeginFadeGroup( this, isVisible ) )
				CallNextDrawer( null );
			SirenixEditorGUI.EndFadeGroup();

			SirenixEditorGUI.EndBox();
		}
	}
}
