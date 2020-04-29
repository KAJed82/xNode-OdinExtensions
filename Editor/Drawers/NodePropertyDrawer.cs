using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

using UnityEditor;

using UnityEngine;

using static XNode.Node;

namespace XNodeEditor.Odin
{
	[DrawerPriority( 1, 0, 0 )]
	public class NodePropertyDrawer<T> : OdinValueDrawer<T>
	{
		private static GUIStyle s_RightAlignStyle;
		public static GUIStyle RightAlignStyle
		{
			get
			{
				if ( s_RightAlignStyle == null )
				{
					s_RightAlignStyle = new GUIStyle( GUI.skin.label );
					switch ( s_RightAlignStyle.alignment )
					{
						case TextAnchor.LowerLeft:
							s_RightAlignStyle.alignment = TextAnchor.LowerRight;
							break;

						case TextAnchor.MiddleLeft:
							s_RightAlignStyle.alignment = TextAnchor.MiddleRight;
							break;

						case TextAnchor.UpperLeft:
							s_RightAlignStyle.alignment = TextAnchor.UpperRight;
							break;

						default:
							break;
					}
				}
				return s_RightAlignStyle;
			}
		}

		protected override bool CanDrawValueProperty( InspectorProperty property )
		{
			return property.ChildResolver is ISimpleNodePropertyPortResolver;
		}

		protected LabelDrawMode labelDrawMode;

		protected override void Initialize()
		{
			var settings = Property.GetAttribute<PortDrawerSettingsAttribute>();
			if ( settings == null )
				labelDrawMode = LabelDrawMode.XNodeDefault;
			else
				labelDrawMode = settings.LabelDrawMode;
		}

		protected bool isVisible = false;
		protected bool drawValue = true;

		protected override void DrawPropertyLayout( GUIContent label )
		{
			var portResolver = Property.ChildResolver as ISimpleNodePropertyPortResolver;
			if ( portResolver == null ) // sanity check
			{
				SirenixEditorGUI.ErrorMessageBox( $"Something went wrong - no valid port resolver. {portResolver}" );
				return;
			}

			var portChild = Property.Children[NodePropertyPort.NodePortPropertyName];

			var binding = ValueEntry.SmartValue;
			if ( Event.current.type == EventType.Layout )
			{
				switch ( portResolver.ShowBackingValue )
				{
					case ShowBackingValue.Always:
						drawValue = true;
						break;

					case ShowBackingValue.Never:
						drawValue = false;
						break;

					case ShowBackingValue.Unconnected:
						drawValue = !portResolver.Port.IsConnected;
						break;
				}

				isVisible = !portResolver.Node.folded;
				isVisible |= portResolver.ShowBackingValue == ShowBackingValue.Always;
				isVisible |= portResolver.Port.IsDynamic; // Dynamics will be folded somewhere else
				isVisible |= portResolver.Port.IsConnected;

				// Make sure that we aren't value-less
				drawValue &= !( Property.Parent != null && Property.Parent.Parent != null && Property.Parent.Parent.ChildResolver is IDynamicNoDataNodePropertyPortResolver );
			}

			if ( !isVisible )
				return;

			using ( new EditorGUILayout.HorizontalScope() )
			{
				if ( portChild == null )
				{
					SirenixEditorGUI.ErrorMessageBox( $"Expected property missing on {Property.Name} count: {Property.Children.Count} name: '{portResolver.Port.fieldName}' " );
					CallNextDrawer( label );
					return;
				}

				portChild.Draw( null ); // Port *always* draws unless folded
										// Offset back to make up for the port draw
				GUILayout.Space( -4 );

				if ( labelDrawMode == LabelDrawMode.OdinDefault )
				{
					if ( drawValue )
					{
						using ( new EditorGUILayout.VerticalScope() )
							CallNextDrawer( label );
					}
				}
				else
				{
					bool drawLabel = label != null && label != GUIContent.none;
					if ( portResolver.Port.IsInput )
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
							EditorGUILayout.PrefixLabel( label, GUI.skin.label, RightAlignStyle );
					}
				}
			}
		}

	}
}
