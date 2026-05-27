using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Added to brain entities to tag that they are a malfunctioning AI.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MalfAiBrainComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MemoryAccumulator = 0f;

    [DataField, AutoNetworkedField]
    public bool HasRolledCore = false;
}
