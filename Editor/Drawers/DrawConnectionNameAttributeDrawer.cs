using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System;
using UnityEngine;
using static XNode.Node;

namespace XNodeEditor.Odin
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class ConnectionNameDrawerPriorityAttribute : DrawerPriorityAttribute
    {
        public ConnectionNameDrawerPriorityAttribute() : base( 1.2, 0, 0 )
        {
        }
    }

    [ConnectionNameDrawerPriority]
    public class DrawConnectionNameAttributeDrawer<T> : OdinAttributeDrawer<DrawConnectionNameAttribute, T>
    {
        protected override bool CanDrawAttributeValueProperty( InspectorProperty property )
        {
            if ( !NodeEditor.InNodeEditor )
                return false;

            return property.Parent != null && property.Parent.ChildResolver is IDynamicPortListNodePropertyResolverWithPorts &&
                property.ChildResolver is ISimpleNodePropertyPortResolver;
        }

        protected override void DrawPropertyLayout( GUIContent label )
        {
            var portResolver = Property.ChildResolver as ISimpleNodePropertyPortResolver;
            var port = portResolver.Port;

            if ( Attribute.LabelWidth > 0 )
                GUIHelper.PushLabelWidth( Attribute.LabelWidth );

            if ( port.connectionType == ConnectionType.Override && port.IsConnected )
                CallNextDrawer( new GUIContent( port.Connection.node.name ) );
            else
                CallNextDrawer( null );

            if ( Attribute.LabelWidth > 0 )
                GUIHelper.PopLabelWidth();
        }
    }
}
