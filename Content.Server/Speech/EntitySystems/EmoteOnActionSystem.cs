using Content.Server.Chat.Systems;
using Content.Shared.Actions.Events;
using Content.Shared.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class EmoteOnActionSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmoteOnActionComponent, ActionPerformedEvent>(OnActionPerformed);
    }

    private void OnActionPerformed(Entity<EmoteOnActionComponent> ent, ref ActionPerformedEvent args)
    {
        var user = args.Performer;
        _chat.TryEmoteWithChat(user, ent.Comp.EmoteId, forceEmote: true);
    }
}
