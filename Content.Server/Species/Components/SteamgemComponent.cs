using Robust.Shared.Audio;

namespace Content.Server.Species.Components
{
    /// <summary>
    /// Component for the Steamgem species logic.
    /// </summary>
    [RegisterComponent]
    public sealed partial class SteamgemComponent : Component
    {
        [DataField("waterSound")]
        public SoundSpecifier WaterSound = new SoundPathSpecifier("/Textures/Mobs/Species/Steamgem/sfx/water.ogg");

        [DataField("printingSound")]
        public SoundSpecifier PrintingSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

        /// <summary>
        /// Whether the water sound is currently playing.
        /// </summary>
        public EntityUid? WaterStream;

        /// <summary>
        /// Next time to spawn a steam effect.
        /// </summary>
        public TimeSpan NextSteamTime;

        public bool IsHeaterActive;

        public TimeSpan NextIgniteAttempt;
        public TimeSpan NextIgniteMessage;
    }
}
