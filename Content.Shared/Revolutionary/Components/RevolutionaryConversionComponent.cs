using Robust.Shared.GameStates;

namespace Content.Shared.Revolutionary.Components;

/// <summary>
/// Tracks the revolutionary conversion progress of an entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RevolutionaryConversionComponent : Component
{
    /// <summary>
    /// Current progress toward conversion.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Progress = 0.0f;

    /// <summary>
    /// Progress required to be converted or notified.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxProgress = 100.0f;
}
