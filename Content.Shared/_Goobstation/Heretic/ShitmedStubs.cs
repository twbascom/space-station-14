using Robust.Shared.Containers;
using Content.Shared.FixedPoint;
using Content.Shared.Damage;

// Global stubs namespace (imported in GlobalUsings.cs)
namespace Content.Shared.Damage.Systems
{
    using Content.Shared.Damage;

    public enum HarmfulActionType
    {
        Harm,
        Other,
        MansusGrasp
    }

    public sealed class BeforeHarmfulActionEvent : EntityEventArgs
    {
        public bool Cancelled;
        public HarmfulActionType Type;
        public EntityUid User;
        public BeforeHarmfulActionEvent(EntityUid user, HarmfulActionType type = HarmfulActionType.Other)
        {
            User = user;
            Type = type;
        }
        public void Cancel() => Cancelled = true;
    }

    public sealed class TeleportAttemptEvent : EntityEventArgs
    {
        public bool Cancelled;
        public void Cancel() => Cancelled = true;
    }

    public sealed class BeforeCastTouchSpellEvent : EntityEventArgs
    {
        public EntityUid Target;
        public bool Cancelled;
        public BeforeCastTouchSpellEvent(EntityUid target, bool cancelled = false)
        {
            Target = target;
            Cancelled = cancelled;
        }
        public void Cancel() => Cancelled = true;
    }

    // DamageModifyEvent is already defined in Content.Shared.Damage.Systems

    public sealed class OldBeforeStatusEffectAddedEvent : EntityEventArgs
    {
        public string Key = default!;
        public bool Cancelled;
    }

    public sealed class TemperatureChangeAttemptEvent : EntityEventArgs
    {
        public bool Cancelled;
        public float CurrentTemperature;
        public float LastTemperature;
        public void Cancel() => Cancelled = true;
    }

    [ByRefEvent]
    public struct GetLightAttackRangeEvent
    {
        public EntityUid? Target;
        public EntityUid User;
        public float Range;
        public bool Cancel;
    }

    [ByRefEvent]
    public struct LightAttackSpecialInteractionEvent
    {
        public EntityUid? Target;
        public EntityUid User;
        public float Range;
        public bool Cancel;
    }



    [Serializable, Robust.Shared.Serialization.NetSerializable]
    public sealed class StopTargetingEvent : EntityEventArgs {}

    [ByRefEvent]
    public struct ModifyDisgustEvent
    {
        public float Amount;
        public ModifyDisgustEvent(float amount) => Amount = amount;
    }

    public static class DamageableSystemExtensions
    {
        public static bool TryChangeDamage(
            this DamageableSystem damageableSystem,
            EntityUid uid,
            DamageSpecifier damage,
            bool ignoreResistances = false,
            bool interruptsDoAfters = true,
            DamageableComponent? damageable = null,
            EntityUid? origin = null,
            dynamic? targetPart = null,
            dynamic? splitDamage = null,
            bool? canMiss = null,
            bool ignoreGlobalModifiers = false
        )
        {
            return damageableSystem.TryChangeDamage((uid, damageable), damage, ignoreResistances, interruptsDoAfters, origin, ignoreGlobalModifiers);
        }
     }

    public static class StaminaSystemExtensions
    {
        public static void ToggleStaminaDrain(this SharedStaminaSystem staminaSystem, EntityUid uid, float rate, bool enable, bool val3, string regenKey, EntityUid? val4 = null) {}
        public static void ToggleStaminaDrain<T>(this SharedStaminaSystem staminaSystem, Robust.Shared.GameObjects.Entity<T> ent, float rate, bool enable, bool val3, string regenKey, EntityUid? val4 = null) where T : IComponent
        {
            ToggleStaminaDrain(staminaSystem, ent.Owner, rate, enable, val3, regenKey, val4);
        }

        public static void ExitStamCrit(this SharedStaminaSystem staminaSystem, EntityUid uid, StaminaComponent component)
        {
            dynamic c = component;
            c.Critical = false;
            c.AfterCritical = true;
            c.StaminaDamage = 0f;
        }
    }
}

