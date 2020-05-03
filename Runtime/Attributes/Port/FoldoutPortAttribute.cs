using System;

namespace XNode.Odin
{
	[AttributeUsage( AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface,
		AllowMultiple = false,
		Inherited = false )]
	public class FoldoutPortAttribute : NodePortAttribute { }
}
