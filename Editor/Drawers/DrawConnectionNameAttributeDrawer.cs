using System;

using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

using UnityEngine;

using static XNode.Node;

namespace XNodeEditor.Odin
{
	public class DrawConnectionNameAttributeDrawer<T> : NodePortAttributeDrawer<DrawConnectionNameAttribute, T>
	{
		protected override bool CanDrawNodePort( NodePortInfo nodePortInfo, InspectorProperty property )
		{
			return nodePortInfo.ConnectionType == ConnectionType.Override;
		}

		protected GUIContent connectionName = GUIContent.none;

		protected override void DrawPort( GUIContent label, NodePortInfo nodePortInfo, bool drawValue )
		{
			if ( Attribute.LabelWidth > 0 )
				GUIHelper.PushLabelWidth( Attribute.LabelWidth );

			// Extra sanity checks
			if ( Event.current.type == EventType.Layout )
			{
				if ( nodePortInfo.Port.IsConnected && nodePortInfo.Port.Connection != null && nodePortInfo.Port.Connection.node != null )
					connectionName = new GUIContent( nodePortInfo.Port.Connection.node.name );
				else
					connectionName = label;
			}

			CallNextDrawer( connectionName );

			if ( Attribute.LabelWidth > 0 )
				GUIHelper.PopLabelWidth();
		}
	}
}
