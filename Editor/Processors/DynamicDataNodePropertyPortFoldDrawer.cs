using System.Collections.Generic;
using System.Linq;

using Sirenix.OdinInspector.Editor;

using UnityEngine;

using static XNode.Node;

namespace XNodeEditor.Odin
{
    //[DrawerPriority( 0, 0, 1 )]
    //public class DynamicDataNodePropertyPortFoldDrawer<TList, TElement> : OdinValueDrawer<TList>
    //    where TList : IList<TElement>
    //{
    //    protected override bool CanDrawValueProperty( InspectorProperty property )
    //    {
    //        if ( !NodeEditor.InNodeEditor )
    //            return false;

    //        if ( property.GetAttribute<DontFoldAttribute>() != null )
    //            return false;

    //        if ( property.ParentValueProperty != null && property.ParentValueProperty.GetAttribute<DontFoldAttribute>() != null )
    //            return false;

    //        var resolver = property.ChildResolver as IDynamicDataNodePropertyPortResolver;
    //        return resolver != null && resolver.IsDynamicPortList;
    //    }

    //    protected bool isVisible = false;

    //    protected override void DrawPropertyLayout( GUIContent label )
    //    {
    //        var resolver = Property.ChildResolver as IDynamicDataNodePropertyPortResolver;

    //        if ( Event.current.type == EventType.Layout )
    //        {
    //            isVisible = !resolver.Node.folded;
    //            isVisible |= resolver.ShowBackingValue == ShowBackingValue.Always;
    //            isVisible |= resolver.DynamicPorts.Any( x => x.IsConnected );
    //        }

    //        if ( !isVisible )
    //            return;

    //        CallNextDrawer( label );
    //    }
    //}
}
