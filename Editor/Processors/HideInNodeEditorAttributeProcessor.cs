using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using XNode.Odin;

namespace XNodeEditor.Odin
{
	public class HideInNodeEditorAttributeProcessor<T> : OdinAttributeProcessor<T>
	{
		public override bool CanProcessSelfAttributes( InspectorProperty property )
		{
			if ( NodeEditor.InNodeEditor )
			{
				if ( property.GetAttribute<HideInNodeEditorAttribute>() != null )
					return true;

				if ( typeof( T ).GetCustomAttribute<HideInNodeEditorAttribute>() != null )
					return true;
			}

			return false;
		}

		public override bool CanProcessChildMemberAttributes( InspectorProperty parentProperty, MemberInfo member )
		{
			return false;
		}

		public override void ProcessSelfAttributes( InspectorProperty property, List<Attribute> attributes )
		{
			attributes.Add( new HideInInspector() );
		}
	}

	// I shouldn't need this, but it guarantees things get removed that shouldn't be shown
	public class HideInNodeEditorPropertyProcessor<T> : OdinPropertyProcessor<T>
	{
		public override bool CanProcessForProperty( InspectorProperty property )
		{
			return NodeEditor.InNodeEditor;
		}

		public override void ProcessMemberProperties( List<InspectorPropertyInfo> propertyInfos )
		{
			for ( int i = propertyInfos.Count -1; i>=0;--i)
			{
				InspectorPropertyInfo p = propertyInfos[i];
				if ( p.GetAttribute<HideInNodeEditorAttribute>() != null )
					propertyInfos.RemoveAt( i );
			}
		}
	}
}
