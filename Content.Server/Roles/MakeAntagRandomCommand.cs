using System.Linq;
using Content.Server.Administration;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Roles
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class MakeAntagRandomCommand : IConsoleCommand
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public string Command => "makeantagrandom";
        public string Description => "Makes a random connected player an antagonist (traitor).";
        public string Help => "makeantagrandom";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var sessions = _playerManager.Sessions.Where(x => x.AttachedEntity != null).ToList();
            if (sessions.Count == 0)
            {
                shell.WriteError("No players with attached entities found.");
                return;
            }

            var selectedPlayer = _random.Pick(sessions);
            var entSystem = IoCManager.Resolve<IEntitySystemManager>();
            var antagSystem = entSystem.GetEntitySystem<AntagSelectionSystem>();
            
            antagSystem.ForceMakeAntag<TraitorRuleComponent>(selectedPlayer, "Traitor");
            shell.WriteLine($"Successfully made {selectedPlayer.Name} a traitor!");
        }
    }
}
