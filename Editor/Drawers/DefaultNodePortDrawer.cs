
using Sirenix.OdinInspector.Editor;

using UnityEditor;

using UnityEngine;

namespace XNodeEditor.Odin
{
	[DrawerPriority( 0, 0, 10 )]
	public class DefaultNodePortDrawer<T> : NodePortDrawer<T>, IDefinesGenericMenuItems
	{
		protected override bool CanDrawNodePort( INodePortResolver portResolver, NodePortInfo nodePortInfo, InspectorProperty property )
		{
			// Don't draw ports for lists that are also ports
			if ( property.ChildResolver is ICollectionResolver )
			{
				if ( property.ChildResolver is IDynamicDataNodePropertyPortResolver )
					return false;
			}

			return true;
		}

		public void PopulateGenericMenu( InspectorProperty property, GenericMenu genericMenu )
		{

		}

		protected override void DrawPort( GUIContent label )
		{
			var nodeEditorWindow = NodeEditorWindow.current;
			if ( nodeEditorWindow == null )
				return;

			NodeEditor nodeEditor = NodeEditor.GetEditor( NodePortInfo.Port.node, nodeEditorWindow );

			using ( new EditorGUILayout.HorizontalScope() )
			{
				var portPosition = EditorGUILayout.GetControlRect( false, 0, GUILayout.Width( 0 ), GUILayout.Height( EditorGUIUtility.singleLineHeight ) );

				// Inputs go on the left, outputs on the right
				if ( NodePortInfo.Port.IsInput )
				{
					NodeEditorGUILayout.PortField(
						new Vector2( 0, portPosition.y ),
						 NodePortInfo.Port
					);
				}
				else
				{
					NodeEditorGUILayout.PortField(
						new Vector2( nodeEditor.GetWidth() - 16, portPosition.y ),
					 NodePortInfo.Port
					);
				}

				// Offset back to make up for the port draw
				GUILayout.Space( -4 );

				// Collections don't have the same kinds of labels
				if ( Property.ChildResolver is ICollectionResolver )
				{
					CallNextDrawer( label );
					return;
				}

				bool drawLabel = label != null && label != GUIContent.none;
				if ( NodePortInfo.Port.IsInput )
				{
					if ( drawLabel )
						EditorGUILayout.PrefixLabel( label, GUI.skin.label );

					if ( DrawValue )
					{
						using ( new EditorGUILayout.VerticalScope() )
							CallNextDrawer( null );
					}

					if ( !DrawValue || drawLabel && Property.Parent != null && Property.Parent.ChildResolver is GroupPropertyResolver )
						GUILayout.FlexibleSpace();
				}
				else
				{
					if ( !DrawValue || drawLabel && Property.Parent != null && Property.Parent.ChildResolver is GroupPropertyResolver )
						GUILayout.FlexibleSpace();

					if ( DrawValue )
					{
						using ( new EditorGUILayout.VerticalScope() )
							CallNextDrawer( null );
					}

					if ( drawLabel )
						EditorGUILayout.PrefixLabel( label, GUI.skin.label, NodeEditorResources.OutputPort );
				}

			}
		}
	}
}