// 2. Content.Shared._Shitmed.Targeting
namespace Content.Shared._Shitmed.Targeting
{
    public enum TargetBodyPart
    {
        Chest,
        All,
        Vital
    }
}

// 3. Content.Shared._Shitmed.Medical.Surgery.Wounds.Components
namespace Content.Shared._Shitmed.Medical.Surgery.Wounds.Components
{
    using Robust.Shared.Containers;

    [RegisterComponent]
    public sealed partial class WoundableComponent : Component
    {
        [DataField]
        public bool CanRemove = true;
        
        [DataField]
        public ContainerSlot Bone = default!;
    }
}

// 4. Content.Shared._Shitmed.Medical.Surgery.Consciousness.Components
namespace Content.Shared._Shitmed.Medical.Surgery.Consciousness.Components
{
    [RegisterComponent]
    public sealed partial class ConsciousnessComponent : Component
    {
        public dynamic? NerveSystem;
        public Dictionary<dynamic, dynamic> Multipliers = new();
        public Dictionary<dynamic, dynamic> Modifiers = new();
    }
}

// 5. Content.Shared._Shitmed.Medical.Surgery.Consciousness
namespace Content.Shared._Shitmed.Medical.Surgery.Consciousness
{
    public enum ConsciousnessModType
    {
        Pain,
        Other
    }
}

// 6. Content.Shared._Shitmed.Medical.Surgery.Consciousness.Systems
namespace Content.Shared._Shitmed.Medical.Surgery.Consciousness.Systems
{
    public sealed class ConsciousnessSystem : EntitySystem
    {
        public void RemoveConsciousnessMultiplier(EntityUid uid, dynamic val1, dynamic val2, dynamic comp) {}
        public void RemoveConsciousnessModifier(EntityUid uid, dynamic val1, dynamic val2, dynamic comp) {}
    }
}

// 7. Content.Shared._Shitmed.Medical.Surgery.Pain.Systems
namespace Content.Shared._Shitmed.Medical.Surgery.Pain.Systems
{
    public sealed class PainSystem : EntitySystem
    {
        public void TryChangePainModifier(EntityUid uid, dynamic val1, dynamic val2, float val3, dynamic comp) {}
        public void TryRemovePainModifier(EntityUid uid, dynamic val1, dynamic val2, dynamic comp) {}
        public void TryRemovePainMultiplier(EntityUid uid, dynamic val1, dynamic comp) {}
        public void TryRemovePainFeelsModifier(dynamic val1, dynamic val2, dynamic val3, dynamic val4) {}
    }
}

// 8. Content.Shared._Shitmed.Medical.Surgery.Traumas.Components
namespace Content.Shared._Shitmed.Medical.Surgery.Traumas.Components
{
    [RegisterComponent]
    public sealed partial class BoneComponent : Component
    {
        [DataField]
        public float IntegrityCap = 100f;
    }
    
    [RegisterComponent]
    public sealed partial class NerveComponent : Component
    {
        public Dictionary<dynamic, dynamic> Modifiers = new();
        public Dictionary<dynamic, dynamic> Multipliers = new();
        public Dictionary<dynamic, dynamic> Nerves = new();
    }
}

// 9. Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems
namespace Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems
{
    public sealed class TraumaSystem : EntitySystem
    {
        public void ApplyDamageToBone(EntityUid uid, FixedPoint2 amount, object comp) {}
        public void SetBoneIntegrity(EntityUid uid, float val1, object comp) {}
    }
}

// 10. Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems
namespace Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems
{
    using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;

    public sealed class WoundSystem : EntitySystem
    {
        public IEnumerable<Robust.Shared.GameObjects.Entity<WoundableComponent>> GetAllWoundableChildren(EntityUid uid)
        {
            return Array.Empty<Robust.Shared.GameObjects.Entity<WoundableComponent>>();
        }
        public void TryHaltAllBleeding(EntityUid uid, object comp, bool value) {}
        public void ForceHealWoundsOnWoundable(EntityUid uid, out List<EntityUid>? val1, object? val2, object? val3) { val1 = null; }
        public void TryHealWoundsOnWoundable(EntityUid uid, FixedPoint2 amount, out List<EntityUid>? val1, object? val2, object? val3, bool val4, bool val5) { val1 = null; }
    }
}

