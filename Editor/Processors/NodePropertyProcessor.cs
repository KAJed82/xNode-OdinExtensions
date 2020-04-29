using System.Collections.Generic;

using Sirenix.OdinInspector.Editor;

using XNode;

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
		}
	}
}
