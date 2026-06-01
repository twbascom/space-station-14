using Robust.Shared.GameStates;

namespace Content.Shared.FeralInfected.Components;

/// <summary>
/// Marks an entity as a Feral Infected host.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FeralInfectedComponent : Component
{
    /// <summary>
    /// Tracks time since the last sanity popup.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SanityAccumulator = 0.0f;

    /// <summary>
    /// The average time in seconds between sanity-slip popups.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float PopupInterval = 45.0f;

    /// <summary>
    /// Tracks time in seconds since the host last devoured a corpse.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HungerAccumulator = 0.0f;

    /// <summary>
    /// Time in seconds before the host starts starving and gets frequent sanity popups.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HungerLimit = 180.0f;

    /// <summary>
    /// Number of crew members this host has devoured.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int DevouredCount = 0;

    /// <summary>
    /// Number of devours required to ascend and mutate.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int RequiredDevourForAscension = 3;

    /// <summary>
    /// Whether this host has completed their mutation/ascension.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Ascended = false;
}
