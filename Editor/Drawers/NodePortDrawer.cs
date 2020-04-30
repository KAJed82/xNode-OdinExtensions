using System;

using Sirenix.OdinInspector.Editor;

using UnityEditor;

using UnityEngine;

using static XNode.Node;

namespace XNodeEditor.Odin
{
	public abstract class NodePortDrawer<T> : OdinValueDrawer<T>
	{
		protected sealed override bool CanDrawValueProperty( InspectorProperty property )
		{
			if ( !NodeEditor.InNodeEditor )
				return false;

			var parent = property.ParentValueProperty;
			if ( parent == null )
				parent = property.Tree.SecretRootProperty;

			if ( parent.ChildResolver is INodePortResolver )
			{
				var resolver = parent.ChildResolver as INodePortResolver;
				NodePortInfo portInfo = resolver.GetNodePortInfo( property.Info );
				if ( portInfo != null )
					return CanDrawNodePort( portInfo, property );

				return false;
			}

			return false;
		}

		protected virtual bool CanDrawNodePort( NodePortInfo nodePortInfo, InspectorProperty property )
		{
			return true;
		}

		protected bool isVisible = false;
		protected bool drawValue = true;

		protected sealed override void DrawPropertyLayout( GUIContent label )
		{
			// Unless someone tells me otherwise I will not draw root dynamic list ports as real things
			if ( !NodeEditor.InNodeEditor )
			{
				CallNextDrawer( label );
				return;
			}

			var nodeEditorWindow = NodeEditorWindow.current;
			if ( nodeEditorWindow == null )
				return;

			// I have to do more work than I used to
			var parent = Property.ParentValueProperty;
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;

			var resolver = parent.ChildResolver as INodePortResolver;
			var nodePortInfo = resolver.GetNodePortInfo( Property.Info );

			if ( Event.current.type == EventType.Layout )
			{
				switch ( nodePortInfo.ShowBackingValue )
				{
					case ShowBackingValue.Always:
						drawValue = true;
						break;

					case ShowBackingValue.Never:
						drawValue = false;
						break;

					case ShowBackingValue.Unconnected:
						drawValue = !nodePortInfo.Port.IsConnected;
						break;
				}

				isVisible = !nodePortInfo.Node.folded;
				isVisible |= nodePortInfo.ShowBackingValue == ShowBackingValue.Always;
				isVisible |= nodePortInfo.Port.IsDynamic; // Dynamics will be folded somewhere else
				isVisible |= nodePortInfo.Port.IsConnected;
			}

			if ( !isVisible )
				return;

			DrawPort( label, nodePortInfo, drawValue );
		}

		protected abstract void DrawPort( GUIContent label, NodePortInfo nodePortInfo, bool drawValue );
	}

	public abstract class NodePortAttributeDrawer<TAttribute> : OdinAttributeDrawer<TAttribute>
		where TAttribute : Attribute
	{
		protected sealed override bool CanDrawAttributeProperty( InspectorProperty property )
		{
			if ( !NodeEditor.InNodeEditor )
				return false;

			var parent = property.ParentValueProperty;
			if ( parent == null )
				parent = property.Tree.SecretRootProperty;

			if ( parent.ChildResolver is INodePortResolver )
			{
				var resolver = parent.ChildResolver as INodePortResolver;
				NodePortInfo portInfo = resolver.GetNodePortInfo( property.Info );
				if ( portInfo != null )
					return CanDrawNodePort( portInfo, property );

				return false;
			}

			return false;
		}

		protected virtual bool CanDrawNodePort( NodePortInfo nodePortInfo, InspectorProperty property )
		{
			return true;
		}

		protected bool isVisible = false;
		protected bool drawValue = true;

		protected sealed override void DrawPropertyLayout( GUIContent label )
		{
			// Unless someone tells me otherwise I will not draw root dynamic list ports as real things
			if ( !NodeEditor.InNodeEditor )
			{
				CallNextDrawer( label );
				return;
			}

			var nodeEditorWindow = NodeEditorWindow.current;
			if ( nodeEditorWindow == null )
				return;

			// I have to do more work than I used to
			var parent = Property.ParentValueProperty;
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;

			var resolver = parent.ChildResolver as INodePortResolver;
			var nodePortInfo = resolver.GetNodePortInfo( Property.Info );

			if ( Event.current.type == EventType.Layout )
			{
				switch ( nodePortInfo.ShowBackingValue )
				{
					case ShowBackingValue.Always:
						drawValue = true;
						break;

					case ShowBackingValue.Never:
						drawValue = false;
						break;

					case ShowBackingValue.Unconnected:
						drawValue = !nodePortInfo.Port.IsConnected;
						break;
				}

				isVisible = !nodePortInfo.Node.folded;
				isVisible |= nodePortInfo.ShowBackingValue == ShowBackingValue.Always;
				isVisible |= nodePortInfo.Port.IsDynamic; // Dynamics will be folded somewhere else
				isVisible |= nodePortInfo.Port.IsConnected;
			}

			if ( !isVisible )
				return;

			DrawPort( label, nodePortInfo, drawValue );
		}

		protected abstract void DrawPort( GUIContent label, NodePortInfo nodePortInfo, bool drawValue );
	}

