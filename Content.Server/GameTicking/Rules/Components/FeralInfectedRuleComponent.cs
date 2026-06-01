using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Component for the Feral Infected gamerule.
/// </summary>
[RegisterComponent, Access(typeof(FeralInfectedRuleSystem))]
public sealed partial class FeralInfectedRuleComponent : Component
{
    /// <summary>
    /// The claws prototype to spawn in the host's hands.
    /// </summary>
    [DataField]
    public EntProtoId ClawsPrototype = "InfectedClaws";

    /// <summary>
    /// The role prototype ID for the Feral Infected role.
    /// </summary>
    [DataField]
    public ProtoId<AntagPrototype> FeralInfectedRole = "FeralInfected";
}
