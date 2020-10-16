
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using XNode;
using static XNode.Node;

namespace XNodeEditor.Odin
{
	public static class NodePortDrawerHelper
	{
		public static void DisableDefaultPortDrawer( OdinDrawer drawer )
		{
			var defaultDrawer = drawer.Property.GetActiveDrawerChain().BakedDrawerArray.FirstOrDefault( x => x.GetType().ImplementsOpenGenericClass( typeof( DefaultNodePortDrawer<> ) ) );
			if ( defaultDrawer != null )
				defaultDrawer.SkipWhenDrawing = true;
		}

		public static void DisableDefaultPortDrawer<TValue>( OdinValueDrawer<TValue> drawer )
		{
			var defaultDrawer = drawer.Property.GetActiveDrawerChain().BakedDrawerArray.FirstOrDefault( x => x is DefaultNodePortDrawer<TValue> );
			if ( defaultDrawer != null )
				defaultDrawer.SkipWhenDrawing = true;
		}

		public static void DisableDefaultPortDrawer<TAttribute, TValue>( OdinAttributeDrawer<TAttribute, TValue> drawer ) where TAttribute : System.Attribute
		{
			var defaultDrawer = drawer.Property.GetActiveDrawerChain().BakedDrawerArray.FirstOrDefault( x => x is DefaultNodePortDrawer<TValue> );
			if ( defaultDrawer != null )
				defaultDrawer.SkipWhenDrawing = true;
		}

		public static void DrawPortHandle( NodePortInfo nodePortInfo, int overridePortHeight = -1 )
		{
			DrawPortHandle( nodePortInfo.Port, overridePortHeight );
		}

		public static void DrawPortHandle( NodePort port, int overridePortHeight = -1 )
		{
			var nodeEditorWindow = NodeEditorWindow.current;
			if ( nodeEditorWindow == null )
				return;

			NodeEditor nodeEditor = NodeEditor.GetEditor( port.node, nodeEditorWindow );
			var portPosition = EditorGUILayout.GetControlRect( false, 0, GUILayout.Width( 0 ), GUILayout.Height( overridePortHeight >= 0 ? overridePortHeight : EditorGUIUtility.singleLineHeight ) );

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

		public static bool DisplayMissingPort( InspectorProperty property, INodePortResolver resolver, NodePortInfo nodePortInfo )
		{
			if ( nodePortInfo == null )
			{
				SirenixEditorGUI.ErrorMessageBox( $"This info went missing. {property.Name}" );
				return true;
			}

			if ( nodePortInfo.Port == null )
			{
				using ( new EditorGUILayout.VerticalScope() )
				{
					SirenixEditorGUI.ErrorMessageBox( "This port went missing." );
					using ( new EditorGUILayout.HorizontalScope() )
					{
						if ( nodePortInfo.IsDynamic )
						{
							if ( GUILayout.Button( "Restore" ) )
								resolver.RememberDynamicPort( property );
							if ( GUILayout.Button( "Remove" ) )
								resolver.ForgetDynamicPort( property );
						}
						else
						{
							if ( GUILayout.Button( "Restore" ) )
								nodePortInfo.Node.UpdatePorts();
						}

					}
				}
				return true;
			}

			return false;
		}
	}
}
