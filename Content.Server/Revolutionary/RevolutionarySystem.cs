using Content.Server.Popups;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Revolutionary;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Zombies;

namespace Content.Server.Revolutionary;

/// <summary>
/// Server-side implementation of the Revolutionary system.
/// Handles the propaganda action and conversion progress.
/// </summary>
public sealed class RevolutionarySystem : SharedRevolutionarySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpreadPropagandaActionEvent>(OnSpreadPropaganda);
    }

    private void OnSpreadPropaganda(SpreadPropagandaActionEvent args)
    {
        var performer = args.Performer;
        if (!HasComp<HeadRevolutionaryComponent>(performer))
            return;

        var xform = Transform(performer);
        var range = 4.0f;
        var targets = _lookup.GetEntitiesInRange(xform.Coordinates, range);

        _popup.PopupEntity(Loc.GetString("rev-propaganda-spread"), performer, performer);

        foreach (var target in targets)
        {
            if (target == performer)
                continue;

            var alwaysConvertible = HasComp<AlwaysRevolutionaryConvertibleComponent>(target);

            if (!_mind.TryGetMind(target, out var mindId, out var mind) && !alwaysConvertible)
                continue;

            if (HasComp<RevolutionaryComponent>(target) ||
                HasComp<HeadRevolutionaryComponent>(target) ||
                (!HasComp<HumanoidAppearanceComponent>(target) && !alwaysConvertible) ||
                !_mobState.IsAlive(target) ||
                HasComp<ZombieComponent>(target))
            {
                continue;
            }

            var conv = EnsureComp<RevolutionaryConversionComponent>(target);
            conv.Progress += 25f;

            if (conv.Progress >= conv.MaxProgress)
            {
                if (HasComp<MindShieldComponent>(target))
                {
                    _popup.PopupEntity(Loc.GetString("rev-propaganda-shielded"), target, target, PopupType.LargeCaution);
                    conv.Progress = 0f;
                }
                else
                {
                    conv.Progress = 0f;
                    var ev = new RevolutionaryConvertEvent(target, performer);
                    RaiseLocalEvent(ev);
                }
            }
            else
            {
                Dirty(target, conv);
            }
        }
    }
}
