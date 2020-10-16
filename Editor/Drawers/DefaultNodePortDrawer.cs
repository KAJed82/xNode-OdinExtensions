
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;

using UnityEngine;
using XNode;
using XNode.Odin;

namespace XNodeEditor.Odin
{
	[NodePortDrawerPriority]
	public class DefaultNodePortDrawer<T> : NodePortDrawer<T>, IDefinesGenericMenuItems
	{
		public bool hideContents = false;

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

		void IDefinesGenericMenuItems.PopulateGenericMenu( InspectorProperty property, GenericMenu genericMenu )
		{
			if ( NodePortInfo.Port.ConnectionCount > 0 )
			{
				// Remove all connections
				genericMenu.AddSeparator( string.Empty );
				genericMenu.AddItem( new GUIContent( "Clear Connections" ), false, ClearConnections );
				genericMenu.AddSeparator( string.Empty );

				for ( int i = 0; i < NodePortInfo.Port.ConnectionCount; ++i )
				{
					NodePort connection = NodePortInfo.Port.GetConnection( i );
					if ( connection == null ) // Connection exists but isn't actually connected
					{
						genericMenu.AddItem( new GUIContent( "Remove blank connections" ), false, RemoveBlankConnections );
						break;
					}
				}

				for ( int i = 0; i < NodePortInfo.Port.ConnectionCount; ++i )
				{
					NodePort connection = NodePortInfo.Port.GetConnection( i );
					if ( connection == null ) // Connection exists but isn't actually connected
						continue;

					int connectionIndex = i;
					genericMenu.AddItem( new GUIContent( $"Disconnect {connectionIndex} {connection.node.name}:{connection.fieldName}" ), false, () => Disconnect( connectionIndex ) );
				}
			}
		}

		protected void ClearConnections()
		{
			EditorApplication.delayCall += () => NodePortInfo.Port.ClearConnections();
		}

		protected void RemoveBlankConnections()
		{
			EditorApplication.delayCall += () =>
			  {
				  NodePortInfo.Port.VerifyConnections();
			  };
		}

		public void Disconnect( int connectionIndex )
		{
			EditorApplication.delayCall += () =>
			  {
				  NodePortInfo.Port.Disconnect( connectionIndex );
			  };
		}

		protected override void Initialize()
		{
			base.Initialize();

			var configuration = Property.GetAttribute<NodePortConfigurationAttribute>();
			if ( configuration != null )
				hideContents = configuration.HideContents;
		}

		protected override void DrawPort( GUIContent label )
		{
			if ( hideContents )
			{
				NodePortDrawerHelper.DrawPortHandle( NodePortInfo );

				// Offset back to make up for the port draw
				GUILayout.Space( -18 );
				return;
			}

			using ( new EditorGUILayout.HorizontalScope() )
			{
				NodePortDrawerHelper.DrawPortHandle( NodePortInfo );

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
					{
						if ( DrawValue )
							EditorGUILayout.LabelField( label, GUILayout.Width( GUIHelper.BetterLabelWidth ) );
						else
							EditorGUILayout.LabelField( label, GUILayout.MaxWidth( float.MaxValue ), GUILayout.ExpandWidth( true ) );
					}

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
					{
						if ( DrawValue )
							EditorGUILayout.LabelField( label, NodeEditorResources.OutputPort, GUILayout.Width( GUIHelper.BetterLabelWidth ) );
						else
							EditorGUILayout.LabelField( label, NodeEditorResources.OutputPort, GUILayout.MaxWidth( float.MaxValue ), GUILayout.ExpandWidth( true ) );
					}
				}
			}
		}
	}
}
