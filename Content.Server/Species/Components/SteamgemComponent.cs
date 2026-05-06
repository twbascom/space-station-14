using Robust.Shared.Audio;

namespace Content.Server.Species.Components
{
    [RegisterComponent]
    public sealed partial class SteamgemComponent : Component
    {
        [DataField("waterSound")]
        public SoundSpecifier WaterSound = new SoundPathSpecifier("/Textures/Mobs/Species/Steamgem/sfx/water.ogg");

        [DataField("printingSound")]
        public SoundSpecifier PrintingSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

        public EntityUid? WaterStream;

        public TimeSpan NextSteamTime;

        public bool IsHeaterActive;

        public TimeSpan NextIgniteAttempt;
        public TimeSpan NextIgniteMessage;
    }
}