// 11. Content.Shared._Shitmed.Surgery
namespace Content.Shared._Shitmed.Surgery
{
    [ByRefEvent]
    public struct SurgeryIgnorePreviousStepsEvent
    {
        public bool Handled;
    }

    [ByRefEvent]
    public struct SurgeryPainEvent
    {
        public bool Cancelled;
        public void Cancel() => Cancelled = true;
    }
}

// 12. Content.Shared._Shitmed.Body
namespace Content.Shared._Shitmed.Body
{
    public class BodyDummy {}
}

// 12b. Content.Shared._Shitmed.Damage
namespace Content.Shared._Shitmed.Damage
{
    public class DamageDummy {}
}

// 13. Content.Shared._Shitmed.DoAfter
namespace Content.Shared._Shitmed.DoAfter
{
    [ByRefEvent]
    public struct GetDoAfterDelayMultiplierEvent
    {
        public float Multiplier;
    }
}

// 14. Content.Shared._EinsteinEngines.Silicon.Components
namespace Content.Shared._EinsteinEngines.Silicon.Components
{
    [RegisterComponent]
    public sealed partial class SiliconComponent : Component {}
}

// 15. Content.Shared._White.Xenomorphs.Xenomorph
namespace Content.Shared._White.Xenomorphs.Xenomorph
{
    [RegisterComponent]
    public sealed partial class XenomorphComponent : Component {}
}

// 16. Content.Shared._White.BackStab
namespace Content.Shared._White.BackStab
{
    public sealed class BackStabSystem : EntitySystem
    {
        public bool TryBackstab(EntityUid target, EntityUid performer, Angle angle) => false;
    }
}

// 17. Content.Shared._Goobstation.Wizard.SanguineStrike
namespace Content.Shared._Goobstation.Wizard.SanguineStrike
{
    public sealed class SharedSanguineStrikeSystem : EntitySystem
    {
        public void LifeSteal(EntityUid user, FixedPoint2 amount, Content.Shared.Damage.Components.DamageableComponent dmg) {}
    }
}

namespace Content.Shared._Goobstation.Wizard
{
    [RegisterComponent]
    public sealed partial class WizardComponent : Component {}

    [RegisterComponent]
    public sealed partial class ApprenticeComponent : Component {}
}

// 18. Content.Goobstation.Common.Identity
namespace Content.Goobstation.Common.Identity
{
    [ByRefEvent]
    public struct GetIdentityRepresentationEntityEvent
    {
        public EntityUid Uid;
    }
}

// 19. Content.Goobstation.Common.Projectiles
namespace Content.Goobstation.Common.Projectiles
{
    [ByRefEvent]
    public struct ShouldTargetedProjectileCollideEvent
    {
        public EntityUid Target;
        public bool Handled;
    }
}

// 20. Content.Goobstation.Common.Speech
namespace Content.Goobstation.Common.Speech
{
    [ByRefEvent]
    public struct GetSpeechSoundEvent
    {
        public string SpeechSoundProtoId;
    }

    [ByRefEvent]
    public struct GetEmoteSoundsEvent
    {
        public string EmoteSoundProtoId;
    }
}

// 21. Content.Goobstation.Common.Religion & Content.Goobstation.Shared.Religion
namespace Content.Goobstation.Common.Religion
{
    public class ReligionDummy {}
}
namespace Content.Goobstation.Shared.Religion
{
    public class ReligionDummyShared {}
}

// 22. Content.Goobstation.Common.Stunnable
namespace Content.Goobstation.Common.Stunnable
{
    [ByRefEvent]
    public struct GetClothingStunModifierEvent
    {
        public float Modifier;
    }
}

// 23. Content.Goobstation.Common.Bloodstream
namespace Content.Goobstation.Common.Bloodstream
{
    [ByRefEvent]
    public struct GetBloodlossDamageMultiplierEvent
    {
        public float Multiplier;
    }
}

// 24. Content.Goobstation.Common.BlockTeleport
namespace Content.Goobstation.Common.BlockTeleport
{
    [RegisterComponent]
    public sealed partial class BlockTeleportComponent : Component {}
}

