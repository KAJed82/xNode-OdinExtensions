﻿using System;

using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;
using XNode.Odin;
using static XNode.Node;

namespace XNodeEditor.Odin
{
	public abstract class NodePortAttributeDrawer<TAttribute> : OdinAttributeDrawer<TAttribute>
		where TAttribute : NodePortAttribute
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
					return ( portInfo.IsDynamic || !portInfo.IsDynamicPortList ) && CanDrawNodePort( resolver, portInfo, property );

				return false;
			}

			return false;
		}

		protected virtual bool CanDrawNodePort( INodePortResolver portResolver, NodePortInfo nodePortInfo, InspectorProperty property )
		{
			return true;
		}

		protected INodePortResolver PortResolver { get; private set; }
		protected NodePortInfo NodePortInfo { get; private set; }
		protected bool CanFold { get; private set; }
		protected bool DrawValue { get; private set; }

		protected override void Initialize()
		{
			var parent = Property.ParentValueProperty;
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;

			PortResolver = parent.ChildResolver as INodePortResolver;
			NodePortInfo = PortResolver.GetNodePortInfo( Property.Name );
			CanFold = Property.GetAttribute<DontFoldAttribute>() == null;
			DrawValue = true;
		}

		private bool isVisible = false;

		protected sealed override void DrawPropertyLayout( GUIContent label )
		{
			if ( NodePortDrawerHelper.DisplayMissingPort( Property, PortResolver, NodePortInfo ) )
				return;

			if ( Event.current.type == EventType.Layout )
			{
				switch ( NodePortInfo.ShowBackingValue )
				{
					case ShowBackingValue.Always:
						DrawValue = true;
						break;

					case ShowBackingValue.Never:
						DrawValue = false;
						break;

					case ShowBackingValue.Unconnected:
						DrawValue = !NodePortInfo.Port.IsConnected;
						break;
				}

				isVisible = !NodePortInfo.Node.folded;
				isVisible |= NodePortInfo.ShowBackingValue == ShowBackingValue.Always;
				isVisible |= NodePortInfo.Port.IsDynamic; // Dynamics will be folded somewhere else
				isVisible |= NodePortInfo.Port.IsConnected;
				isVisible |= !CanFold;

				DrawValue &= NodePortInfo.HasValue;
			}

			if ( !isVisible )
				return;

			DrawPort( label );
		}

		protected abstract void DrawPort( GUIContent label );
	}

	public abstract class NodePortAttributeDrawer<TAttribute, TValue> : OdinAttributeDrawer<TAttribute, TValue>
		where TAttribute : NodePortAttribute
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

		protected INodePortResolver PortResolver { get; private set; }
		protected NodePortInfo NodePortInfo { get; private set; }
		protected bool CanFold { get; private set; }
		protected bool DrawValue { get; private set; }

		protected override void Initialize()
		{
			var parent = Property.ParentValueProperty;
			if ( parent == null )
				parent = Property.Tree.SecretRootProperty;

			PortResolver = parent.ChildResolver as INodePortResolver;
			NodePortInfo = PortResolver.GetNodePortInfo( Property.Name );
			CanFold = Property.GetAttribute<DontFoldAttribute>() == null;
			DrawValue = true;
		}

		private bool isVisible = false;

		protected sealed override void DrawPropertyLayout( GUIContent label )
		{
			if ( NodePortDrawerHelper.DisplayMissingPort( Property, PortResolver, NodePortInfo ) )
				return;

			if ( Event.current.type == EventType.Layout )
			{
				switch ( NodePortInfo.ShowBackingValue )
				{
					case ShowBackingValue.Always:
						DrawValue = true;
						break;

					case ShowBackingValue.Never:
						DrawValue = false;
						break;

					case ShowBackingValue.Unconnected:
						DrawValue = !NodePortInfo.Port.IsConnected;
						break;
				}

				isVisible = !NodePortInfo.Node.folded;
				isVisible |= NodePortInfo.ShowBackingValue == ShowBackingValue.Always;
				isVisible |= NodePortInfo.Port.IsDynamic; // Dynamics will be folded somewhere else
				isVisible |= NodePortInfo.Port.IsConnected;
				isVisible |= !CanFold;

				DrawValue &= NodePortInfo.HasValue;
			}

			if ( !isVisible )
				return;

			DrawPort( label );
		}

		protected abstract void DrawPort( GUIContent label );
	}
}
