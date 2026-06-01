namespace Content.Server.Revolutionary;

/// <summary>
/// Event raised targeting a player entity to trigger their conversion to the revolutionaries.
/// </summary>
public sealed class RevolutionaryConvertEvent : EntityEventArgs
{
    /// <summary>
    /// The target entity to convert.
    /// </summary>
    public EntityUid Target { get; }

    /// <summary>
    /// The entity that converted them (e.g. the Head Rev performing propaganda).
    /// </summary>
    public EntityUid? Converter { get; }

    public RevolutionaryConvertEvent(EntityUid target, EntityUid? converter)
    {
        Target = target;
        Converter = converter;
    }
}
