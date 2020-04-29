using System;
using System.Collections.Generic;
using System.Reflection;

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

using XNode;

namespace XNodeEditor.Odin
{
    public class NodePortAttributeProcessor : OdinAttributeProcessor<NodePort>
    {
        public override bool CanProcessSelfAttributes( InspectorProperty property )
        {
            return true;
        }

        public override bool CanProcessChildMemberAttributes( InspectorProperty parentProperty, MemberInfo member )
        {
            return false;
        }

        public override void ProcessSelfAttributes( InspectorProperty property, List<Attribute> attributes )
        {
            attributes.Add( new HideReferenceObjectPickerAttribute() );
        }
    }
}
