using System;

namespace XNode.Odin
{
	[AttributeUsage( AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false )]
	public abstract class NodePortAttribute : Attribute { }
}
