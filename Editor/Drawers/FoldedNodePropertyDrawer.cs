
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;
using static XNode.Node;

namespace XNodeEditor.Odin
{
    [DrawerPriority( 1.5, 0, 0 )]
    internal class FoldedNodePropertyDrawer<T> : OdinValueDrawer<T>
    {
        protected override bool CanDrawValueProperty( InspectorProperty property )
        {
            if ( !( property.Tree.WeakTargets.FirstOrDefault() is Node ) )
                return false;

            if ( property.ValueEntry.TypeOfValue.ImplementsOrInherits( typeof( NodePort ) ) )
                return false;

            if ( property.GetAttribute<InputAttribute>() != null || property.GetAttribute<OutputAttribute>() != null || property.GetAttribute<DontFoldAttribute>() != null )
                return false;

            // Don't try to fold that were resolved by my magic
            if ( property.Parent != null )
            {
                if ( property.Parent.ChildResolver is IDynamicPortListNodePropertyResolverWithPorts )
                    return false;
                if ( property.Parent.ChildResolver is IDynamicNoDataNodePropertyPortResolver )
                    return false;
            }

            return true;
        }

        protected override void DrawPropertyLayout( GUIContent label )
        {
            // Passthrough if we aren't in the node editor
            if ( !NodeEditor.InNodeEditor )
            {
                CallNextDrawer( label );
                return;
            }

            Node node = Property.Tree.WeakTargets.OfType<Node>().FirstOrDefault();

            // If this property has "DontFoldAttribute" then don't!
            if ( node == null )
            {
                CallNextDrawer( label );
                return;
            }

            if ( !node.folded )
                CallNextDrawer( label );
        }
    }
}