using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using XNode;

namespace XNodeEditor.Odin
{
    [DrawerPriority( 0, 0, 1000 )]
    public class NodePortDrawer : OdinValueDrawer<NodePort>
    {
        protected override void DrawPropertyLayout( GUIContent label )
        {
            // Unless someone tells me otherwise I will not draw root dynamic list ports as real things
            if ( !NodeEditor.InNodeEditor )
            {
                CallNextDrawer( label );
                return;
            }

            if ( label != null )
                return;

            var nodeEditorWindow = GUIHelper.CurrentWindow as NodeEditorWindow;
            if ( nodeEditorWindow == null )
                return;

            var port = ValueEntry.SmartValue;
            if ( port == null || port.node == null )
                return;

            var portPosition = EditorGUILayout.GetControlRect( false, 0, GUILayout.Width( 0 ), GUILayout.Height( EditorGUIUtility.singleLineHeight ) );

            NodeEditor nodeEditor = NodeEditor.GetEditor( port.node, nodeEditorWindow );

            // Inputs go on the left, outputs on the right
            if ( port.IsInput )
            {
                NodeEditorGUILayout.PortField(
                    new Vector2( 0, portPosition.y ),
                    port
                );
            }
            else
            {
                NodeEditorGUILayout.PortField(
                    new Vector2( nodeEditor.GetWidth() - 16, portPosition.y ),
                    port
                );
            }
        }
    }
}