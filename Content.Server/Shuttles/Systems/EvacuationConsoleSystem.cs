using Content.Server.Access.Systems;
using Content.Server.Chat.Systems;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    public sealed partial class EvacuationConsoleComponent : Component
    {
        [DataField]
        public bool Activated = false;
    }
}

namespace Content.Server.Shuttles.Systems
{
    public sealed class EvacuationConsoleSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly RoundEndSystem _roundEnd = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EvacuationConsoleComponent, InteractHandEvent>(OnInteractHand);
        }

        private void OnInteractHand(EntityUid uid, EvacuationConsoleComponent component, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            var user = args.User;

            // Check if already activated
            if (component.Activated)
            {
                _popup.PopupEntity(Loc.GetString("evack-console-already-active"), uid, user, PopupType.Medium);
                return;
            }

            // Check access if AccessReader is present
            if (TryComp<AccessReaderComponent>(uid, out var reader))
            {
                if (!_accessReader.IsAllowed(user, uid, reader))
                {
                    _popup.PopupEntity(Loc.GetString("evac-console-denied"), uid, user, PopupType.MediumCaution);
                    _audio.PlayPvs("/Audio/Machines/button_error.ogg", uid);
                    return;
                }
            }

            // Mark as activated
            component.Activated = true;
            args.Handled = true;

            // Play a confirmation sound
            _audio.PlayPvs("/Audio/Machines/terminal_alert.ogg", uid);

            // Trigger evacuation sequence!
            TriggerEvacuation(uid, component);
        }

        public void TriggerEvacuation(EntityUid uid, EvacuationConsoleComponent component)
        {
            var stationUid = _station.GetOwningStation(uid);
            if (stationUid == null)
                return;

            // Announce evacuation globally
            _chatSystem.DispatchGlobalAnnouncement(
                Loc.GetString("evac-console-activated-announcement"),
                Loc.GetString("evac-console-sender"),
                playSound: false,
                colorOverride: Color.Red
            );

            _audio.PlayGlobal("/Audio/Announcements/shuttlecalled.ogg", Filter.Broadcast(), true);

            // Find all escape pods docked to the station and set their launch time
            var podQuery = EntityQueryEnumerator<EscapePodComponent, ShuttleComponent>();
            int podsCount = 0;
            while (podQuery.MoveNext(out var podUid, out var pod, out var shuttle))
            {
                if (_station.GetOwningStation(podUid) == stationUid)
                {
                    // Stagger launch time by a fraction of a second for visual effect
                    pod.LaunchTime = _timing.CurTime + TimeSpan.FromSeconds(podsCount * 0.2f + 2.0f);
                    podsCount++;
                }
            }

            // Start the round end countdown so the round restarts after the pods arrive at CentComm.
            _roundEnd.EndRound(TimeSpan.FromSeconds(15));
        }
    }
}