// 25. Content.Goobstation.Common.Physics
namespace Content.Goobstation.Common.Physics
{
    public class PhysicsDummy {}
}

// 26. Content.Goobstation.Common.SecondSkin
namespace Content.Goobstation.Common.SecondSkin
{
    public class SecondSkinDummy {}
}

// 27. Content.Goobstation.Common.Weapons
namespace Content.Goobstation.Common.Weapons
{
    public class WeaponsDummy {}
}

// 28. Content.Goobstation.Common.MartialArts
namespace Content.Goobstation.Common.MartialArts
{
    public abstract class BaseRiposteCheckEvent
    {
        public bool Handled { get; set; }
    }
}

// 29. Content.Shared._Goobstation.Wizard.FadingTimedDespawn
namespace Content.Shared._Goobstation.Wizard.FadingTimedDespawn
{
    [RegisterComponent]
    public sealed partial class FadingTimedDespawnComponent : Component
    {
        [DataField]
        public float Lifetime;

        [DataField]
        public float FadeOutTime;
    }
}


// 31. Content.Goobstation.Common.Temperature.Components
namespace Content.Goobstation.Common.Temperature.Components
{
    [RegisterComponent]
    public sealed partial class SpecialHighTempImmunityComponent : Component {}

    [RegisterComponent]
    public sealed partial class SpecialLowTempImmunityComponent : Component {}

    [RegisterComponent]
    public sealed partial class SpecialPressureImmunityComponent : Component {}
}

// 32. Content.Goobstation.Common.Body.Components
namespace Content.Goobstation.Common.Body.Components
{
    [RegisterComponent]
    public sealed partial class SpecialBreathingImmunityComponent : Component {}
}

// 33. Content.Goobstation.Common.Atmos
namespace Content.Goobstation.Common.Atmos
{
    public class AtmosDummy {}
}

// 34. Content.Shared._Shitcode.Heretic.Components
namespace Content.Shared._Shitcode.Heretic.Components
{
    [RegisterComponent]
    public sealed partial class ComplexJointVisualsComponent : Component
    {
        [DataField]
        public Dictionary<NetEntity, ComplexJointVisualsData> Data = new();
    }

    [Serializable, Robust.Shared.Serialization.NetSerializable]
    public sealed class ComplexJointVisualsData
    {
        public string Id;
        public Robust.Shared.Utility.SpriteSpecifier Sprite;
        public Robust.Shared.Utility.SpriteSpecifier? StartSprite;
        public Robust.Shared.Utility.SpriteSpecifier? EndSprite;
        public System.Numerics.Vector2 Scale;

        public ComplexJointVisualsData(string id, Robust.Shared.Utility.SpriteSpecifier sprite)
        {
            Id = id;
            Sprite = sprite;
        }

        public ComplexJointVisualsData(string id, Robust.Shared.Utility.SpriteSpecifier sprite, Robust.Shared.Utility.SpriteSpecifier? start, Robust.Shared.Utility.SpriteSpecifier? end, TimeSpan time)
        {
            Id = id;
            Sprite = sprite;
            StartSprite = start;
            EndSprite = end;
        }
    }
}

// 35. Content.Shared._Goobstation.Wizard.Traps
namespace Content.Shared._Goobstation.Wizard.Traps
{
    public class TrapsDummy {}
}

// 36. Content.Goobstation.Maths.FixedPoint
namespace Content.Goobstation.Maths.FixedPoint
{
    public class FixedPointDummy {}
}

// 37. Content.Shared._Goobstation.Wizard.TimeStop
namespace Content.Shared._Goobstation.Wizard.TimeStop
{
    public class TimeStopDummy {}
}

// 38. BodySystemExtensions providing GibBody in standard body namespaces
namespace Content.Shared.Body.Systems
{
    using Content.Shared.Body.Components;
    using System.Collections.Generic;

    public static class BodySystemExtensions
    {
        public static void GibBody(this Content.Shared.Body.BodySystem bodySystem, EntityUid ent, Content.Shared.Body.BodyComponent? body = null, dynamic? contents = null)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<Content.Shared.Gibbing.GibbingSystem>().Gib(ent);
        }

