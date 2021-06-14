
using Sirenix.OdinInspector.Editor;

using UnityEngine;

namespace XNodeEditor.Odin
{
	[DrawerPriority( 1.5, 0, 0 )]
	internal class FoldedNodePropertyDrawer : OdinDrawer
	{
		public override bool CanDrawProperty( InspectorProperty property )
		{
			if ( !NodeEditor.InNodeEditor )
				return false;

			var parent = property.ParentValueProperty;
#if ODIN_INSPECTOR_3
			if ( property.IsTreeRoot )
				return false;

			if ( parent == null )
				parent = property.Tree.RootProperty;
#else
			if ( parent == null )
				parent = property.Tree.SecretRootProperty;
#endif

			if ( parent.ChildResolver is INodePortResolver )
			{
				var resolver = parent.ChildResolver as INodePortResolver;
				NodePortInfo portInfo = resolver.GetNodePortInfo( property.Name );
				if ( portInfo != null )
					return false;
			}
			else
			{
				return false;
			}

			if ( property.ChildResolver is GroupPropertyResolver )
				return false;

			return property.GetAttribute<DontFoldAttribute>() == null;
		}

		protected INodePortResolver PortResolver { get; private set; }
		protected NodePortInfo NodePortInfo { get; private set; }
		protected bool CanFold { get; private set; }
		protected bool DrawValue { get; private set; }

		protected override void Initialize()
		{
			var parent = Property.ParentValueProperty;
#if ODIN_INSPECTOR_3
			if ( parent == null )
				parent = Property.Tree.RootProperty;
#else
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;
#endif

			PortResolver = parent.ChildResolver as INodePortResolver;
			NodePortInfo = PortResolver.GetNodePortInfo( Property.Name );
			CanFold = Property.GetAttribute<DontFoldAttribute>() == null;
			DrawValue = true;
		}

		private bool isVisible = false;

		protected override void DrawPropertyLayout( GUIContent label )
		{
			if ( Event.current.type == EventType.Layout )
				isVisible = !PortResolver.Node.folded;

			if ( !isVisible )
				return;

			CallNextDrawer( label );
		}
	}
}
