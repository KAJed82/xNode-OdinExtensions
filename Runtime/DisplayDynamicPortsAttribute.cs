using System;

/// <summary>
/// Automatically draws extra 'loose' dynamic ports to the end of the node.
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = false )]
public class DisplayDynamicPortsAttribute : Attribute { }
