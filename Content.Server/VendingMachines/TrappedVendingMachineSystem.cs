using Content.Shared.Destructible;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Trigger.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.VendingMachines;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.VendingMachines;

public sealed class TrappedVendingMachineSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SleepingSystem _sleepingSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrappedVendingMachineComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<TrappedVendingMachineComponent, BoundUIClosedEvent>(OnUIClosed);
        SubscribeLocalEvent<TrappedVendingMachineComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<TrappedVendingMachineComponent, BreakageEventArgs>(OnBreakage);
        SubscribeLocalEvent<TrappedVendingMachineComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<PullStartedMessage>(OnPullStarted);
    }

    private void OnUIOpened(EntityUid uid, TrappedVendingMachineComponent component, BoundUIOpenedEvent args)
    {
        if (component.TrapState != TrappedVendingMachineState.Normal)
            return;

        if (component.ActiveUser == null)
        {
            component.ActiveUser = args.Actor;
            component.TriggerTime = _timing.CurTime + component.DispenseDelay;
        }
    }

    private void OnUIClosed(EntityUid uid, TrappedVendingMachineComponent component, BoundUIClosedEvent args)
    {
        if (component.ActiveUser == args.Actor)
        {
            component.ActiveUser = null;
            component.TriggerTime = null;
        }
    }

    private void OnDamageChanged(EntityUid uid, TrappedVendingMachineComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null || !args.DamageIncreased)
            return;

        var damageTotal = (float) args.DamageDelta.GetTotal();

        if (component.TrapState == TrappedVendingMachineState.Dragging && component.Victim != null)
        {
            component.DamageSinceDrag += damageTotal;
            if (component.DamageSinceDrag >= component.DamageToRescue)
            {
                ReleaseVictim(uid, component, rescueByDamage: true);
            }
        }
        else if (component.TrapState == TrappedVendingMachineState.Captured && component.InsideVictim != null)
        {
            component.DamageSinceCapture += damageTotal;

            // Transfer 1/5 of the damage to the person inside
            var redirectedDamage = args.DamageDelta * 0.2f;
            _damageableSystem.TryChangeDamage(component.InsideVictim.Value, redirectedDamage, ignoreResistances: true);

            if (component.DamageSinceCapture >= 500f)
            {
                ReleaseCaptured(uid, component);
            }
        }
    }

    private void OnBreakage(EntityUid uid, TrappedVendingMachineComponent component, BreakageEventArgs args)
    {
        if (component.TrapState == TrappedVendingMachineState.Captured)
        {
            ReleaseCaptured(uid, component);
        }
        else if (component.TrapState == TrappedVendingMachineState.Dragging)
        {
            ReleaseVictim(uid, component, rescueByDamage: true);
        }
    }

    private void OnShutdown(EntityUid uid, TrappedVendingMachineComponent component, ComponentShutdown args)
    {
        if (component.TrapState == TrappedVendingMachineState.Captured)
        {
            ReleaseCaptured(uid, component);
        }
        else if (component.TrapState == TrappedVendingMachineState.Dragging)
        {
            ReleaseVictim(uid, component, rescueByDamage: true);
        }
    }

    private void OnPullStarted(PullStartedMessage args)
    {
        var query = EntityQueryEnumerator<TrappedVendingMachineComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.TrapState == TrappedVendingMachineState.Dragging && component.Victim == args.PulledUid)
            {
                ReleaseVictim(uid, component, rescueByPull: true, rescuer: args.PullerUid);
                break;
            }
        }
    }

    private void ReleaseVictim(EntityUid uid, TrappedVendingMachineComponent component, bool rescueByDamage = false, bool rescueByPull = false, EntityUid? rescuer = null, bool rescueByObstruction = false)
    {
        if (component.Victim == null)
            return;

        var victim = component.Victim.Value;
        component.Victim = null;
        component.TrapState = TrappedVendingMachineState.Normal;
        component.ExtraItemStock = 1; // Refill
        component.Dispensed = false;

        _statusEffects.TryRemoveStatusEffect(victim, SleepingSystem.StatusEffectForcedSleeping);
        _sleepingSystem.TryWaking(victim, force: true);

        var machineName = Name(uid);
        var victimName = Name(victim);

        if (rescueByDamage)
        {
            var msg = Loc.GetString("trapped-vending-machine-rescue-damage", ("machine", machineName), ("victim", victimName));
            _popup.PopupEntity(msg, uid, PopupType.Medium);
        }
        else if (rescueByObstruction)
        {
            var msg = Loc.GetString("trapped-vending-machine-rescue-obstructed", ("machine", machineName), ("victim", victimName));
            _popup.PopupEntity(msg, uid, PopupType.Medium);
        }
        else if (rescueByPull && rescuer != null)
        {
            var rescuerName = Name(rescuer.Value);
            var msg = Loc.GetString("trapped-vending-machine-rescue-pull", ("rescuer", rescuerName), ("victim", victimName), ("machine", machineName));
            _popup.PopupEntity(msg, victim, PopupType.Medium);
        }
    }

    private void ReleaseCaptured(EntityUid uid, TrappedVendingMachineComponent component)
    {
        if (component.InsideVictim == null)
            return;

        var victim = component.InsideVictim.Value;
        component.InsideVictim = null;
        component.TrapState = TrappedVendingMachineState.Normal;
        component.ExtraItemStock = 1; // Refill
        component.Dispensed = false;

        var container = _container.EnsureContainer<Container>(uid, "trapped_victim_container");
        _container.Remove(victim, container);

        _statusEffects.TryRemoveStatusEffect(victim, SleepingSystem.StatusEffectForcedSleeping);
        _sleepingSystem.TryWaking(victim, force: true);

        var machineName = Name(uid);
        var victimName = Name(victim);
        var msg = Loc.GetString("trapped-vending-machine-rescue-damage", ("machine", machineName), ("victim", victimName));
        _popup.PopupEntity(msg, uid, PopupType.Medium);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TrappedVendingMachineComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            var curTime = _timing.CurTime;

            switch (component.TrapState)
            {
                case TrappedVendingMachineState.Normal:
                    if (component.ActiveUser != null && component.TriggerTime != null && curTime >= component.TriggerTime.Value)
                    {
                        if (_random.Prob(component.DispenseChance))
                        {
                            TriggerTrap(uid, component);
                        }
                        else
                        {
                            component.TriggerTime = curTime + component.DispenseDelay;
                        }
                    }
                    break;

                case TrappedVendingMachineState.GrenadeExploding:
                    if (component.DragEndTime != null && curTime >= component.DragEndTime.Value)
                    {
                        var victim = FindVictim(uid);
                        if (victim != null)
                        {
                            component.Victim = victim;
                            component.TrapState = TrappedVendingMachineState.Dragging;
                            component.DamageSinceDrag = 0f;

                            _statusEffects.TryAddStatusEffectDuration(victim.Value, SleepingSystem.StatusEffectForcedSleeping, component.SleepDuration);

                            var machineName = Name(uid);
                            var victimName = Name(victim.Value);
                            var msg = Loc.GetString("trapped-vending-machine-drag", ("machine", machineName), ("victim", victimName));
                            _popup.PopupEntity(msg, uid, PopupType.MediumCaution);
                        }
                        else
                        {
                            component.TrapState = TrappedVendingMachineState.Normal;
                            component.ExtraItemStock = 1;
                            component.Dispensed = false;
                        }
                    }
                    break;

                case TrappedVendingMachineState.Dragging:
                    if (component.Victim == null || Deleted(component.Victim.Value) || !_mobStateSystem.IsAlive(component.Victim.Value))
                    {
                        component.Victim = null;
                        component.TrapState = TrappedVendingMachineState.Normal;
                        component.ExtraItemStock = 1;
                        component.Dispensed = false;
                        break;
                    }

                    var victimUid = component.Victim.Value;
                    var machineXform = Transform(uid);
                    var victimXform = Transform(victimUid);

                    var victimWorldPos = _transformSystem.GetWorldPosition(victimXform);
                    var machineWorldPos = _transformSystem.GetWorldPosition(machineXform);
                    var dir = machineWorldPos - victimWorldPos;
                    var distance = dir.Length();

                    if (!_interactionSystem.InRangeUnobstructed(uid, victimUid, distance + 0.5f))
                    {
                        ReleaseVictim(uid, component, rescueByObstruction: true);
                        break;
                    }

                    if (distance <= 1.0f)
                    {
                        if (_interactionSystem.InRangeUnobstructed(uid, victimUid, 1.0f))
                        {
                            CaptureVictim(uid, component, victimUid);
                        }
                        else
                        {
                            ReleaseVictim(uid, component, rescueByObstruction: true);
                        }
                    }
                    else
                    {
                        var step = MathF.Min(distance, component.DragSpeed * frameTime);
                        var newWorldPos = victimWorldPos + dir.Normalized() * step;
                        _transformSystem.SetWorldPosition(victimUid, newWorldPos);
                    }
                    break;

                case TrappedVendingMachineState.Captured:
                    if (component.InsideVictim != null && (Deleted(component.InsideVictim.Value) || !_mobStateSystem.IsAlive(component.InsideVictim.Value)))
                    {
                        component.InsideVictim = null;
                        component.TrapState = TrappedVendingMachineState.Normal;
                        component.ExtraItemStock = 1;
                        component.Dispensed = false;
                    }
                    break;
            }
        }
    }

    private void TriggerTrap(EntityUid uid, TrappedVendingMachineComponent component)
    {
        if (component.ExtraItemStock <= 0)
            return;

        component.ExtraItemStock--;
        component.Dispensed = true;

        var xform = Transform(uid);
        Spawn(component.ExtraItemPrototype, xform.Coordinates);

        _audio.PlayPvs(component.DispenseSound, uid);

        component.TrapState = TrappedVendingMachineState.GrenadeExploding;
        component.DragEndTime = _timing.CurTime + component.GrenadeDelay;

        var grenade = Spawn("SmokeGrenade", xform.Coordinates);
        _triggerSystem.Trigger(grenade, component.ActiveUser, "timer");

        _uiSystem.CloseUi(uid, VendingMachineUiKey.Key);
    }

    private EntityUid? FindVictim(EntityUid uid)
    {
        var xform = Transform(uid);
        var center = xform.Coordinates;
        EntityUid? bestVictim = null;
        var bestDistance = float.MaxValue;

        foreach (var mob in _lookup.GetEntitiesInRange<MobStateComponent>(center, 2.0f))
        {
            var entity = mob.Owner;
            if (entity == uid)
                continue;

            if (!_mobStateSystem.IsAlive(entity))
                continue;

            if (!_interactionSystem.InRangeUnobstructed(uid, entity, 2.0f))
                continue;

            var dist = (_transformSystem.GetMapCoordinates(entity).Position - _transformSystem.GetMapCoordinates(uid, xform).Position).Length();
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestVictim = entity;
            }
        }

        return bestVictim;
    }

    private void CaptureVictim(EntityUid uid, TrappedVendingMachineComponent component, EntityUid victim)
    {
        component.InsideVictim = victim;
        component.Victim = null;
        component.TrapState = TrappedVendingMachineState.Captured;
        component.DamageSinceCapture = 0f;

        var container = _container.EnsureContainer<Container>(uid, "trapped_victim_container");
        _container.Insert(victim, container);

        _audio.PlayPvs(component.CaptureSound, uid);

        var machineName = Name(uid);
        var victimName = Name(victim);
        var msg = Loc.GetString("trapped-vending-machine-capture", ("machine", machineName), ("victim", victimName));
        _popup.PopupEntity(msg, uid, PopupType.LargeCaution);
    }
}
