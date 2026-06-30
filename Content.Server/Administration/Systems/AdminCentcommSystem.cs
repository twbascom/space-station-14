using Content.Server.Administration.Components;
using Content.Server.Preferences.Managers;
using Content.Server.Humanoid;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Server.Administration.Systems;

public sealed class AdminCentcommSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AdminCentcommComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(EntityUid uid, AdminCentcommComponent component, ComponentStartup args)
    {
        if (string.IsNullOrEmpty(component.Username))
            return;

        // Find the player session by username (case-insensitive)
        var session = _playerManager.Sessions.FirstOrDefault(s => s.Name.Equals(component.Username, System.StringComparison.OrdinalIgnoreCase));
        if (session == null)
            return;

        // Get the player's selected profile
        if (!_prefsManager.TryGetCachedPreferences(session.UserId, out var prefs) || prefs.SelectedCharacter is not HumanoidCharacterProfile profile)
            return;

        // Load the profile onto the entity!
        _humanoidAppearance.LoadProfile(uid, profile);

        // Also update the entity name!
        _metaData.SetEntityName(uid, $"{profile.Name} (CentComm)");
    }
}
