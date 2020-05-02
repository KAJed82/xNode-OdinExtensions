
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

using static XNode.Node;

namespace XNodeEditor.Odin
{
	public static class NodePortDrawerHelper
	{
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
				NodePortInfo portInfo = resolver.GetNodePortInfo( property.Name );
				if ( portInfo != null )
					return ( portInfo.IsDynamic || !portInfo.IsDynamicPortList ) && CanDrawNodePort( resolver, portInfo, property );

				return false;
			}

			return false;
		}

		protected virtual bool CanDrawNodePort( INodePortResolver portResolver, NodePortInfo nodePortInfo, InspectorProperty property )
		{
			return true;
		}

		protected INodePortResolver PortResolver { get; private set; }
		protected NodePortInfo NodePortInfo { get; private set; }
		protected bool CanFold { get; private set; }
		protected bool DrawValue { get; private set; }

		protected override void Initialize()
		{
			var parent = Property.ParentValueProperty;
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;

			PortResolver = parent.ChildResolver as INodePortResolver;
			NodePortInfo = PortResolver.GetNodePortInfo( Property.Name );
			CanFold = Property.GetAttribute<DontFoldAttribute>() == null;
			DrawValue = true;
		}

		private bool isVisible = false;

		protected sealed override void DrawPropertyLayout( GUIContent label )
		{
			if ( NodePortDrawerHelper.DisplayMissingPort( Property, PortResolver, NodePortInfo ) )
				return;

			if ( Event.current.type == EventType.Layout )
			{
				switch ( NodePortInfo.ShowBackingValue )
				{
					case ShowBackingValue.Always:
						DrawValue = true;
						break;

					case ShowBackingValue.Never:
						DrawValue = false;
						break;

					case ShowBackingValue.Unconnected:
						DrawValue = !NodePortInfo.Port.IsConnected;
						break;
				}

				isVisible = !NodePortInfo.Node.folded;
				isVisible |= NodePortInfo.ShowBackingValue == ShowBackingValue.Always;
				isVisible |= NodePortInfo.Port.IsDynamic && PortResolver is IDynamicDataNodePropertyPortResolver; // Dynamics will be folded somewhere else
				isVisible |= NodePortInfo.Port.IsConnected;
				isVisible |= !CanFold;

				DrawValue &= NodePortInfo.HasValue;
			}

			if ( !isVisible )
				return;

			DrawPort( label );
		}

		protected abstract void DrawPort( GUIContent label );
	}
}
