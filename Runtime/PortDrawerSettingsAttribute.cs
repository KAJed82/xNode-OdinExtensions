#if ODIN_INSPECTOR
using System;

public enum LabelDrawMode
{
    /// <summary>
    /// Input port labels are aligned to the left, output port labels are aligned to the right. Label passed through drawer is null
    /// </summary>
    XNodeDefault,
    /// <summary>
    /// Input / output port will draw with standard Odin drawers; no alignment of labels. Label passed through is valid
    /// </summary>
    OdinDefault
}

[AttributeUsage( AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Struct, AllowMultiple = false, Inherited = true )]
public class PortDrawerSettingsAttribute : Attribute
{
    public LabelDrawMode LabelDrawMode { get; protected set; }

    public PortDrawerSettingsAttribute( LabelDrawMode labelDrawMode )
    {
        LabelDrawMode = labelDrawMode;
    }
}
#endif