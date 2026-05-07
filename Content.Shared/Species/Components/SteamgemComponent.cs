using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Species.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class SteamgemComponent : Component
    {
        [DataField("waterSound")]
        public SoundSpecifier WaterSound = new SoundPathSpecifier("/Audio/Mobs/Species/Steamgem/water.ogg");

        [DataField("printingSound")]
        public SoundSpecifier PrintingSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

        public EntityUid? WaterStream;

        public TimeSpan NextSteamTime;

        public bool IsHeaterActive;

        public TimeSpan NextIgniteAttempt;
        public TimeSpan NextIgniteMessage;
    }
}
