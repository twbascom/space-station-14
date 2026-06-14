using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Server.Revolutionary.Components;
using Content.Shared.Chat;
using Content.Shared.Implants.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Containers;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.Medical.CrewMonitoring;

public sealed class BlueshieldTabletSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<CrewMonitoringConsoleComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnEntRemoved(EntityUid uid, CrewMonitoringConsoleComponent component, EntRemovedFromContainerMessage args)
    {
        if (!component.CommandOnly)
            return;

        if (args.Container.ID != ImplanterComponent.ImplanterSlotId)
            return;

        if (TryComp<ImplanterComponent>(uid, out var implanterComp))
        {
            if (implanterComp.ImplanterSlot.ContainerSlot?.ContainedEntities.Count == 0)
            {
                var coords = Transform(uid).Coordinates;
                var newImplant = Spawn("CommandTrackingImplant", coords);
                _container.Insert(newImplant, implanterComp.ImplanterSlot.ContainerSlot);
            }
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        var uid = args.Target;

        // Only alert if the entity has been implanted with a CommandTrackingImplant
        if (!TryComp<ImplantedComponent>(uid, out var implanted) ||
            !implanted.ImplantContainer.ContainedEntities.Any(e => Prototype(e)?.ID == "CommandTrackingImplant"))
        {
            return;
        }

        if (args.NewMobState != MobState.Critical && args.NewMobState != MobState.Dead)
            return;

        // Get nearest beacon
        var location = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(uid));
        var name = Name(uid);
        var stateText = args.NewMobState == MobState.Critical ? "in critical condition" : "dead";

        var message = $"ALERT: {name} is {stateText} near {location}!";

        // Find all active commandOnly crew monitoring consoles (the BSO tablets)
        var query = EntityQueryEnumerator<CrewMonitoringConsoleComponent>();
        while (query.MoveNext(out var tabletUid, out var consoleComp))
        {
            if (!consoleComp.CommandOnly)
                continue;

            // Check if tablet is toggled on (if toggleable)
            if (HasComp<ItemToggleComponent>(tabletUid) && !_itemToggle.IsActivated(tabletUid))
                continue;

            // Check if tablet has power cell with charge (if it uses power cells)
            if (HasComp<PowerCellSlotComponent>(tabletUid) && !_powerCell.HasCharge(tabletUid, 0f))
                continue;

            // Send local chat message at the tablet's location
            _chat.TrySendInGameICMessage(tabletUid, message, InGameICChatType.Speak, hideChat: false);
        }
    }
}
