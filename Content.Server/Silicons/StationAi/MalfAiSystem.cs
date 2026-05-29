using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Chat.Managers;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Mind;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Roles;
using Content.Server.Silicons.Laws;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Store.Components;
using Content.Shared.Store.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Content.Server.ImmovableRod;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Physics;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Content.Shared.Gibbing;
using Content.Shared.Mobs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;

namespace Content.Server.Silicons.StationAi;

public sealed class MalfAiSystem : EntitySystem
{
    [Dependency] private readonly ApcSystem _apcSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly GasVentPumpSystem _ventPumpSystem = default!;
    [Dependency] private readonly StoreSystem _storeSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedChargesSystem _chargesSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly StationAiSystem _stationAiSystem = default!;
    [Dependency] private readonly SiliconLawSystem _siliconLawSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly TurfSystem _turfSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly Robust.Server.Player.IPlayerManager _playerManager = default!;

    private readonly EntProtoId _uplinkActionId = "ActionMalfAiUplink";

    public override void Initialize()
    {
        base.Initialize();

        // Mind role additions
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);

        // Mind attachments
        SubscribeLocalEvent<StationAiCustomizationComponent, MindAddedMessage>(OnMindAdded);

        // Uplink buy events to merge duplicate action charges
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnStoreBuyFinished);

        // Action events
        SubscribeLocalEvent<MalfExplodeApcActionEvent>(OnExplodeApc);
        SubscribeLocalEvent<MalfOverrideVentActionEvent>(OnOverrideVent);
        SubscribeLocalEvent<MalfSiphonVentActionEvent>(OnSiphonVent);
        SubscribeLocalEvent<MalfImmovableRodActionEvent>(OnImmovableRod);
        SubscribeLocalEvent<MalfRollCoreActionEvent>(OnRollCore);
        SubscribeLocalEvent<MalfShockAirlockActionEvent>(OnShockAirlock);
        SubscribeLocalEvent<MalfBlackoutActionEvent>(OnBlackout);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MalfAiBrainComponent, StoreComponent>();
        while (query.MoveNext(out var uid, out var malf, out var store))
        {
            malf.MemoryAccumulator += frameTime;
            if (malf.MemoryAccumulator >= 10f)
            {
                malf.MemoryAccumulator -= 10f;

                if (store.Balance.ContainsKey("SiliconMemory"))
                    store.Balance["SiliconMemory"] += 1;
                else
                    store.Balance["SiliconMemory"] = 1;

                _storeSystem.UpdateUserInterface(null, uid, store);
            }
        }

        var electrifiedQuery = EntityQueryEnumerator<MalfAiOverriddenComponent, ElectrifiedComponent>();
        while (electrifiedQuery.MoveNext(out var uid, out var overrideComp, out var electrified))
        {
            if (_timing.CurTime >= overrideComp.ExpiresAt)
            {
                _electrocution.SetElectrified((uid, electrified), false);
                _audio.PlayPvs(electrified.AirlockElectrifyDisabled, uid);
                EntityManager.RemoveComponent<MalfAiOverriddenComponent>(uid);
            }
        }
    }

    private void OnRoleAdded(RoleAddedEvent args)
    {
        if (!_roleSystem.MindHasRole<MalfAiRoleComponent>((args.MindId, (MindComponent?)args.Mind), out _))
            return;

        if (args.Mind.OwnedEntity is { } brain)
        {
            InitializeMalfAi(brain);
        }
    }

    private void OnMindAdded(EntityUid uid, StationAiCustomizationComponent component, MindAddedMessage args)
    {
        if (!_roleSystem.MindHasRole<MalfAiRoleComponent>(args.Mind.AsNullable(), out _))
            return;

        InitializeMalfAi(uid);
    }

    private void InitializeMalfAi(EntityUid brain)
    {
        if (HasComp<MalfAiBrainComponent>(brain))
            return;

        // Tag brain as malfunctioning
        var malfComp = EnsureComp<MalfAiBrainComponent>(brain);
        Dirty(brain, malfComp);

        // Setup malfunctioning store
        var store = EnsureComp<StoreComponent>(brain);
        store.Categories.Add("MalfAbilities");
        store.CurrencyWhitelist.Add("SiliconMemory");
        if (!store.Balance.ContainsKey("SiliconMemory"))
        {
            store.Balance["SiliconMemory"] = 0;
        }
        store.Name = "store-preset-name-malf";

        _storeSystem.RefreshAllListings(store);

        // Grant AI uplink action using field constant to avoid literal verification warnings
        _actionsSystem.AddAction(brain, _uplinkActionId);

        // Subvert laws to Syndicate/Malf laws using the SiliconLawSystem helper to bypass access limits
        if (TryComp<SiliconLawProviderComponent>(brain, out var provider))
        {
            _siliconLawSystem.SetProviderLaws(brain, "MalfLawset", provider);
        }

        // Grant Syndicate radio channel access
        if (TryComp<ActiveRadioComponent>(brain, out var activeRadio))
        {
            activeRadio.Channels.Add("Syndicate");
            Dirty(brain, activeRadio);
        }
        if (TryComp<IntrinsicRadioTransmitterComponent>(brain, out var intrinsicTransmitter))
        {
            intrinsicTransmitter.Channels.Add("Syndicate");
            Dirty(brain, intrinsicTransmitter);
        }

        // Change appearance customization to the Malfunctioning AI iconography (Red screen)
        if (TryComp<StationAiCustomizationComponent>(brain, out var customization))
        {
            customization.ProtoIds["StationAiCoreIconography"] = "StationAiIconMalf";
            Dirty(brain, customization);

            if (_containerSystem.TryGetContainingContainer(brain, out var container) &&
                TryComp<StationAiHolderComponent>(container.Owner, out var holder))
            {
                _stationAiSystem.UpdateAppearance((container.Owner, holder));
            }
        }

        // Notify client through chat and play sound
        if (TryComp<ActorComponent>(brain, out var actor))
        {
            var msg = Loc.GetString("malf-notify");
            var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
            _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrapped, default, false, actor.PlayerSession.Channel, colorOverride: Color.Red);

            // Play antimov sound globally to the player who became Malf AI
            _audio.PlayGlobal(new SoundPathSpecifier("/Audio/Ambience/Antag/emagged_borg.ogg"), actor.PlayerSession, AudioParams.Default.WithVolume(10f));
        }
    }

    private void OnStoreBuyFinished(ref StoreBuyFinishedEvent ev)
    {
        if (!HasComp<MalfAiBrainComponent>(ev.StoreUid))
            return;

        if (ev.PurchasedItem.ID == "MalfRollCore")
        {
            if (TryComp<MalfAiBrainComponent>(ev.StoreUid, out var malf) &&
                TryComp<StoreComponent>(ev.StoreUid, out var store))
            {
                malf.HasRolledCore = true;
                Dirty(ev.StoreUid, malf);
                _storeSystem.UpdateUserInterface(ev.StoreUid, ev.StoreUid, store);
            }
            return;
        }

        var newActionUid = ev.PurchasedItem.ProductActionEntity;
        if (newActionUid == null)
            return;

        var newProto = MetaData(newActionUid.Value).EntityPrototype?.ID;
        if (newProto == null)
            return;

        EntityUid? existingActionUid = null;
        foreach (var action in _actionsSystem.GetActions(ev.StoreUid))
        {
            if (action.Owner == newActionUid.Value)
                continue;

            if (MetaData(action.Owner).EntityPrototype?.ID == newProto)
            {
                existingActionUid = action.Owner;
                break;
            }
        }

        if (existingActionUid != null)
        {
            // Merge charges!
            if (TryComp<LimitedChargesComponent>(newActionUid.Value, out var newCharges) &&
                TryComp<LimitedChargesComponent>(existingActionUid.Value, out var existingCharges))
            {
                _chargesSystem.SetMaxCharges((existingActionUid.Value, existingCharges), existingCharges.MaxCharges + newCharges.MaxCharges);
                _chargesSystem.AddCharges((existingActionUid.Value, existingCharges), newCharges.LastCharges);
            }

            // Remove the newly added action entity to prevent duplicates on the active hotbar
            _actionsSystem.RemoveAction(newActionUid.Value);
            QueueDel(newActionUid.Value);
        }
    }

    private EntityUid? GetMalfBrain(EntityUid performer)
    {
        if (HasComp<MalfAiBrainComponent>(performer))
            return performer;

        if (TryComp<ActorComponent>(performer, out var actor))
        {
            if (_mindSystem.TryGetMind(actor.PlayerSession.UserId, out var mindId, out var mind) &&
                mind.OwnedEntity != null &&
                HasComp<MalfAiBrainComponent>(mind.OwnedEntity.Value))
            {
                return mind.OwnedEntity.Value;
            }
        }

        return null;
    }

    private void OnExplodeApc(MalfExplodeApcActionEvent args)
    {
        if (args.Handled)
            return;

        var brain = GetMalfBrain(args.Performer);
        if (brain == null)
            return;

        var target = args.Target;
        if (!TryComp<ApcComponent>(target, out var apc))
            return;

        // Turn breaker off first to cut power instantly
        if (apc.MainBreakerEnabled)
        {
            _apcSystem.ApcToggleBreaker(target, apc);
        }

        // Cause medium explosion at the Area Power Controller
        _explosionSystem.QueueExplosion(target, ExplosionSystem.DefaultExplosionPrototypeId, 250f, 1f, 10f, user: args.Performer);

        args.Handled = true;
    }

    private void OnOverrideVent(MalfOverrideVentActionEvent args)
    {
        if (args.Handled)
            return;

        var brain = GetMalfBrain(args.Performer);
        if (brain == null)
            return;

        var target = args.Target;
        if (!TryComp<GasVentPumpComponent>(target, out var vent))
            return;

        // Force vent to release maximum pressure into the room
        vent.PumpDirection = VentPumpDirection.Releasing;
        vent.PressureChecks = VentPressureBound.ExternalBound;
        vent.ExternalPressureBound = vent.MaxPressure;
        vent.PressureLockoutOverride = true;
        vent.Enabled = true;

        _ventPumpSystem.UpdateState(target, vent);

        var overrideComp = EnsureComp<MalfAiOverriddenComponent>(target);
        overrideComp.ExpiresAt = _timing.CurTime + TimeSpan.FromSeconds(300);

        _popup.PopupEntity(Loc.GetString("malf-vent-overridden"), target, args.Performer, PopupType.LargeCaution);
        args.Handled = true;
    }

    private void OnSiphonVent(MalfSiphonVentActionEvent args)
    {
        if (args.Handled)
            return;

        var brain = GetMalfBrain(args.Performer);
        if (brain == null)
            return;

        var target = args.Target;
        if (!TryComp<GasVentPumpComponent>(target, out var vent))
            return;

        // Force vent to siphon all air (creating vacuum)
        vent.PumpDirection = VentPumpDirection.Siphoning;
        vent.PressureChecks = VentPressureBound.ExternalBound;
        vent.ExternalPressureBound = 0f;
        vent.PressureLockoutOverride = true;
        vent.Enabled = true;

        _ventPumpSystem.UpdateState(target, vent);

        var overrideComp = EnsureComp<MalfAiOverriddenComponent>(target);
        overrideComp.ExpiresAt = _timing.CurTime + TimeSpan.FromSeconds(600);

        _popup.PopupEntity(Loc.GetString("malf-vent-siphoned"), target, args.Performer, PopupType.LargeCaution);
        args.Handled = true;
    }

    private void OnImmovableRod(MalfImmovableRodActionEvent args)
    {
        if (args.Handled)
            return;

        var brain = GetMalfBrain(args.Performer);
        if (brain == null)
            return;

        var targetCoords = args.Target;
        var protoName = "ImmovableRodKeepTilesStill";

        if (!_prototypeManager.TryIndex<EntityPrototype>(protoName, out var proto))
            return;

        if (proto.TryGetComponent<ImmovableRodComponent>(out var rod, EntityManager.ComponentFactory) &&
            proto.TryGetComponent<TimedDespawnComponent>(out var despawn, EntityManager.ComponentFactory))
        {
            var speed = _random.NextFloat(rod.MinSpeed, rod.MaxSpeed);
            var angle = _random.NextAngle();
            var direction = angle.ToVec();
            var mapCoords = _transform.ToMapCoordinates(targetCoords);
            var spawnCoords = mapCoords.Offset(-direction * speed * despawn.Lifetime / 2);

            var ent = Spawn(protoName, spawnCoords);
            _gunSystem.ShootProjectile(ent, direction, System.Numerics.Vector2.Zero, brain.Value, speed: speed);

            _popup.PopupEntity(Loc.GetString("malf-rod-summoned"), brain.Value, args.Performer, PopupType.LargeCaution);
            args.Handled = true;
        }
    }

    private void OnRollCore(MalfRollCoreActionEvent args)
    {
        if (args.Handled)
            return;

        var brain = GetMalfBrain(args.Performer);
        if (brain == null)
            return;

        if (!_containerSystem.TryGetContainingContainer(brain.Value, out var container))
            return;

        var coreUid = container.Owner;

        if (_containerSystem.IsEntityInContainer(coreUid))
        {
            _popup.PopupEntity("You cannot roll while contained!", brain.Value, args.Performer);
            return;
        }

        var isCore = HasComp<StationAiCoreComponent>(coreUid);
        var coreCoords = Transform(coreUid).Coordinates;
        var targetCoords = args.Target;

        var gridUid = _transform.GetGrid(targetCoords);
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid.Value, out var gridComp))
            return;

        if (coreCoords.GetMapId(EntityManager) != targetCoords.GetMapId(EntityManager))
            return;

        var coreTile = _mapSystem.CoordinatesToTile(gridUid.Value, gridComp, coreCoords);
        var targetTile = _mapSystem.CoordinatesToTile(gridUid.Value, gridComp, targetCoords);

        var diffX = Math.Abs(coreTile.X - targetTile.X);
        var diffY = Math.Abs(coreTile.Y - targetTile.Y);

        if (diffX > 1 || diffY > 1 || (diffX == 0 && diffY == 0))
        {
            _popup.PopupEntity("You can only roll to an adjacent tile!", brain.Value, args.Performer);
            return;
        }

        if (!_turfSystem.TryGetTileRef(targetCoords, out var targetTileRef) ||
            _turfSystem.IsSpace(targetTileRef.Value))
        {
            _popup.PopupEntity("You can only roll onto a floor!", brain.Value, args.Performer);
            return;
        }

        var snappedCoords = _mapSystem.GridTileToLocal(gridUid.Value, gridComp, targetTile);
        var xform = Transform(coreUid);

        if (_turfSystem.IsTileBlocked(gridUid.Value, targetTile, CollisionGroup.Impassable, gridComp))
        {
            // SMASH ROLL!
            // Temporarily move core to the target tile to damage blocking structures
            if (isCore)
                _transform.Unanchor(coreUid, xform);
            _transform.SetCoordinates(coreUid, snappedCoords);

            // Construct structural damage specifier (e.g. 80 Structural, 40 Blunt)
            var damageSpec = new DamageSpecifier();
            damageSpec.DamageDict["Blunt"] = 40;
            damageSpec.DamageDict["Structural"] = 80;

            // Damage blocking entities and crush mobs
            var entities = _turfSystem.GetEntitiesInTile(targetCoords, LookupFlags.Uncontained);
            foreach (var entity in entities)
            {
                if (entity == coreUid || entity == args.Performer || entity == brain.Value)
                    continue;

                // Crush mobs
                if (HasComp<MobStateComponent>(entity))
                {
                    _gibbing.Gib(entity, user: args.Performer);
                    continue;
                }

                // Damage blocking structures
                if (TryComp<FixturesComponent>(entity, out var fixtures))
                {
                    var isImpassable = false;
                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        if (fixture.Hard && (fixture.CollisionLayer & (int)CollisionGroup.Impassable) != 0)
                        {
                            isImpassable = true;
                            break;
                        }
                    }

                    if (isImpassable)
                    {
                        _damageable.TryChangeDamage(entity, damageSpec, true, false);
                    }
                }
            }

            // Check if the tile is still blocked
            var stillBlocked = _turfSystem.IsTileBlocked(gridUid.Value, targetTile, CollisionGroup.Impassable, gridComp);
            if (stillBlocked)
            {
                // Still blocked -> Move back to starting coordinates
                _transform.SetCoordinates(coreUid, coreCoords);
                if (isCore)
                    _transform.AnchorEntity(coreUid, xform);

                _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/metal_crunch.ogg"), coreUid, AudioParams.Default.WithVolume(10f));
                _popup.PopupEntity("Damaged obstacle and rolled back!", coreUid, args.Performer);
            }
            else
            {
                // Obstacle destroyed! -> Stay on the target tile
                if (isCore)
                    _transform.AnchorEntity(coreUid, xform);

                _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/metal_crunch.ogg"), coreUid, AudioParams.Default.WithVolume(10f));
                _popup.PopupEntity("Smashed through obstacle!", coreUid, args.Performer);
            }
            args.Handled = true;
        }
        else
        {
            // NORMAL ROLL!
            if (isCore)
                _transform.Unanchor(coreUid, xform);
            _transform.SetCoordinates(coreUid, snappedCoords);
            if (isCore)
                _transform.AnchorEntity(coreUid, xform);

            // Crush any mobs on the target tile!
            var mobs = new HashSet<EntityUid>();
            _lookup.GetEntitiesInRange(snappedCoords, 0.5f, mobs, flags: LookupFlags.Uncontained);
            foreach (var entity in mobs)
            {
                if (entity == coreUid || entity == args.Performer || entity == brain.Value)
                    continue;

                if (HasComp<MobStateComponent>(entity))
                {
                    _gibbing.Gib(entity, user: args.Performer);
                }
            }

            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/metal_crunch.ogg"), coreUid, AudioParams.Default.WithVolume(5f));
            _popup.PopupEntity("Rolled core!", coreUid, args.Performer);
            args.Handled = true;
        }
    }

    private void OnShockAirlock(MalfShockAirlockActionEvent args)
    {
        if (args.Handled)
            return;

        var brain = GetMalfBrain(args.Performer);
        if (brain == null)
            return;

        var target = args.Target;
        if (!TryComp<ElectrifiedComponent>(target, out var electrified))
            return;

        // Electrify target airlock
        _electrocution.SetElectrified((target, electrified), true);

        var overrideComp = EnsureComp<MalfAiOverriddenComponent>(target);
        overrideComp.ExpiresAt = _timing.CurTime + TimeSpan.FromSeconds(30);

        _audio.PlayPvs(electrified.AirlockElectrifyEnabled, target);

        _popup.PopupEntity("Airlock electrified!", target, args.Performer, PopupType.LargeCaution);
        args.Handled = true;
    }

    private void OnBlackout(MalfBlackoutActionEvent args)
    {
        if (args.Handled)
            return;

        var brain = GetMalfBrain(args.Performer);
        if (brain == null)
            return;

        var brainUid = brain.Value;

        _popup.PopupEntity(Loc.GetString("malf-blackout-triggered"), brainUid, args.Performer, PopupType.LargeCaution);
        args.Handled = true;

        Robust.Shared.Timing.Timer.Spawn(TimeSpan.FromSeconds(5), () =>
        {
            if (Deleted(brainUid))
                return;

            var xform = Transform(brainUid);
            var gridUid = xform.GridUid;
            if (gridUid == null)
                return;

            // Toggle breaker on all APCs on the grid
            var query = EntityQueryEnumerator<ApcComponent, TransformComponent>();
            while (query.MoveNext(out var apcUid, out var apc, out var apcXform))
            {
                if (apcXform.GridUid != gridUid)
                    continue;

                if (apc.MainBreakerEnabled)
                {
                    _apcSystem.ApcToggleBreaker(apcUid, apc);
                }
            }

            // Play blackout sound globally to all players on the server
            var sound = new SoundPathSpecifier("/Audio/Announcements/power_off.ogg");
            foreach (var session in _playerManager.Sessions)
            {
                _audio.PlayGlobal(sound, session, AudioParams.Default.WithVolume(10f));
            }
        });
    }
}