        public static void RestoreBody(this Content.Shared.Body.BodySystem bodySystem, EntityUid ent)
        {
        }

        public static IEnumerable<Entity<T>> GetBodyOrganEntityComps<T>(this SharedBodySystem bodySystem, Entity<BodyComponent?> body) where T : Component
        {
            if (bodySystem.TryGetOrgansWithComponent<T>(body, out var organs))
            {
                return organs;
            }
            return System.Array.Empty<Entity<T>>();
        }

        public static IEnumerable<dynamic> GetBodyChildren(this SharedBodySystem bodySystem, EntityUid uid)
        {
            return System.Array.Empty<dynamic>();
        }

        public static List<EntityUid> GetBodyChildrenOfType(this Content.Shared.Body.BodySystem bodySystem, EntityUid uid, Content.Shared.Body.Part.BodyPartType type)
        {
            return new List<EntityUid>();
        }
    }
}
namespace Content.Shared.Body
{
    public enum BodyType
    {
        Simple,
        Complex
    }

    public sealed partial class BodyComponent
    {
        public BodyType BodyType => BodyType.Complex;
    }

    public static class BodySystemExtensions
    {
        public static bool TryGetRootPart(this Content.Shared.Body.BodySystem bodySystem, EntityUid uid, out EntityUid? rootPart, Content.Shared.Body.BodyComponent? body = null)
        {
            rootPart = null;
            return false;
        }
    }
}


namespace Content.Shared._Goobstation.Heretic.Systems
{
    [RegisterComponent]
    public sealed partial class FrozenComponent : Component {}

    [RegisterComponent]
    public sealed partial class IceCubeComponent : Component {}

    public sealed class SaveLastAttacksEvent : EntityEventArgs {}
    public sealed class ResetLastAttacksEvent : EntityEventArgs
    {
        public bool Value;
        public ResetLastAttacksEvent(bool value) => Value = value;
    }
    public sealed class LoadLastAttacksEvent : EntityEventArgs {}
}

namespace Content.Shared.Movement.Pulling.Components
{
    public enum GrabStage
    {
        No,
        Soft,
        Hard
    }

    public sealed partial class PullerComponent
    {
        public GrabStage GrabStage => GrabStage.No;
    }
}

namespace Content.Shared.Movement.Pulling.Systems
{
    public static class PullingSystemExtensions
    {
        public static void StopAllPulls(this PullingSystem pullingSystem, EntityUid entity, bool stopPuller = false)
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            if (entManager.TryGetComponent<Content.Shared.Movement.Pulling.Components.PullableComponent>(entity, out var pullable))
            {
                pullingSystem.TryStopPull(entity, pullable);
            }
            if (entManager.TryGetComponent<Content.Shared.Movement.Pulling.Components.PullerComponent>(entity, out var puller) && puller.Pulling != null)
            {
                if (entManager.TryGetComponent<Content.Shared.Movement.Pulling.Components.PullableComponent>(puller.Pulling.Value, out var pulledPullable))
                {
                    pullingSystem.TryStopPull(puller.Pulling.Value, pulledPullable);
                }
            }
        }

        public static bool TryStartPull(this PullingSystem pullingSystem, EntityUid pullerUid, EntityUid pullableUid,
            Content.Shared.Movement.Pulling.Components.PullerComponent? pullerComp,
            Content.Shared.Movement.Pulling.Components.PullableComponent? pullableComp,
            dynamic grabStage,
            bool force)
        {
            return pullingSystem.TryStartPull(pullerUid, pullableUid, pullerComp, pullableComp);
        }
    }
}

namespace Content.Shared.Magic
{
    public static class MagicSystemExtensions
    {
        public static bool IsTouchSpellDenied(this SharedMagicSystem magicSystem, EntityUid target)
        {
            var ev = new Content.Shared.Damage.Systems.BeforeCastTouchSpellEvent(target, false);
            IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(target, ev, true);
            return ev.Cancelled;
        }
    }
}

namespace Content.Shared.Weapons.Ranged.Systems
{
    public static class GunSystemExtensions
    {
        public static bool SetTarget(this SharedGunSystem gunSystem, EntityUid projectile, EntityUid? target, out dynamic? val)
        {
            val = null;
            return false;
        }
    }
}

