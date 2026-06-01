using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Revolutionary.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles progress for the ConvertPersonConditionComponent objective.
/// </summary>
public sealed class ConvertPersonConditionSystem : EntitySystem
{
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConvertPersonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, ConvertPersonConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = (HasComp<RevolutionaryComponent>(target) || HasComp<HeadRevolutionaryComponent>(target)) ? 1f : 0f;
    }
}
