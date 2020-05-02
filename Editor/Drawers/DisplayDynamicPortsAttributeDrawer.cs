using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace XNodeEditor.Odin
{

	public class DisplayDynamicPortsAttributeDrawer : NodePortAttributeDrawer<DisplayDynamicPortsAttribute>
	{
		protected override bool CanDrawNodePort( NodePortInfo nodePortInfo, InspectorProperty property )
		{
			return property.GetAttribute< DisplayDynamicPortsAttribute>().ShowRemoveButton;
		}

		protected override void DrawPort( GUIContent label, INodePortResolver resolver, NodePortInfo nodePortInfo, bool drawValue )
		{
			SirenixEditorGUI.BeginBox();
			if ( nodePortInfo.IsInput )
			{
				using ( new EditorGUILayout.HorizontalScope() )
				{
					CallNextDrawer( label );
					if ( GUILayout.Button( "Remove" ) )
						resolver.ForgetDynamicPort( Property );
				}
			}
			else
			{
				using ( new EditorGUILayout.HorizontalScope() )
				{
					if ( GUILayout.Button( "Remove" ) )
						resolver.ForgetDynamicPort( Property );
					CallNextDrawer( label );
				}
			}
			SirenixEditorGUI.EndBox();
		}
	}
}
