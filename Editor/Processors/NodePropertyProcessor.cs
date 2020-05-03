using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using XNode;
using XNode.Odin;

namespace XNodeEditor.Odin
{
	public static class NodePropertyPort
	{
		public const string NodePortPropertyName = "xnode:port";
		public const string NodePortListPropertyName = "xnode:portlist";
	}

	public class NodePropertyProcessor<TNode> : OdinPropertyProcessor<TNode>
		where TNode : Node
	{
		public override void ProcessMemberProperties( List<InspectorPropertyInfo> infos )
		{
			if ( !NodeEditor.InNodeEditor )
				return;

			// Remove excluded properties
			string[] excludes = { "m_Script", "graph", "position", "folded", "ports" };
			foreach ( var exclude in excludes )
				infos.Remove( infos.Find( exclude ) );

			if ( Property.GetAttribute<ShowNameInNodeEditorAttribute>() != null )
			{
				var nameProperty = InspectorPropertyInfo.CreateValue(
					"name",
					0,
					Property.ValueEntry.SerializationBackend,
					new GetterSetter<TNode, string>(
						( ref TNode node ) => node.name,
						( ref TNode node, string value ) =>
						{
							Undo.RegisterFullObjectHierarchyUndo( node, "Set node name" );
							node.name = value;
						}
					),
					new Sirenix.OdinInspector.DelayedPropertyAttribute()
				);
				infos.Insert( 0, nameProperty );
			}
		}
	}

	public class InspectorNodePropertyProcessor<TNode> : OdinPropertyProcessor<TNode>
		where TNode : Node
	{
		public override void ProcessMemberProperties( List<InspectorPropertyInfo> infos )
		{
			if ( NodeEditor.InNodeEditor )
				return;

			if ( Property.GetAttribute<ShowNameInInspectorAttribute>() != null )
			{
				var nameProperty = InspectorPropertyInfo.CreateValue(
					"name",
					0,
					Property.ValueEntry.SerializationBackend,
					new GetterSetter<TNode, string>(
						( ref TNode node ) => node.name,
						( ref TNode node, string value ) =>
						{
							Undo.RegisterFullObjectHierarchyUndo( node, "Set node name" );
							node.name = value;
						}
					),
					new Sirenix.OdinInspector.DelayedPropertyAttribute()
				);
				infos.Insert( 0, nameProperty );
			}
		}
	}
}
