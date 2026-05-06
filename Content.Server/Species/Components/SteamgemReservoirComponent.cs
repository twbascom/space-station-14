using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Server.Species.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class SteamgemReservoirComponent : Component
    {
        [DataField("water")]
        public FixedPoint2 Water = 1000;

        [DataField("maxWater")]
        public FixedPoint2 MaxWater = 1000;
    }
}
