using System;

using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

using UnityEngine;

using static XNode.Node;

namespace XNodeEditor.Odin
{
	//[AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
	//public class ConnectionNameDrawerPriorityAttribute : DrawerPriorityAttribute
	//{
	//	public ConnectionNameDrawerPriorityAttribute() : base( 0, 1, 0 )
	//	{
	//	}
	//}

	//[ConnectionNameDrawerPriority]
	public class DrawConnectionNameAttributeDrawer<T> : NodePortAttributeDrawer<DrawConnectionNameAttribute, T>
	{
		protected override bool CanDrawNodePort( NodePortInfo nodePortInfo, InspectorProperty property )
		{
			return nodePortInfo.ConnectionType == ConnectionType.Override;
		}

		protected override void DrawPort( GUIContent label, NodePortInfo nodePortInfo, bool drawValue )
		{
			if ( Attribute.LabelWidth > 0 )
				GUIHelper.PushLabelWidth( Attribute.LabelWidth );

			if ( nodePortInfo.Port.IsConnected )
				CallNextDrawer( new GUIContent( nodePortInfo.Port.Connection.node.name ) );
			else
				CallNextDrawer( label );

			if ( Attribute.LabelWidth > 0 )
				GUIHelper.PopLabelWidth();
		}
	}
}
