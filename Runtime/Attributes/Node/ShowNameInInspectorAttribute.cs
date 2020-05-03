using System;

namespace XNode.Odin
{
	[AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
	public class ShowNameInInspectorAttribute : Attribute { }
}
