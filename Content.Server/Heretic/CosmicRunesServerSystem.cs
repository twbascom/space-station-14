using Content.Server.Bible.Components;
using Content.Shared._Goobstation.Wizard.FadingTimedDespawn;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared._Shitcode.Heretic.Systems.Abilities;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Heretic;

public sealed class CosmicRunesServerSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHereticAbilitySystem _heretic = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HereticCosmicRuneComponent, AfterInteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<HereticCosmicRuneComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || HasComp<FadingTimedDespawnComponent>(ent))
            return;

        // 1. StarTouch interaction
        if (TryComp<StarTouchComponent>(args.Used, out var starTouch))
        {
            _heretic.InvokeTouchSpell<StarTouchComponent>((args.Used, starTouch), args.User);
            EnsureComp<FadingTimedDespawnComponent>(ent).Lifetime = 0f;
            if (Exists(ent.Comp.LinkedRune))
                EnsureComp<FadingTimedDespawnComponent>(ent.Comp.LinkedRune.Value).Lifetime = 0f;
            args.Handled = true;
            return;
        }

        // 2. Bible interaction
        if (!TryComp<BibleComponent>(args.Used, out var bible) ||
            !HasComp<BibleUserComponent>(args.User) ||
            !TryComp<UseDelayComponent>(args.Used, out var useDelay) ||
            _useDelay.IsDelayed((args.Used, useDelay)))
            return;

        _useDelay.TryResetDelay(args.Used, false, useDelay);
        _audio.PlayPvs(bible.HealSoundPath, ent);
        EnsureComp<FadingTimedDespawnComponent>(ent).Lifetime = 0f;
        args.Handled = true;
    }
}
