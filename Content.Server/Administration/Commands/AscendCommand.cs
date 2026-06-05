using Content.Server.Heretic.Components;
using Content.Server.Heretic.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class AscendCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public string Command => "ascend";
    public string Description => "Instantly ascends a player as a Heretic.";
    public string Help => "ascend <playername> [path] (paths: Void, Ash, Blade, Flesh, Rust, Cosmos)";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError("You must specify a target player name.");
            return;
        }

        var name = args[0];
        if (!_playerManager.TryGetSessionByUsername(name, out var player))
        {
            shell.WriteError($"Could not find player session with name: {name}");
            return;
        }

        if (player.AttachedEntity == null)
        {
            shell.WriteError($"Player {name} has no attached entity/mob.");
            return;
        }

        var target = player.AttachedEntity.Value;
        var entMan = IoCManager.Resolve<IEntityManager>();

        // Determine path
        string? path = null;
        if (args.Length >= 2)
        {
            path = args[1];
        }

        // Heretic Ascension
        var hereticComp = entMan.EnsureComponent<HereticComponent>(target);
        var hereticKnowledge = entMan.System<HereticKnowledgeSystem>();

        string hereticPath = "Void";
        if (path != null)
        {
            hereticPath = path;
        }
        else if (!string.IsNullOrWhiteSpace(hereticComp.CurrentPath))
        {
            hereticPath = hereticComp.CurrentPath;
        }

        string knowledgeId;
        switch (hereticPath.ToLower())
        {
            case "void":
                hereticPath = "Void";
                knowledgeId = "WaltzAtTheEndOfTime";
                break;
            case "ash":
                hereticPath = "Ash";
                knowledgeId = "AshlordRite";
                break;
            case "blade":
                hereticPath = "Blade";
                knowledgeId = "MaelstromOfSilver";
                break;
            case "flesh":
                hereticPath = "Flesh";
                knowledgeId = "PriestFinalHymn";
                break;
            case "rust":
                hereticPath = "Rust";
                knowledgeId = "RustbringersOath";
                break;
            case "cosmos":
                hereticPath = "Cosmos";
                knowledgeId = "CreatorsGift";
                break;
            default:
                shell.WriteError($"Unknown path: {hereticPath}. Valid paths: Void, Ash, Blade, Flesh, Rust, Cosmos");
                return;
        }

        hereticComp.CurrentPath = hereticPath;
        hereticComp.PathStage = 10;
        hereticComp.CanAscend = true;

        // Force add final knowledge (which triggers the path-specific ascension event)
        hereticKnowledge.AddKnowledge(target, hereticComp, new ProtoId<HereticKnowledgePrototype>(knowledgeId), silent: false);

        // Raise main ascension event to play sounds, update delays etc.
        var ascEv = new EventHereticAscension();
        entMan.EventBus.RaiseLocalEvent(target, ascEv, true);

        shell.WriteLine($"Successfully ascended {name} via Heretic path {hereticPath}!");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                "Player Name");
        }
        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                new[] { "Void", "Ash", "Blade", "Flesh", "Rust", "Cosmos" },
                "Ascension Path");
        }
        return CompletionResult.Empty;
    }
}