namespace Content.Shared.Stunnable
{
    public static class StunSystemExtensions
    {
        public static bool KnockdownOrStun(this SharedStunSystem stunSystem, EntityUid uid, TimeSpan time, bool refresh, object? statusEffects = null)
        {
            return stunSystem.TryKnockdown(uid, time, refresh);
        }

        public static bool KnockdownOrStun(this SharedStunSystem stunSystem, EntityUid uid, TimeSpan time, bool refresh)
        {
            return stunSystem.TryKnockdown(uid, time, refresh);
        }

        public static bool TrySlowdown(this SharedStunSystem stunSystem, EntityUid uid, TimeSpan time, bool refresh, float walk, float sprint)
        {
            return false;
        }

        public static bool TryParalyze(this SharedStunSystem stunSystem, EntityUid uid, TimeSpan time, bool refresh, dynamic? status = null)
        {
            return stunSystem.TryAddStunDuration(uid, time);
        }
    }
}


namespace Content.Shared.Damage.Systems
{
    public static class StaminaSystemExtensionsExtra
    {
        public static void TakeOvertimeStaminaDamage(this SharedStaminaSystem staminaSystem, EntityUid uid, float amount) {}
    }
}

namespace Content.Shared.EntityEffects
{
    using Robust.Shared.Prototypes;
    using Robust.Shared.Random;

    public struct EntityEffectBaseArgs
    {
        public EntityUid TargetEntity;
        public IEntityManager EntityManager;

        public EntityEffectBaseArgs(EntityUid targetEntity, IEntityManager entityManager)
        {
            TargetEntity = targetEntity;
            EntityManager = entityManager;
        }
    }

    public abstract partial class EntityEffect
    {
        public virtual void Effect(EntityEffectBaseArgs args) {}
        protected virtual string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;
        public virtual bool ShouldApply(EntityEffectBaseArgs args, Robust.Shared.Random.IRobustRandom random)
        {
            return random.Prob(Probability);
        }
    }
}

namespace Content.Shared.Roles
{
    [RegisterComponent]
    public sealed partial class GhoulRoleComponent : Content.Shared.Roles.Components.BaseMindRoleComponent {}
}

namespace Content.Shared.Damage.Systems
{
    [RegisterComponent]
    public sealed partial class RandomTeleportComponent : Component {}
}

namespace Content.Goobstation.Shared.Teleportation.Systems
{
    using Content.Shared.Damage.Systems;

    public class SharedRandomTeleportSystem : EntitySystem
    {
        public void RandomTeleport(EntityUid user, RandomTeleportComponent comp, bool flag) {}
    }
}

namespace Content.Goobstation.Shared.MartialArts.Components
{
    public enum MartialArtModifierType
    {
        Healing,
        AttackRate,
        MoveSpeed
    }

    public sealed class MartialArtModifierData
    {
        public MartialArtModifierType Type;
        public float Multiplier;
        public TimeSpan EndTime;
    }

    [RegisterComponent]
    public sealed partial class MartialArtModifiersComponent : Component
    {
        public List<MartialArtModifierData> Data = new();
        public Dictionary<MartialArtModifierType, System.Numerics.Vector2> MinMaxModifiersMultipliers = new();
    }
}

namespace Content.Shared._Starlight.CollectiveMind
{
    using Robust.Shared.Prototypes;

    [Prototype("collectiveMind")]
    public sealed partial class CollectiveMindPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;
    }
}

namespace Content.Shared.Body.Part
{
    public enum BodyPartType
    {
        Head,
        Other
    }

    [ByRefEvent]
    public struct BodyPartAddedEvent
    {
        public EntityUid Part;
    }

    [ByRefEvent]
    public struct BodyPartRemovedEvent
    {
        public EntityUid Part;
    }
}

namespace Content.Shared.Damage.Systems
{
    [ByRefEvent]
    public struct BeforeTriggerEvent
    {
        public EntityUid? User;
        public bool Cancelled;
        public void Cancel() => Cancelled = true;
    }


}