	public abstract class NodePortAttributeDrawer<TAttribute, TValue> : OdinAttributeDrawer<TAttribute, TValue>
		where TAttribute : Attribute
	{
		protected sealed override bool CanDrawAttributeValueProperty( InspectorProperty property )
		{
			if ( !NodeEditor.InNodeEditor )
				return false;

			var parent = property.ParentValueProperty;
			if ( parent == null )
				parent = property.Tree.SecretRootProperty;

			if ( parent.ChildResolver is INodePortResolver )
			{
				var resolver = parent.ChildResolver as INodePortResolver;
				NodePortInfo portInfo = resolver.GetNodePortInfo( property.Info );
				if ( portInfo != null )
					return CanDrawNodePort( portInfo, property );

				return false;
			}

			return false;
		}
		protected virtual bool CanDrawNodePort( NodePortInfo nodePortInfo, InspectorProperty property )
		{
			return true;
		}

		protected bool isVisible = false;
		protected bool drawValue = true;

		protected sealed override void DrawPropertyLayout( GUIContent label )
		{
			// Unless someone tells me otherwise I will not draw root dynamic list ports as real things
			if ( !NodeEditor.InNodeEditor )
			{
				CallNextDrawer( label );
				return;
			}

			var nodeEditorWindow = NodeEditorWindow.current;
			if ( nodeEditorWindow == null )
				return;

			// I have to do more work than I used to
			var parent = Property.ParentValueProperty;
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;

			var resolver = parent.ChildResolver as INodePortResolver;
			var nodePortInfo = resolver.GetNodePortInfo( Property.Info );

			if ( Event.current.type == EventType.Layout )
			{
				switch ( nodePortInfo.ShowBackingValue )
				{
					case ShowBackingValue.Always:
						drawValue = true;
						break;

					case ShowBackingValue.Never:
						drawValue = false;
						break;

					case ShowBackingValue.Unconnected:
						drawValue = !nodePortInfo.Port.IsConnected;
						break;
				}

				isVisible = !nodePortInfo.Node.folded;
				isVisible |= nodePortInfo.ShowBackingValue == ShowBackingValue.Always;
				isVisible |= nodePortInfo.Port.IsDynamic; // Dynamics will be folded somewhere else
				isVisible |= nodePortInfo.Port.IsConnected;
			}

			if ( !isVisible )
				return;

			DrawPort( label, nodePortInfo, drawValue );
		}

		protected abstract void DrawPort( GUIContent label, NodePortInfo nodePortInfo, bool drawValue );
	}

	[DrawerPriority( 0, 0, 1000 )]
	public class DefaultNodePortDrawer<T> : NodePortDrawer<T>
	{
		protected override bool CanDrawNodePort( NodePortInfo nodePortInfo, InspectorProperty property )
		{
			return !nodePortInfo.IsDynamicPortList;
		}

		protected override void DrawPort( GUIContent label, NodePortInfo nodePortInfo, bool drawValue )
		{
			var nodeEditorWindow = NodeEditorWindow.current;
			if ( nodeEditorWindow == null )
				return;

			NodeEditor nodeEditor = NodeEditor.GetEditor( nodePortInfo.Port.node, nodeEditorWindow );

			using ( new EditorGUILayout.HorizontalScope() )
			{
				var portPosition = EditorGUILayout.GetControlRect( false, 0, GUILayout.Width( 0 ), GUILayout.Height( EditorGUIUtility.singleLineHeight ) );

				// Inputs go on the left, outputs on the right
				if ( nodePortInfo.Port.IsInput )
				{
					NodeEditorGUILayout.PortField(
						new Vector2( 0, portPosition.y ),
						 nodePortInfo.Port
					);
				}
				else
				{
					NodeEditorGUILayout.PortField(
						new Vector2( nodeEditor.GetWidth() - 16, portPosition.y ),
					 nodePortInfo.Port
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
				if ( nodePortInfo.Port.IsInput )
				{
					if ( drawLabel )
						EditorGUILayout.PrefixLabel( label, GUI.skin.label );

					if ( drawValue )
					{
						using ( new EditorGUILayout.VerticalScope() )
							CallNextDrawer( null );
					}

					if ( !drawValue || drawLabel && Property.Parent != null && Property.Parent.ChildResolver is GroupPropertyResolver )
						GUILayout.FlexibleSpace();
				}
				else
				{
					if ( !drawValue || drawLabel && Property.Parent != null && Property.Parent.ChildResolver is GroupPropertyResolver )
						GUILayout.FlexibleSpace();

					if ( drawValue )
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
