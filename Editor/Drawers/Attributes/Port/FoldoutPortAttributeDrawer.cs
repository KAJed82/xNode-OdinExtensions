using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;
using XNode.Odin;

namespace XNodeEditor.Odin
{
	[NodePortAttributeDrawerPriority]
	public class FoldoutPortAttributeDrawer<T> : NodePortAttributeDrawer<FoldoutPortAttribute, T>
	{
		protected override void Initialize()
		{
			base.Initialize();

			NodePortDrawerHelper.DisableDefaultPortDrawer( this );
			isUnfolded = Property.Context.GetPersistent( this, nameof( isUnfolded ), GeneralDrawerConfig.Instance.ExpandFoldoutByDefault );
		}

		protected LocalPersistentContext<bool> isUnfolded;

		protected override void DrawPort( GUIContent label )
		{
			SirenixEditorGUI.BeginBox();
			SirenixEditorGUI.BeginBoxHeader();
			isUnfolded.Value = SirenixEditorGUI.Foldout( isUnfolded.Value, label == null ? GUIContent.none : label );
			NodePortDrawerHelper.DrawPortHandle( NodePortInfo );
			SirenixEditorGUI.EndBoxHeader();

			if ( SirenixEditorGUI.BeginFadeGroup( this, isUnfolded.Value ) )
				CallNextDrawer( null );
			SirenixEditorGUI.EndFadeGroup();

			SirenixEditorGUI.EndBox();
		}
	}
}
