using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;

using UnityEngine;
using XNode;

namespace XNodeEditor.Odin
{
	public class DynamicPortInfo
	{
		public List<NodePort> ports;

		public int min;
		public int max;

		//public int ExpectedCount => max - min;
	}

	public class DynamicPortHelper : MonoBehaviour
	{
		// Taken from 'public static void DynamicPortList' in NodeEditorGUILayout
		public static DynamicPortInfo GetDynamicPortData( Node node, string baseFieldName )
		{
			DynamicPortInfo info = new DynamicPortInfo() { min = int.MaxValue, max = int.MinValue };

			var indexedPorts = node.DynamicPorts.Select( x =>
			{
				string[] split = x.fieldName.Split( ' ' );
				if ( split != null && split.Length == 2 && split[0] == baseFieldName )
				{
					int i = -1;
					if ( int.TryParse( split[1], out i ) )
					{
						info.min = i < info.min ? i : info.min;
						info.max = i > info.max ? i : info.max;
						return new { index = i, port = x };
					}
				}
				return new { index = -1, port = (XNode.NodePort)null };
			} ).Where( x => x.port != null );

			info.ports = indexedPorts.OrderBy( x => x.index ).Select( x => x.port ).ToList();
			return info;
		}
	}
}
