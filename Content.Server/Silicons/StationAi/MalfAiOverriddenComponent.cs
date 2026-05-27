using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Silicons.StationAi;

[RegisterComponent]
public sealed partial class MalfAiOverriddenComponent : Component
{
    [DataField("expiresAt")]
    public TimeSpan ExpiresAt;
}
