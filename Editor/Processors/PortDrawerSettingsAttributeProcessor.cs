using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using System.Reflection;
using static XNode.Node;
using Node = XNode.Node;

namespace XNodeEditor.Odin
{
    public class PortDrawerSettingsAttributeProcessor<T> : OdinAttributeProcessor<T>
        where T : Node
    {
        public override bool CanProcessSelfAttributes( InspectorProperty property )
        {
            return false;
        }

        public override bool CanProcessChildMemberAttributes( InspectorProperty parentProperty, MemberInfo member )
        {
            return member.GetCustomAttribute<PortDrawerSettingsAttribute>() == null &&
                parentProperty.GetAttribute<PortDrawerSettingsAttribute>() != null &&
                ( member.GetCustomAttribute<InputAttribute>() != null || member.GetCustomAttribute<OutputAttribute>() != null );
        }

        public override void ProcessChildMemberAttributes( InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes )
        {
            attributes.Add( parentProperty.GetAttribute<PortDrawerSettingsAttribute>() );
        }
    }
}
