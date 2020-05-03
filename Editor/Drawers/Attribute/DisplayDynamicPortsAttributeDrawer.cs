using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using XNode.Odin;

namespace XNodeEditor.Odin
{
	[NodePortAttributeDrawerPriority]
	public class DisplayDynamicPortsAttributeDrawer : NodePortAttributeDrawer<DisplayDynamicPortsAttribute>
	{
		protected override bool CanDrawNodePort( INodePortResolver portResolver, NodePortInfo nodePortInfo, InspectorProperty property )
		{
			return property.GetAttribute<DisplayDynamicPortsAttribute>().ShowRemoveButton;
		}

		protected override void DrawPort( GUIContent label )
		{
			if ( IsVisible )
			{
				SirenixEditorGUI.BeginBox();
				if ( NodePortInfo.IsInput )
				{
					using ( new EditorGUILayout.HorizontalScope() )
					{
						CallNextDrawer( label );
						if ( GUILayout.Button( "Remove" ) )
							PortResolver.ForgetDynamicPort( Property );
					}
				}
				else
				{
					using ( new EditorGUILayout.HorizontalScope() )
					{
						if ( GUILayout.Button( "Remove" ) )
							PortResolver.ForgetDynamicPort( Property );
						CallNextDrawer( label );
					}
				}
				SirenixEditorGUI.EndBox();
			}
		}
	}
}
