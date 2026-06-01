using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that at least a certain percentage of the station or a department is converted into Revolutionaries.
/// </summary>
[RegisterComponent]
public sealed partial class ConvertPercentageConditionComponent : Component
{
    /// <summary>
    /// Target percentage to convert (e.g. 0.30 for 30%).
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public float Percentage;

    /// <summary>
    /// Optional department prototype ID to limit the conversion target to (e.g. "Medical").
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<DepartmentPrototype>? Department;
}
