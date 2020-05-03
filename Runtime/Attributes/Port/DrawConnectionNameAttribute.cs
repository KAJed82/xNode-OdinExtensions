using System;

namespace XNode.Odin
{
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
