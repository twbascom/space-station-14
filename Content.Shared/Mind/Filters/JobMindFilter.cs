using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind filter that requires minds to have a specific job.
/// This uses mind roles, not ID cards.
/// </summary>
public sealed partial class JobMindFilter : MindFilter
{
    [DataField(required: true)]
    public ProtoId<JobPrototype> Job;

    [DataField]
    public bool Invert = false;

    protected override bool ShouldRemove(Entity<MindComponent> mind, EntityUid? exclude, IEntityManager entMan, SharedMindSystem mindSys)
    {
        var jobSys = entMan.System<SharedJobSystem>();
        var hasJob = jobSys.MindHasJobWithId(mind, Job);
        return Invert ? !hasJob : hasJob;
    }
}
