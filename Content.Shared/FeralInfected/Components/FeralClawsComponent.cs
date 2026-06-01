using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared.FeralInfected.Components;

/// <summary>
/// Component applied to claws to identify them for prying and feeding interactions.
/// </summary>
[RegisterComponent]
public sealed partial class FeralClawsComponent : Component
{
    /// <summary>
    /// If true, these claws are the upgraded ascended claws.
    /// </summary>
    [DataField]
    public bool Ascended = false;
}

/// <summary>
/// Event raised when the feed DoAfter completes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class FeralFeedDoAfterEvent : SimpleDoAfterEvent;
