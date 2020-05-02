using System;

using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

using static XNode.Node;

namespace XNodeEditor.Odin
{
	public abstract class NodePortAttributeDrawer<TAttribute> : OdinAttributeDrawer<TAttribute>
		where TAttribute : Attribute
	{
		protected sealed override bool CanDrawAttributeProperty( InspectorProperty property )
		{
			if ( !NodeEditor.InNodeEditor )
				return false;

			var parent = property.ParentValueProperty;
			if ( parent == null )
				parent = property.Tree.SecretRootProperty;

			if ( parent.ChildResolver is INodePortResolver )
			{
				var resolver = parent.ChildResolver as INodePortResolver;
				NodePortInfo portInfo = resolver.GetNodePortInfo( property.Name );
				if ( portInfo != null )
					return ( portInfo.IsDynamic || !portInfo.IsDynamicPortList ) && CanDrawNodePort( portInfo, property );

				return false;
			}

			return false;
		}

		protected virtual bool CanDrawNodePort( NodePortInfo nodePortInfo, InspectorProperty property )
		{
			return true;
		}

		protected bool isVisible = false;
		protected bool drawValue = true;

		protected sealed override void DrawPropertyLayout( GUIContent label )
		{
			// Unless someone tells me otherwise I will not draw root dynamic list ports as real things
			if ( !NodeEditor.InNodeEditor )
			{
				CallNextDrawer( label );
				return;
			}

			// I have to do more work than I used to
			var parent = Property.ParentValueProperty;
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;

			var resolver = parent.ChildResolver as INodePortResolver;
			var nodePortInfo = resolver.GetNodePortInfo( Property.Name );
			var dontFold = Property.GetAttribute<DontFoldAttribute>() != null;

			if ( NodePortDrawerHelper.DisplayMissingPort( Property, resolver, nodePortInfo ) )
				return;

			if ( Event.current.type == EventType.Layout )
			{
				switch ( nodePortInfo.ShowBackingValue )
				{
					case ShowBackingValue.Always:
						drawValue = true;
						break;

					case ShowBackingValue.Never:
						drawValue = false;
						break;

					case ShowBackingValue.Unconnected:
						drawValue = !nodePortInfo.Port.IsConnected;
						break;
				}

				isVisible = !nodePortInfo.Node.folded;
				isVisible |= nodePortInfo.ShowBackingValue == ShowBackingValue.Always;
				isVisible |= nodePortInfo.Port.IsDynamic; // Dynamics will be folded somewhere else
				isVisible |= nodePortInfo.Port.IsConnected;
				isVisible |= dontFold;

				drawValue &= nodePortInfo.HasValue;
			}

			if ( !isVisible )
				return;

			DrawPort( label, nodePortInfo, drawValue );
		}

		protected abstract void DrawPort( GUIContent label, NodePortInfo nodePortInfo, bool drawValue );
	}

	public abstract class NodePortAttributeDrawer<TAttribute, TValue> : OdinAttributeDrawer<TAttribute, TValue>
		where TAttribute : Attribute
	{
		protected sealed override bool CanDrawAttributeValueProperty( InspectorProperty property )
		{
			if ( !NodeEditor.InNodeEditor )
				return false;

			var parent = property.ParentValueProperty;
			if ( parent == null )
				parent = property.Tree.SecretRootProperty;

			if ( parent.ChildResolver is INodePortResolver )
			{
				var resolver = parent.ChildResolver as INodePortResolver;
				NodePortInfo portInfo = resolver.GetNodePortInfo( property.Name );
				if ( portInfo != null )
					return ( portInfo.IsDynamic || !portInfo.IsDynamicPortList ) && CanDrawNodePort( portInfo, property );

				return false;
			}

			return false;
		}
		protected virtual bool CanDrawNodePort( NodePortInfo nodePortInfo, InspectorProperty property )
		{
			return true;
		}

		protected bool isVisible = false;
		protected bool drawValue = true;

		protected sealed override void DrawPropertyLayout( GUIContent label )
		{
			// Unless someone tells me otherwise I will not draw root dynamic list ports as real things
			if ( !NodeEditor.InNodeEditor )
			{
				CallNextDrawer( label );
				return;
			}

			// I have to do more work than I used to
			var parent = Property.ParentValueProperty;
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;

			var resolver = parent.ChildResolver as INodePortResolver;
			var nodePortInfo = resolver.GetNodePortInfo( Property.Name );
			var dontFold = Property.GetAttribute<DontFoldAttribute>() != null;

			if ( NodePortDrawerHelper.DisplayMissingPort( Property, resolver, nodePortInfo ) )
				return;

			if ( Event.current.type == EventType.Layout )
			{
				switch ( nodePortInfo.ShowBackingValue )
				{
					case ShowBackingValue.Always:
						drawValue = true;
						break;

					case ShowBackingValue.Never:
						drawValue = false;
						break;

					case ShowBackingValue.Unconnected:
						drawValue = !nodePortInfo.Port.IsConnected;
						break;
				}

				isVisible = !nodePortInfo.Node.folded;
				isVisible |= nodePortInfo.ShowBackingValue == ShowBackingValue.Always;
				isVisible |= nodePortInfo.Port.IsDynamic; // Dynamics will be folded somewhere else
				isVisible |= nodePortInfo.Port.IsConnected;
				isVisible |= dontFold;

				drawValue &= nodePortInfo.HasValue;
			}

			if ( !isVisible )
				return;

			DrawPort( label, nodePortInfo, drawValue );
		}

		protected abstract void DrawPort( GUIContent label, NodePortInfo nodePortInfo, bool drawValue );
	}
}
