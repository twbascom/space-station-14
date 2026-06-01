using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared.FeralInfected.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Server system for managing the Feral Infected game rule.
/// Handles role assignment, objective creation, and claw spawning.
/// </summary>
public sealed class FeralInfectedRuleSystem : GameRuleSystem<FeralInfectedRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FeralInfectedRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelected);
    }

    private void OnAntagSelected(Entity<FeralInfectedRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        MakeFeralInfected(args.EntityUid, ent);
    }

    public bool MakeFeralInfected(EntityUid host, FeralInfectedRuleComponent rule)
    {
        if (!_mind.TryGetMind(host, out var mindId, out var mind))
            return false;

        // Give them the infection component (tracks periodic popups)
        var infectedComp = EnsureComp<FeralInfectedComponent>(host);
        
        // Mark their mind with the Feral Infected role
        _roles.MindHasRole<FeralInfectedRoleComponent>(mindId, out var role);
        if (role == null)
        {
            _roles.MindAddRole(mindId, "MindRoleFeralInfected");
        }

        // Send briefing/notification popup
        _popup.PopupEntity(Loc.GetString("feral-infected-infect-popup"), host, host, PopupType.LargeCaution);
        _antag.SendBriefing(host, Loc.GetString("feral-infected-briefing"), null, null);

        // Equip claws in every hand, dropping what was held
        if (TryComp<HandsComponent>(host, out var handsComp))
        {
            foreach (var handName in handsComp.Hands.Keys)
            {
                // Force drop any currently held item
                _hands.TryDrop((host, handsComp), handName, checkActionBlocker: false);

                // Spawn and force-pickup claws to this hand
                var claws = Spawn(rule.ClawsPrototype);
                _hands.TryPickup(host, claws, handName, checkActionBlocker: false, handsComp: handsComp);
            }
        }

        return true;
    }
}
