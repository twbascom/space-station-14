// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Ilya246 <ilyukarno@gmail.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 whateverusername0 <whateveremail>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server._Goobstation.Objectives.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body;
using Content.Server.Heretic.Components;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid;
using Content.Server.Revolutionary.Components;
using Content.Shared.Mind;
using Content.Shared.Heretic;
using Content.Server.Heretic.EntitySystems;
using Content.Shared.Gibbing;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Access.Systems;
using Content.Server.Access.Components;
using Content.Shared.PDA;
using Robust.Shared.Containers;

namespace Content.Server.Heretic.Ritual;

/// <summary>
///     Checks for a nearest dead body,
///     gibs it and gives the heretic knowledge points.
/// </summary>
// these classes should be lead out and shot
[Virtual] public partial class RitualSacrificeBehavior : RitualCustomBehavior
{
    /// <summary>
    ///     Minimal amount of corpses.
    /// </summary>
    [DataField]
    public float Min = 1;

    /// <summary>
    ///     Maximum amount of corpses.
    /// </summary>
    [DataField]
    public float Max = 1;

    /// <summary>
    ///     Should we count only targets?
    /// </summary>
    [DataField]
    public bool OnlyTargets;

    /// <summary>
    ///     Should we count only humanoids?
    /// </summary>
    [DataField]
    public bool OnlyHumanoid = true;

    // this is awful but it works so i'm not complaining
    protected SharedMindSystem _mind = default!;
    protected HereticSystem _heretic = default!;
    protected BodySystem _body = default!;
    protected EntityLookupSystem _lookup = default!;
    [Dependency] protected IPrototypeManager _proto = default!;
    [Dependency] protected ILogManager _log = default!;

    private ISawmill? _sawmill;

    protected List<EntityUid> uids = new();

    public override bool Execute(RitualData args, out string? outstr)
    {
        _mind = args.EntityManager.System<SharedMindSystem>();
        _heretic = args.EntityManager.System<HereticSystem>();
        _body = args.EntityManager.System<BodySystem>();
        _lookup = args.EntityManager.System<EntityLookupSystem>();
        _proto = IoCManager.Resolve<IPrototypeManager>();
        _log = IoCManager.Resolve<ILogManager>();

        uids = new();

        if (!args.EntityManager.TryGetComponent<HereticComponent>(args.Performer, out var hereticComp))
        {
            outstr = string.Empty;
            return false;
        }

        var lookup = _lookup.GetEntitiesInRange(args.Platform, 1.5f);
        if (lookup.Count == 0)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice");
            return false;
        }

        // get all the dead ones
        foreach (var look in lookup)
        {
            if (!args.EntityManager.TryGetComponent<MobStateComponent>(look, out var mobstate) // only mobs
            || OnlyHumanoid && !args.EntityManager.HasComponent<HumanoidAppearanceComponent>(look) // only humans
            || args.EntityManager.HasComponent<BorgChassisComponent>(look) // no borgs
            || OnlyTargets
                && hereticComp.SacrificeTargets.All(x => x.Entity != args.EntityManager.GetNetEntity(look)) // only targets
                && !args.EntityManager.HasComponent<HereticComponent>(look)) // or other heretics
                continue;

            if (mobstate.CurrentState != Shared.Mobs.MobState.Alive)
                uids.Add(look);
        }

        if (uids.Count < Min)
        {
            if (Min > 1)
            {
                outstr = Loc.GetString("heretic-ritual-fail-sacrifice-ineligible-count", ("min", Min), ("actual", uids.Count));
            }
            else
            {
                outstr = Loc.GetString("heretic-ritual-fail-sacrifice-ineligible");
            }
            return false;
        }

        outstr = null;
        return true;
    }

    public override void Finalize(RitualData args)
    {
        if (!args.EntityManager.TryGetComponent(args.Performer, out HereticComponent? heretic))
        {
            uids = new();
            return;
        }

        var jobSystem = args.EntityManager.System<SharedJobSystem>();
        var accessReader = args.EntityManager.System<AccessReaderSystem>();

        var knowledgeGain = 0f;
        for (var i = 0; i < Max && i < uids.Count; i++)
        {
            if (!args.EntityManager.EntityExists(uids[i]))
                continue;

            var uid = uids[i];

            var isCommand = args.EntityManager.HasComponent<CommandStaffComponent>(uid);
            Robust.Shared.Log.Logger.Info($"[HereticSacrifice] Finalizing sacrifice of victim: {args.EntityManager.ToPrettyString(uid)}. Has CommandStaffComponent: {isCommand}");

            if (!isCommand)
            {
                if (_mind.TryGetMind(uid, out var victimMindId, out _) && jobSystem.MindTryGetJobId(victimMindId, out var jobId) && jobId != null)
                {
                    var jobProto = jobId.Value.Id;
                    Robust.Shared.Log.Logger.Info($"[HereticSacrifice] Victim has mind {victimMindId} and job ID {jobProto}");
                    if (jobProto == "Captain" || jobProto == "HeadOfPersonnel" || jobProto == "HeadOfSecurity" ||
                        jobProto == "ChiefEngineer" || jobProto == "ChiefMedicalOfficer" || jobProto == "ResearchDirector" ||
                        jobProto == "Quartermaster")
                    {
                        isCommand = true;
                    }
                }

                if (!isCommand)
                {
                    var containerSys = args.EntityManager.System<SharedContainerSystem>();
                    var visited = new HashSet<EntityUid>();
                    if (CheckIsCommandRecursively(uid, containerSys, accessReader, args.EntityManager, visited))
                    {
                        isCommand = true;
                    }
                }

                if (!isCommand)
                {
                    // Check if there is any command ID card or PDA lying on the ground on/near the rune (within 1.5m)
                    var lookup = args.EntityManager.System<EntityLookupSystem>().GetEntitiesInRange(args.Platform, 1.5f);
                    foreach (var entity in lookup)
                    {
                        if (IsCommandIdCardOrPda(entity, accessReader, args.EntityManager))
                        {
                            Robust.Shared.Log.Logger.Info($"[HereticSacrifice] Found command ID card/PDA on the ground: {args.EntityManager.ToPrettyString(entity)}");
                            isCommand = true;
                            break;
                        }
                    }
                }
            }

            Robust.Shared.Log.Logger.Info($"[HereticSacrifice] Final isCommand value: {isCommand}");

            var isSec = args.EntityManager.HasComponent<SecurityStaffComponent>(uid);
            if (!isSec)
            {
                if (_mind.TryGetMind(uid, out var victimMindIdSec, out _) && jobSystem.MindTryGetJobId(victimMindIdSec, out var jobIdSec) && jobIdSec != null)
                {
                    var jobProto = jobIdSec.Value.Id;
                    if (jobProto == "HeadOfSecurity" || jobProto == "Warden" || jobProto == "Detective" ||
                        jobProto == "SecurityOfficer" || jobProto == "SecurityCadet")
                    {
                        isSec = true;
                    }
                }

                if (!isSec)
                {
                    var accessTags = accessReader.FindAccessTags(uid);
                    if (accessTags.Any(x => x.Id == "Security"))
                    {
                        isSec = true;
                    }
                }
            }

            var isHeretic = args.EntityManager.HasComponent<HereticComponent>(uid);
            knowledgeGain +=
                isHeretic ||
                heretic.SacrificeTargets.Any(x => x.Entity == args.EntityManager.GetNetEntity(uid))
                    ? isCommand || isSec || isHeretic ? 3f : 2f
                    : 1f;

            try
            {
                // YES!!! GIB!!!
                _body.GibBody(uid);
            }
            catch (Exception e)
            {
                if (!args.EntityManager.IsQueuedForDeletion(uid) && !args.EntityManager.Deleted(uid))
                    args.EntityManager.QueueDeleteEntity(uid);

                _sawmill ??= _log.GetSawmill("sacrifice");
                _sawmill.Error(e.Message);
            }

            // update objectives
            if (_mind.TryGetMind(args.Performer, out var mindId, out var mind))
            {
                // this is godawful dogshit. but it works :)
                if (_mind.TryFindObjective((mindId, mind), "HereticSacrificeObjective", out var crewObj)
                && args.EntityManager.TryGetComponent<HereticSacrificeConditionComponent>(crewObj, out var crewObjComp))
                    crewObjComp.Sacrificed += 1;

                if (_mind.TryFindObjective((mindId, mind), "HereticSacrificeHeadObjective", out var crewHeadObj)
                && args.EntityManager.TryGetComponent<HereticSacrificeConditionComponent>(crewHeadObj, out var crewHeadObjComp)
                && isCommand)
                    crewHeadObjComp.Sacrificed += 1;
            }
        }

        if (knowledgeGain > 0)
            _heretic.UpdateKnowledge(args.Performer, heretic, knowledgeGain);

        // reset it because it refuses to work otherwise.
        uids = new();
        args.EntityManager.EventBus.RaiseLocalEvent(args.Performer, new EventHereticUpdateTargets());
    }

    private bool IsCommandIdCardOrPda(EntityUid uid, AccessReaderSystem accessReader, IEntityManager entMan)
    {
        // Check PresetIdCardComponent
        if (entMan.TryGetComponent<PresetIdCardComponent>(uid, out var presetId) && presetId.JobName != null)
        {
            var job = presetId.JobName;
            if (job == "Captain" || job == "HeadOfPersonnel" || job == "HeadOfSecurity" ||
                job == "ChiefEngineer" || job == "ChiefMedicalOfficer" || job == "ResearchDirector" ||
                job == "Quartermaster")
            {
                return true;
            }
        }

        // Check PDA's contained ID card
        if (entMan.TryGetComponent<PdaComponent>(uid, out var pda) && pda.ContainedId != null)
        {
            if (IsCommandIdCardOrPda(pda.ContainedId.Value, accessReader, entMan))
                return true;
        }

        // Check access tags on the entity itself
        var accessTags = accessReader.FindAccessTags(uid);
        if (accessTags.Any(x => x.Id == "Command"))
            return true;

        return false;
    }

    private bool CheckIsCommandRecursively(EntityUid parent, SharedContainerSystem containerSys, AccessReaderSystem accessReader, IEntityManager entMan, HashSet<EntityUid> visited)
    {
        if (!visited.Add(parent))
            return false;

        if (IsCommandIdCardOrPda(parent, accessReader, entMan))
            return true;

        if (entMan.TryGetComponent<ContainerManagerComponent>(parent, out var containerManager) && containerManager.Containers != null)
        {
            foreach (var container in containerManager.Containers.Values)
            {
                if (container?.ContainedEntities == null)
                    continue;

                foreach (var child in container.ContainedEntities)
                {
                    if (CheckIsCommandRecursively(child, containerSys, accessReader, entMan, visited))
                        return true;
                }
            }
        }

        return false;
    }
}
