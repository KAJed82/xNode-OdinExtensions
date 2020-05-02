using System;

namespace XNode.Odin
{
	[AttributeUsage( AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
	public class DrawConnectionNameAttribute : NodePortAttribute
	{
		public int LabelWidth { get; private set; }

		public DrawConnectionNameAttribute()
		{
		}

		public DrawConnectionNameAttribute( int labelWidth )
		{
			LabelWidth = labelWidth;
		}
	}
}
