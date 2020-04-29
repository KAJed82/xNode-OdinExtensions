#if ODIN_INSPECTOR
using System;

[AttributeUsage( AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
public class HideConnectionLabelAttribute : Attribute { }
#endif