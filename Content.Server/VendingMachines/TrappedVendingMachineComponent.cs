using Robust.Shared.Audio;

namespace Content.Server.VendingMachines;

[RegisterComponent]
public sealed partial class TrappedVendingMachineComponent : Component
{
    /// <summary>
    /// The active user currently interacting with the vending machine.
    /// </summary>
    [ViewVariables]
    public EntityUid? ActiveUser;

    /// <summary>
    /// Timer indicating when the 5-second check is scheduled to run.
    /// </summary>
    [ViewVariables]
    public TimeSpan? TriggerTime;

    [DataField("dispenseDelay")]
    public TimeSpan DispenseDelay = TimeSpan.FromSeconds(5);

    [DataField("dispenseChance")]
    public float DispenseChance = 0.10f; // 10% chance

    [DataField("extraItemStock")]
    public int ExtraItemStock = 1;

    [DataField("extraItemPrototype")]
    public string ExtraItemPrototype = "DrinkGoldenColaCan";

    [ViewVariables]
    public bool Dispensed = false;

    [ViewVariables]
    public TrappedVendingMachineState TrapState = TrappedVendingMachineState.Normal;

    /// <summary>
    /// Time when the grenade/smoke step finishes, transitioning to the drag/grab step.
    /// </summary>
    [ViewVariables]
    public TimeSpan? DragEndTime;

    /// <summary>
    /// Entity currently being dragged towards the vending machine.
    /// </summary>
    [ViewVariables]
    public EntityUid? Victim;

    /// <summary>
    /// Entity currently captured inside the vending machine.
    /// </summary>
    [ViewVariables]
    public EntityUid? InsideVictim;

    [ViewVariables]
    public float DamageSinceDrag = 0f;

    [ViewVariables]
    public float DamageSinceCapture = 0f;

    [DataField("dispenseSound")]
    public SoundSpecifier DispenseSound = new SoundPathSpecifier("/Audio/Machines/vending_dispense.ogg");

    [DataField("captureSound")]
    public SoundSpecifier CaptureSound = new SoundPathSpecifier("/Audio/Effects/spit.ogg");
}

public enum TrappedVendingMachineState
{
    Normal,
    GrenadeExploding,
    Dragging,
    Captured
}
