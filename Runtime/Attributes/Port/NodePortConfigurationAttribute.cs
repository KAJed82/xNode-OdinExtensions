namespace XNode.Odin
{
	public class NodePortConfigurationAttribute : NodePortAttribute
	{
		/// <summary>
		/// Useful for ports that only want to be connections and not hold data in any way.
		/// </summary>
		public bool HideContents { get; protected set; }

		public NodePortConfigurationAttribute( bool hideContents )
		{
			HideContents = hideContents;
		}
	}
}
