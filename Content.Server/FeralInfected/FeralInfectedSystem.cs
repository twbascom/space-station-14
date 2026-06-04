using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.FeralInfected.Components;
using Content.Shared.Gibbing;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Content.Shared.Stunnable;
using Content.Server.Polymorph.Systems;
using Content.Server.Chat.Systems;

namespace Content.Server.FeralInfected;

/// <summary>
/// Server-side system for Feral Infected hosts.
/// Handles periodic sanity slip messages, starvation/hunger mechanics, feeding, and ascension.
/// </summary>
public sealed class FeralInfectedSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    private readonly string[] _hungryThoughts =
    {
        "feral-infected-hungry-1",
        "feral-infected-hungry-2",
        "feral-infected-hungry-3",
        "feral-infected-hungry-4"
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FeralClawsComponent, AfterInteractEvent>(OnClawsAfterInteract);
        SubscribeLocalEvent<FeralClawsComponent, FeralFeedDoAfterEvent>(OnFeedDoAfter);
    }

    private void OnClawsAfterInteract(EntityUid uid, FeralClawsComponent component, AfterInteractEvent args)
    {
        if (args.Target == null)
            return;

        var target = args.Target.Value;
        var user = args.User;

        // User must be a Feral Infected host
        if (!HasComp<FeralInfectedComponent>(user))
            return;

        // Target must be a dead humanoid/mob
        if (!TryComp<MobStateComponent>(target, out var mobState) || !_mobState.IsDead(target, mobState))
            return;

        args.Handled = true;

        _popup.PopupEntity(Loc.GetString("feral-infected-eating-start"), user, user, PopupType.Medium);

        var doAfterArgs = new DoAfterArgs(EntityManager, user, 5.0f, new FeralFeedDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnFeedDoAfter(EntityUid uid, FeralClawsComponent component, ref FeralFeedDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        var user = args.User;
        var target = args.Args.Target.Value;

        if (!TryComp<FeralInfectedComponent>(user, out var infected))
            return;

        args.Handled = true;

        // Sound effect
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/bite.ogg"), user);

        // Explode/gib the target corpse
        _gibbing.Gib(target);

        // Reset hunger timer and update devours count
        infected.HungerAccumulator = 0.0f;
        infected.DevouredCount++;

        // Popups
        _popup.PopupEntity(Loc.GetString("feral-infected-eating-success"), user, user, PopupType.Medium);
        
        var otherMessage = Loc.GetString("feral-infected-eating-others", ("user", user));
        _popup.PopupEntity(otherMessage, user, Filter.PvsExcept(user), true, PopupType.MediumCaution);

        // Check for Ascension
        if (infected.DevouredCount >= infected.RequiredDevourForAscension && !infected.Ascended)
        {
            AscendHost(user, infected);
        }
    }

    public void AscendHost(EntityUid host, FeralInfectedComponent infected)
    {
        infected.Ascended = true;

        // Polymorph into the parasite
        var newEntity = _polymorph.PolymorphEntity(host, "FeralParasitePolymorph");

        if (newEntity != null)
        {
            var parasite = newEntity.Value;

            // Mark the new parasite entity as ascended Feral Infected
            var newInfected = EnsureComp<FeralInfectedComponent>(parasite);
            newInfected.Ascended = true;

            // Paralyze/stun the parasite as it finishes morphing
            _stun.TryUpdateParalyzeDuration(parasite, TimeSpan.FromSeconds(4.0f));

            // Grotesque roar sound
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg"), parasite);

            // Play custom parasite ascension music globally and send global announcement
            var music = new SoundPathSpecifier("/Audio/Misc/feral_parasite_ascend.ogg");
            var announcer = Loc.GetString("feral-infected-ascended-announcement-title", ("player", Name(host)));
            var announcement = Loc.GetString("feral-infected-ascended-announcement");
            _chat.DispatchGlobalAnnouncement(announcement, announcer, playSound: true, announcementSound: music, colorOverride: Robust.Shared.Maths.Color.Red);

            // Big caution popup to the host player
            _popup.PopupEntity(Loc.GetString("feral-infected-ascended-popup"), parasite, parasite, PopupType.LargeCaution);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FeralInfectedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Ascended)
                continue;

            comp.HungerAccumulator += frameTime;
            comp.SanityAccumulator += frameTime;

            var isStarving = comp.HungerAccumulator > comp.HungerLimit;
            var currentInterval = isStarving ? _random.NextFloat(8.0f, 15.0f) : comp.PopupInterval;

            if (comp.SanityAccumulator < currentInterval)
                continue;

            comp.SanityAccumulator = 0f;

            // Update standard popup interval for standard state
            if (!isStarving)
            {
                comp.PopupInterval = _random.NextFloat(30.0f, 60.0f);
                var msgIndex = _random.Next(1, 7);
                var locKey = $"feral-infected-sanity-{msgIndex}";
                _popup.PopupEntity(Loc.GetString(locKey), uid, uid, PopupType.MediumCaution);
            }
            else
            {
                // Hungry/starving popups trigger fast
                var msgIndex = _random.Next(0, _hungryThoughts.Length);
                var locKey = _hungryThoughts[msgIndex];
                _popup.PopupEntity(Loc.GetString(locKey), uid, uid, PopupType.LargeCaution);
            }
        }
    }
}
