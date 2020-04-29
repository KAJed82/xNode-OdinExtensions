
using System;
using System.Collections.Generic;

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;

using static XNode.Node;

namespace XNodeEditor.Odin
{
    public class DynamicDataNodePropertyPortAttributeProcessor<TList, TElement> : OdinAttributeProcessor<TList>
    where TList : IList<TElement>
    {
        public override bool CanProcessSelfAttributes( InspectorProperty property )
        {
            if ( !NodeEditor.InNodeEditor )
                return false;

            var inputAttribute = property.GetAttribute<InputAttribute>();
            if ( inputAttribute != null )
                return inputAttribute.dynamicPortList;

            var outputAttribute = property.GetAttribute<OutputAttribute>();
            if ( outputAttribute != null )
                return outputAttribute.dynamicPortList;

            return false;
        }

        public override void ProcessSelfAttributes( InspectorProperty property, List<Attribute> attributes )
        {
            var listDrawerAttributes = attributes.GetAttribute<ListDrawerSettingsAttribute>();
            if ( listDrawerAttributes == null )
            {
                listDrawerAttributes = new ListDrawerSettingsAttribute();
                attributes.Add( listDrawerAttributes );
            }

            listDrawerAttributes.Expanded = true;
            listDrawerAttributes.ShowPaging = false;
        }
    }
}
