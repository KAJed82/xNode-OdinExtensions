
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
				if ( parent == null )
					parent = property.Tree.SecretRootProperty;

				if ( parent.ChildResolver is INodePortResolver )
				{
					var resolver = parent.ChildResolver as INodePortResolver;
					NodePortInfo portInfo = resolver.GetNodePortInfo( property.Info );
					if ( portInfo != null )
						return true;
				}

				return false;
			}

			return false;
		}

		protected bool isVisible = false;

		protected sealed override void DrawPropertyLayout( GUIContent label )
		{
			if ( !NodeEditor.InNodeEditor )
			{
				CallNextDrawer( label );
				return;
			}

			// I have to do more work than I used to
			var parent = Property.ParentValueProperty;
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;

			var childResolver = Property.ChildResolver as IDynamicDataNodePropertyPortResolver;
			var resolver = parent.ChildResolver as INodePortResolver;
			var nodePortInfo = resolver.GetNodePortInfo( Property.Info );
			var dontFold = Property.GetAttribute<DontFoldAttribute>() != null;

			if ( Event.current.type == EventType.Layout )
			{
				isVisible = !nodePortInfo.Node.folded;
				isVisible |= childResolver.DynamicPortInfo.ports.Any( x => x.IsConnected );
				isVisible |= dontFold;
			}

			if ( !isVisible )
				return;

			DrawPortList( label, nodePortInfo, childResolver.DynamicPortInfo.ports );
		}

		protected abstract void DrawPortList( GUIContent label, NodePortInfo nodePortInfo, List<NodePort> nodePorts );
	}
}
