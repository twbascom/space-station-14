using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Goobstation.Blueshield;

[RegisterComponent]
public sealed partial class HardsuitPackageComponent : Component
{
    [DataField("heavyProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string HeavyProto = "ClothingOuterHardsuitBSOHeavy";

    [DataField("lightProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string LightProto = "ClothingOuterHardsuitBSOLight";

    [DataField("unwrapSound")]
    public SoundSpecifier UnwrapSound = new SoundPathSpecifier("/Audio/Effects/unwrap.ogg");
}
