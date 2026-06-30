using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Administration.Components;

[RegisterComponent]
public sealed partial class AdminCentcommComponent : Component
{
    /// <summary>
    /// The username of the admin whose profile should be copied onto this entity when spawned.
    /// </summary>
    [DataField("username", required: true)]
    public string Username = string.Empty;
}
