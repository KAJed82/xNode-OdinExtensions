using System;

/// <summary>
/// Apply to a dynamic port list in order to hide the connection names in the list
/// </summary>
[AttributeUsage( AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
public class HideConnectionLabelAttribute : Attribute { }
