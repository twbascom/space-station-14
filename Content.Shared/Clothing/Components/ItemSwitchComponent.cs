using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Content.Shared.Damage;

namespace Content.Shared.Clothing.Components;

[RegisterComponent]
public sealed partial class ItemSwitchComponent : Component
{
    [DataField("showLabel")]
    public bool ShowLabel = true;

    [DataField("state")]
    public string? State;

    [DataField("states")]
    public Dictionary<string, ItemSwitchState>? States;
}

[DataDefinition]
public sealed partial class ItemSwitchState
{
    [DataField("verb")]
    public string? Verb;

    [DataField("sprite")]
    public SpriteSpecifier? Sprite;

    [DataField("components")]
    public ComponentRegistry? Components;

    [DataField("soundStateActivate")]
    public SoundSpecifier? SoundStateActivate;
}

[RegisterComponent]
public sealed partial class ScannableForPointsComponent : Component
{
    [DataField("points")]
    public int Points;
}

[RegisterComponent]
public sealed partial class LoudspeakerComponent : Component
{
    [DataField("canToggle")]
    public bool CanToggle;

    [DataField("affectRadio")]
    public bool AffectRadio;
}

[RegisterComponent]
public sealed partial class ClothingGrantComponentComponent : Component
{
    [DataField("component")]
    public ComponentRegistry? Component;
}

[RegisterComponent]
public sealed partial class StasisProtectionComponent : Component
{
}

[RegisterComponent]
public sealed partial class StasisBlinkProviderComponent : Component
{
}

[RegisterComponent]
public sealed partial class GrantCorporateJudoComponent : Component
{
}

[RegisterComponent]
public sealed partial class ModifyStandingUpTimeComponent : Component
{
    [DataField("multiplier")]
    public float Multiplier = 1.0f;
}

[RegisterComponent]
public sealed partial class VoxRestrictedComponent : Component
{
}

[RegisterComponent]
public sealed partial class GrabModifierComponent : Component
{
    [DataField("startingGrabStage")]
    public string? StartingGrabStage;

    [DataField("grabEscapeModifier")]
    public float GrabEscapeModifier;

    [DataField("grabEscapeMultiplier")]
    public float GrabEscapeMultiplier;

    [DataField("grabMoveSpeedMultiplier")]
    public float GrabMoveSpeedMultiplier;
}

[RegisterComponent]
public sealed partial class ShowDiseaseIconsComponent : Component
{
    [DataField("lowThreshold")]
    public float LowThreshold;

    [DataField("mediumThreshold")]
    public float MediumThreshold;

    [DataField("highThreshold")]
    public float HighThreshold;
}

[RegisterComponent]
public sealed partial class ClothingAutoInjectComponent : Component
{
    [DataField("reagents")]
    public Dictionary<string, float>? Reagents;
}

[RegisterComponent]
public sealed partial class CrematoriumImmuneComponent : Component
{
}

[RegisterComponent]
public sealed partial class ShowContrabandIconsComponent : Component
{
}

[RegisterComponent]
public sealed partial class ExplosiveShockComponent : Component
{
    [DataField("handsDamage")]
    public DamageSpecifier? HandsDamage;

    [DataField("armsDamage")]
    public DamageSpecifier? ArmsDamage;
}

[RegisterComponent]
public sealed partial class ReverseBearTrapComponent : Component
{
    [DataField("countdownDuration")]
    public float CountdownDuration;

    [DataField("baseEscapeChance")]
    public float BaseEscapeChance;

    [DataField("delayOptions")]
    public List<float>? DelayOptions;
}

[RegisterComponent]
public sealed partial class TriggerOnSpeakComponent : Component
{
}

[RegisterComponent]
public sealed partial class ContainmentFieldIgnoreComponent : Component
{
}

[RegisterComponent]
public sealed partial class SpeechSoundsReplacerComponent : Component
{
    [DataField("speechSounds")]
    public string? SpeechSounds;
}

[RegisterComponent]
public sealed partial class CloneProjectorComponent : Component
{
    [DataField("damageOnDestroyed")]
    public DamageSpecifier? DamageOnDestroyed;

    [DataField("addedComponents")]
    public ComponentRegistry? AddedComponents;

    [DataField("ghostRoleDescription")]
    public string? GhostRoleDescription;

    [DataField("doStun")]
    public bool DoStun;

    [DataField("restrictRangedWeapons")]
    public bool RestrictRangedWeapons;

    [DataField("nameSuffix")]
    public string? NameSuffix;

    [DataField("ghostRoleName")]
    public string? GhostRoleName;

    [DataField("removedComponents")]
    public ComponentRegistry? RemovedComponents;

    [DataField("userBlacklist")]
    public Content.Shared.Whitelist.EntityWhitelist? UserBlacklist;

    [DataField("clonedItemBlacklist")]
    public Content.Shared.Whitelist.EntityWhitelist? ClonedItemBlacklist;

    [DataField("clonedItemWhitelist")]
    public Content.Shared.Whitelist.EntityWhitelist? ClonedItemWhitelist;
}

[RegisterComponent]
public sealed partial class UncloneableComponent : Component {}

[RegisterComponent]
public sealed partial class SurgeryTargetComponent : Component {}

[RegisterComponent]
public sealed partial class CondemnedComponent : Component
{
    [DataField("soulOwnedNotDevil")]
    public bool SoulOwnedNotDevil;
}

[RegisterComponent]
public sealed partial class AbsorbableComponent : Component {}

[RegisterComponent]
public sealed partial class FootprintOwnerComponent : Component {}

[RegisterComponent]
public sealed partial class BreathingImmunityComponent : Component {}

[RegisterComponent]
public sealed partial class TemperatureImmunityComponent : Component {}
