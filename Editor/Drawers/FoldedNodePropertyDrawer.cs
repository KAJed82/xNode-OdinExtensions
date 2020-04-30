
using Sirenix.OdinInspector.Editor;

using UnityEngine;

namespace XNodeEditor.Odin
{
	[DrawerPriority( 1.5, 0, 0 )]
	internal class FoldedNodePropertyDrawer<T> : OdinValueDrawer<T>
	{
		protected override bool CanDrawValueProperty( InspectorProperty property )
		{
			if ( !NodeEditor.InNodeEditor )
				return false;

			var parent = property.ParentValueProperty;
			if ( parent == null )
				parent = property.Tree.SecretRootProperty;

			if ( parent.ChildResolver is INodePortResolver )
			{
				var resolver = parent.ChildResolver as INodePortResolver;
				NodePortInfo portInfo = resolver.GetNodePortInfo( property.Info );
				if ( portInfo != null )
					return false;
			}
			else
			{
				return false;
			}

			return property.GetAttribute<DontFoldAttribute>() == null;
		}

		protected bool isVisible = false;

		protected override void DrawPropertyLayout( GUIContent label )
		{
			// Passthrough if we aren't in the node editor
			if ( !NodeEditor.InNodeEditor )
			{
				CallNextDrawer( label );
				return;
			}

			var parent = Property.ParentValueProperty;
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;

			var resolver = parent.ChildResolver as INodePortResolver;
			if ( Event.current.type == EventType.Layout )
				isVisible = !resolver.Node.folded;

			if ( !isVisible )
				return;

			CallNextDrawer( label );
		}
	}
}
