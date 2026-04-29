using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Explosion.Components;

/// <summary>
///     Component for a bomb that spawns funny items when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent, ComponentProtoName("FunnyBomb")]
public sealed partial class FunnyBombComponent : Component
{
    [DataField]
    public EntProtoId LubePrototype = "CrazyLube";

    [DataField]
    public EntProtoId BananaPrototype = "TrashBananaPeel";

    [DataField]
    public int BananaCount = 5;

    [DataField]
    public EntProtoId SoapPrototype = "Soap";

    [DataField]
    public int SoapCount = 10;

    /// <summary>
    ///     Probability of spawning many soap instead of the other options.
    /// </summary>
    [DataField]
    public float SoapProbability = 0.1f;

    /// <summary>
    ///     Probability of spawning lube vs bananas if soap isn't chosen.
    /// </summary>
    [DataField]
    public float LubeProbability = 0.5f;

    [DataField]
    public string TriggerKey = "timer";

    public bool IsTriggered = false;
}
