
using System.Collections.Generic;
using System.Linq;

using Sirenix.OdinInspector.Editor;

using UnityEngine;

using XNode;

namespace XNodeEditor.Odin
{
	public abstract class NodePortListDrawer<T> : OdinValueDrawer<T>
	{
		protected sealed override bool CanDrawValueProperty( InspectorProperty property )
		{
			if ( !NodeEditor.InNodeEditor )
				return false;

			if ( property.ChildResolver is IDynamicDataNodePropertyPortResolver )
			{
				var parent = property.ParentValueProperty;
#if ODIN_INSPECTOR_3
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
						return true;
				}

				return false;
			}

			return false;
		}

		protected INodePortResolver PortResolver { get; private set; }
		protected IDynamicDataNodePropertyPortResolver PortListResolver { get; private set; }

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
			PortListResolver = Property.ChildResolver as IDynamicDataNodePropertyPortResolver;
			NodePortInfo = PortResolver.GetNodePortInfo( Property.Name );
			CanFold = Property.GetAttribute<DontFoldAttribute>() == null;
			DrawValue = true;
		}

		private bool isVisible = false;

		protected sealed override void DrawPropertyLayout( GUIContent label )
		{
			if ( Event.current.type == EventType.Layout && !NodeEditorWindow.current.IsDraggingPort )
			{
				isVisible = !NodePortInfo.Node.folded;
				isVisible |= PortListResolver.AnyConnected;
				isVisible |= !CanFold;
			}

			if ( !isVisible )
				return;

			DrawPortList( label );
		}

		protected abstract void DrawPortList( GUIContent label );
	}
}
