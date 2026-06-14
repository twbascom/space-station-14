using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing;

/// <summary>
///     Restricts this clothing item to be equipped only by certain species.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpeciesRestrictClothingComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<SpeciesPrototype>> AllowedSpecies = new();
}
