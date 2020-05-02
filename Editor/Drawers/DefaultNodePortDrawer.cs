
using Sirenix.OdinInspector.Editor;

using UnityEditor;

using UnityEngine;
using XNode;

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

		protected override void DrawPort( GUIContent label )
		{
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
