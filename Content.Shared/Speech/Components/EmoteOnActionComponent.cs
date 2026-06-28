using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.Components;

/// <summary>
///     Action component that triggers a specific emote on the performer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmoteOnActionComponent : Component
{
    /// <summary>
    ///     The emote prototype ID to trigger.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<EmotePrototype> EmoteId;
}
