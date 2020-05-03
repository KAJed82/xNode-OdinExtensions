using System;

namespace XNode.Odin
{
	/// <summary>
	/// Automatically draws extra 'loose' dynamic ports to the end of the node.
	/// </summary>
	[AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
	public class DisplayDynamicPortsAttribute : NodePortAttribute
	{
		public bool ShowRemoveButton { get; private set; }

		public DisplayDynamicPortsAttribute()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="showRemoveButton">Set this to true to had a button automatically added to 'loose' dynamic ports to remove them.</param>
		public DisplayDynamicPortsAttribute( bool showRemoveButton )
		{
			ShowRemoveButton = showRemoveButton;
		}
	}
}
