using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Cloning.Events;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Physics;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Movement.Systems;

public sealed partial class SharedJumpAbilitySystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JumpAbilityComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<JumpAbilityComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<JumpAbilityComponent, GravityJumpEvent>(OnGravityJump);

        SubscribeLocalEvent<ActiveLeaperComponent, StartCollideEvent>(OnLeaperCollide);
        SubscribeLocalEvent<ActiveLeaperComponent, LandEvent>(OnLeaperLand);
        SubscribeLocalEvent<ActiveLeaperComponent, StopThrowEvent>(OnLeaperStopThrow);
        SubscribeLocalEvent<UnstoppableLeaperComponent, LandEvent>(OnUnstoppableLand);
        SubscribeLocalEvent<UnstoppableLeaperComponent, StopThrowEvent>(OnUnstoppableStopThrow);

        SubscribeLocalEvent<UnstoppableLeaperComponent, ComponentShutdown>(OnUnstoppableShutdown);

        SubscribeLocalEvent<JumpAbilityComponent, CloningEvent>(OnClone);
    }

    private void OnInit(Entity<JumpAbilityComponent> entity, ref MapInitEvent args)
    {
        if (!TryComp(entity, out ActionsComponent? comp))
            return;

        _actions.AddAction(entity, ref entity.Comp.ActionEntity, entity.Comp.Action, component: comp);
    }

    private void OnShutdown(Entity<JumpAbilityComponent> entity, ref ComponentShutdown args)
    {
        _actions.RemoveAction(entity.Owner, entity.Comp.ActionEntity);
    }

    private void OnLeaperCollide(Entity<ActiveLeaperComponent> entity, ref StartCollideEvent args)
    {
        if (HasComp<UnstoppableLeaperComponent>(entity))
            return;

        _stun.TryKnockdown(entity.Owner, entity.Comp.KnockdownDuration, force: true);
        RemCompDeferred<ActiveLeaperComponent>(entity);
    }

    private void OnLeaperLand(Entity<ActiveLeaperComponent> entity, ref LandEvent args)
    {
        RemCompDeferred<ActiveLeaperComponent>(entity);
    }

    private void OnLeaperStopThrow(Entity<ActiveLeaperComponent> entity, ref StopThrowEvent args)
    {
        RemCompDeferred<ActiveLeaperComponent>(entity);
    }

    private void OnUnstoppableLand(Entity<UnstoppableLeaperComponent> entity, ref LandEvent args)
    {
        RemCompDeferred<UnstoppableLeaperComponent>(entity);
    }

    private void OnUnstoppableStopThrow(Entity<UnstoppableLeaperComponent> entity, ref StopThrowEvent args)
    {
        RemCompDeferred<UnstoppableLeaperComponent>(entity);
    }

    private void OnGravityJump(Entity<JumpAbilityComponent> entity, ref GravityJumpEvent args)
    {
        if (_gravity.IsWeightless(args.Performer) || _standing.IsDown(args.Performer))
        {
            if (entity.Comp.JumpFailedPopup != null)
                _popup.PopupClient(Loc.GetString(entity.Comp.JumpFailedPopup.Value), args.Performer, args.Performer);
            return;
        }

        var xform = Transform(args.Performer);
        var throwing = xform.LocalRotation.ToWorldVec() * entity.Comp.JumpDistance;
        var direction = xform.Coordinates.Offset(throwing); // to make the character jump in the direction he's looking

        _throwing.TryThrow(args.Performer, direction, entity.Comp.JumpThrowSpeed);

        _audio.PlayPredicted(entity.Comp.JumpSound, args.Performer, args.Performer);

        if (entity.Comp.CanCollide)
        {
            EnsureComp<ActiveLeaperComponent>(entity, out var leaperComp);
            leaperComp.KnockdownDuration = entity.Comp.CollideKnockdown;
            Dirty(entity.Owner, leaperComp);
        }

        if (entity.Comp.Unstoppable && TryComp<PhysicsComponent>(args.Performer, out var physics) && TryComp<FixturesComponent>(args.Performer, out var fixtures))
        {
            var unstoppable = EnsureComp<UnstoppableLeaperComponent>(args.Performer);
            unstoppable.OldLayer = physics.CollisionLayer;
            unstoppable.OldMask = physics.CollisionMask;

            foreach (var (id, fixture) in fixtures.Fixtures)
            {
                _physics.SetCollisionLayer(args.Performer, id, fixture, (int) CollisionGroup.FlyingMobLayer, manager: fixtures, body: physics);
                _physics.SetCollisionMask(args.Performer, id, fixture, (int) CollisionGroup.FlyingMobMask, manager: fixtures, body: physics);
            }
            Dirty(args.Performer, unstoppable);
        }

        args.Handled = true;
    }

    private void OnUnstoppableShutdown(Entity<UnstoppableLeaperComponent> entity, ref ComponentShutdown args)
    {
        if (!TryComp<PhysicsComponent>(entity, out var physics) || !TryComp<FixturesComponent>(entity, out var fixtures))
            return;

        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            _physics.SetCollisionLayer(entity, id, fixture, entity.Comp.OldLayer, manager: fixtures, body: physics);
            _physics.SetCollisionMask(entity, id, fixture, entity.Comp.OldMask, manager: fixtures, body: physics);
        }
    }

    private void OnClone(Entity<JumpAbilityComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        var targetComp = Factory.GetComponent<JumpAbilityComponent>();
        targetComp.Action = ent.Comp.Action;
        targetComp.CanCollide = ent.Comp.CanCollide;
        targetComp.JumpSound = ent.Comp.JumpSound;
        targetComp.CollideKnockdown = ent.Comp.CollideKnockdown;
        targetComp.JumpDistance = ent.Comp.JumpDistance;
        targetComp.JumpThrowSpeed = ent.Comp.JumpThrowSpeed;
        AddComp(args.CloneUid, targetComp, true);
    }
}
