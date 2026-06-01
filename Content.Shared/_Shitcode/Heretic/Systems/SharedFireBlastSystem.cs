using System.Linq;
using Content.Goobstation.Common.Physics;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared._Shitmed.Damage;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Shitcode.Heretic.Systems;

public abstract class SharedFireBlastSystem : EntitySystem
{
    [Dependency] protected readonly SharedTransformSystem Xform = default!;
    [Dependency] protected readonly StatusEffectsSystem Status = default!;
    [Dependency] protected readonly DamageableSystem Dmg = default!;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStaminaSystem _stam = default!;

    public static readonly EntProtoId FireBlastStatusEffect = "StatusEffectFireBlasted";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireBlastedStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<FireBlastedStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
    }

    private void OnRemoved(Entity<FireBlastedStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (TerminatingOrDeleted(args.Target))
            return;

        RemCompDeferred<FireBlastedComponent>(args.Target);
    }

    private void OnApplied(Entity<FireBlastedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        EnsureComp<FireBlastedComponent>(args.Target);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<FireBlastedComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var blast, out var dmg))
        {
            blast.Accumulator += frameTime;

            if (blast.Accumulator < blast.TickInterval)
                continue;

            blast.Accumulator = 0f;

            UpdateBeams(uid, blast);

            if (blast.Damage == 0f)
                continue;

            var damage = new DamageSpecifier
            {
                DamageDict =
                {
                    { "Heat", blast.Damage },
                },
            };

            Dmg.TryChangeDamage(uid,
                damage,
                true,
                false,
                dmg,
                targetPart: TargetBodyPart.All,
                splitDamage: SplitDamageBehavior.SplitEnsureAll,
                canMiss: false);

            var stamDmg = blast.Damage * blast.StaminaDamageMultiplier;

            _stam.TakeOvertimeStaminaDamage(uid, stamDmg);
        }
    }

    private void UpdateBeams(EntityUid uid, FireBlastedComponent blast, ComplexJointVisualsComponent? visuals = null)
    {
        if (!Resolve(uid, ref visuals, false))
            return;

        var hasFireBeams = false;

        foreach (var (netEnt, _) in visuals.Data.Where(x => x.Value.Id == blast.FireBlastBeamDataId).ToList())
        {
            if (!TryGetEntity(netEnt, out var target) || TerminatingOrDeleted(target) ||
                !Xform.InRange(target.Value, uid, blast.FireBlastRange))
            {
                visuals.Data.Remove(netEnt);
                continue;
            }

            hasFireBeams = true;

            BeamCollision((uid, blast), target.Value);
        }

        Dirty(uid, visuals);

        if (hasFireBeams)
            return;

        blast.ShouldBounce = false;
        Status.TryRemoveStatusEffect(uid, FireBlastStatusEffect);
    }

    protected virtual void BeamCollision(Entity<FireBlastedComponent> origin, EntityUid target)
    {
    }
}
