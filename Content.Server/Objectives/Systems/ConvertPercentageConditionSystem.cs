using Content.Server.Objectives.Components;
using Content.Server.Revolutionary.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles progress for the ConvertPercentageConditionComponent objective.
/// </summary>
public sealed class ConvertPercentageConditionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConvertPercentageConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, ConvertPercentageConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var total = 0;
        var converted = 0;

        var minds = AllEntityQuery<MindComponent>();
        while (minds.MoveNext(out var mindId, out var mind))
        {
            if (mind.OwnedEntity is not { } mob)
                continue;

            // Must be alive
            if (!_mobState.IsAlive(mob))
                continue;

            // Must be humanoid (or always convertible)
            if (!HasComp<HumanoidAppearanceComponent>(mob) && !HasComp<AlwaysRevolutionaryConvertibleComponent>(mob))
                continue;

            // Check department restriction
            if (comp.Department != null)
            {
                if (!_prototype.TryIndex(comp.Department.Value, out var dept))
                    continue;

                if (!_job.MindTryGetJobId(mindId, out var jobId) || jobId == null || !dept.Roles.Contains(jobId.Value))
                    continue;
            }
            else
            {
                // Station-wide: exclude command staff
                if (HasComp<CommandStaffComponent>(mob))
                    continue;
            }

            total++;

            if (HasComp<RevolutionaryComponent>(mob) || HasComp<HeadRevolutionaryComponent>(mob))
            {
                converted++;
            }
        }

        if (total == 0)
        {
            args.Progress = 1f;
            return;
        }

        var targetCount = total * comp.Percentage;
        if (targetCount <= 0)
        {
            args.Progress = 1f;
            return;
        }

        args.Progress = Math.Min(1f, converted / targetCount);
    }
}