namespace Content.Shared.Heretic
{
    using Robust.Shared.Prototypes;

    [RegisterComponent]
    public sealed partial class RustGraspComponent : Component
    {
        [DataField]
        public float MinUseDelay = 0.7f;

        [DataField]
        public float MaxUseDelay = 3f;

        [DataField]
        public float CatwalkDelayMultiplier = 0.15f;

        [DataField]
        public string Delay = "rust";

        [DataField]
        public EntProtoId TileRune = "TileHereticRustRune";
    }
}

namespace Content.Shared._Goobstation.Wizard.Projectiles
{
    [RegisterComponent]
    public sealed partial class HomingProjectileComponent : Component
    {
        [DataField]
        public EntityUid Target;

        [DataField]
        public float HomingSpeed;
    }
}

namespace Content.Goobstation.Common.Weapons.DelayedKnockdown
{
    [RegisterComponent]
    public sealed partial class DelayedKnockdownComponent : Component {}
}

namespace Content.Goobstation.Shared.Overlays
{
    [RegisterComponent]
    public sealed partial class NightVisionComponent : Component
    {
        [DataField]
        public Color Color = Color.White;

        [DataField]
        public Robust.Shared.Audio.SoundSpecifier? ActivateSound;

        [DataField]
        public Robust.Shared.Audio.SoundSpecifier? DeactivateSound;

        [DataField]
        public bool DrawOverlay;
    }

    [RegisterComponent]
    public sealed partial class ThermalVisionComponent : Component
    {
        [DataField]
        public Color Color = Color.White;

        [DataField]
        public float LightRadius;

        [DataField]
        public float FlashDurationMultiplier;

        [DataField]
        public string? ActivateSound;

        [DataField]
        public string? DeactivateSound;

        [DataField]
        public string? ThermalShader;
    }

    public sealed class ToggleThermalVisionEvent : EntityEventArgs {}
}

namespace Content.Goobstation.Common.CCVar
{
    public static class GoobCVars
    {
        public static readonly Robust.Shared.Configuration.CVarDef<bool> AscensionRequiresObjectives =
            Robust.Shared.Configuration.CVarDef.Create("heretic.ascension_requires_objectives", true, Robust.Shared.Configuration.CVar.SERVERONLY);
    }
}

namespace Content.Shared._Goobstation.Wizard.Traps
{
    [ByRefEvent]
    public struct TrapTriggeredEvent
    {
        public EntityUid Victim;
    }

    [Serializable, Robust.Shared.Serialization.NetSerializable]
    public sealed class ButtonTagPressedEvent : EntityEventArgs
    {
        public string Id = default!;
        public NetEntity User;
        public Robust.Shared.Map.NetCoordinates Coords;
    }

    [RegisterComponent]
    public sealed partial class WizardTrapComponent : Component
    {
        [DataField]
        public HashSet<EntityUid> IgnoredMinds = new();
    }
}

namespace Content.Shared._Starlight.CollectiveMind
{
    using Robust.Shared.Prototypes;

    [RegisterComponent]
    public sealed partial class CollectiveMindComponent : Component
    {
        [DataField]
        public HashSet<ProtoId<CollectiveMindPrototype>> Channels = new();
    }
}

namespace Content.Shared.Heretic
{
    [RegisterComponent]
    public sealed partial class WeakToHolyComponent : Component
    {
        [DataField]
        public bool AlwaysTakeHoly;
    }
}

namespace Content.Shared.Body
{
    public enum GibContentsOption
    {
        Drop,
        Skip,
        All
    }
}





namespace Content.Shared._Shitmed.Damage
{
    public enum SplitDamageBehavior
    {
        SplitEnsureAll,
        Ignore,
        Split
    }
}

namespace Content.Shared.Body.Components
{
    [RegisterComponent]
    public sealed partial class BodyPartComponent : Component {}
}

namespace Content.Shared.Whitelist
{
    public static class WhitelistSystemExtensions
    {
        public static bool IsBlacklistPass(this EntityWhitelistSystem system, EntityWhitelist? blacklist, EntityUid uid)
        {
            return system.IsWhitelistPass(blacklist, uid);
        }
    }
}


