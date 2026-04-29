using Content.Shared.Explosion.Components;
using Content.Shared.Trigger;
using Content.Shared.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Throwing;
using Content.Server.Fluids.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Server.Explosion.EntitySystems;

public sealed class FunnyBombSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FunnyBombComponent, TriggerEvent>(OnTrigger);
        SubscribeLocalEvent<FunnyBombComponent, ActiveTimerTriggerEvent>(OnActiveTimer);
    }

    private void OnActiveTimer(Entity<FunnyBombComponent> entity, ref ActiveTimerTriggerEvent args)
    {
        if (args.User == null)
            return;

        _popup.PopupEntity(Loc.GetString("funny-bomb-activate-text"), entity, args.User.Value);
    }

    private void OnTrigger(Entity<FunnyBombComponent> entity, ref TriggerEvent args)
    {
        if (args.Key != entity.Comp.TriggerKey)
            return;

        entity.Comp.IsTriggered = true;
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FunnyBombComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.IsTriggered)
                continue;

            SpawnFunnyItems(uid, component);
            QueueDel(uid);
        }
    }

    private void SpawnFunnyItems(EntityUid uid, FunnyBombComponent component)
    {
        var coords = _transformSystem.GetMapCoordinates(uid);
        
        if (_random.Prob(component.SoapProbability))
        {
            // Many soap!
            for (var i = 0; i < component.SoapCount; i++)
            {
                var spawned = SpawnAtRandom(component.SoapPrototype, coords);
                ThrowItem(spawned);
            }
        }
        else if (_random.Prob(component.LubeProbability))
        {
            // Space lube puddle
            var solution = new Solution("SpaceLube", FixedPoint2.New(70));
            _puddleSystem.TrySpillAt(uid, solution, out _);
        }
        else
        {
            // Banana pieces
            for (var i = 0; i < component.BananaCount; i++)
            {
                var spawned = SpawnAtRandom(component.BananaPrototype, coords);
                ThrowItem(spawned);
            }
        }
    }

    private EntityUid SpawnAtRandom(string prototype, MapCoordinates coords)
    {
        var offset = new Vector2(_random.NextFloat(-0.5f, 0.5f), _random.NextFloat(-0.5f, 0.5f));
        return Spawn(prototype, coords.Offset(offset));
    }

    private void ThrowItem(EntityUid item)
    {
        var angle = _random.NextAngle();
        var direction = angle.ToVec();
        _throwingSystem.TryThrow(item, direction, baseThrowSpeed: 5f);
    }
}
