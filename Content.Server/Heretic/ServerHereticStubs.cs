using Content.Shared.EntityEffects;
using Robust.Shared.GameObjects;
using Content.Shared.Damage;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.EntityEffects
{
    public sealed class EntityEffectSystem : EntitySystem
    {
        public void Effect(EntityEffect effect, dynamic args)
        {
            if (effect is Content.Server._Goobstation.Heretic.Effects.RemoveAccess removeAccess)
            {
                removeAccess.Effect(args);
            }
            else if (effect is Content.Server._Goobstation.Heretic.Effects.SpillBlood spillBlood)
            {
                spillBlood.Effect(args);
            }
            else if (effect is Content.Server._Goobstation.Heretic.Effects.VoidCurse voidCurse)
            {
                voidCurse.Effect(args);
            }
        }
    }
}

namespace Content.Server.Antag
{
    using Robust.Shared.Player;
    using System.Collections.Generic;
    using System.Linq;
    using Content.Shared.Mobs.Components;
    using Content.Shared.Mobs;

    public static class AntagSelectionSystemExtensions
    {
        public static IEnumerable<ICommonSession> GetAliveConnectedPlayers(this AntagSelectionSystem system, IEnumerable<ICommonSession> sessions)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var mobStateQuery = entMan.GetEntityQuery<MobStateComponent>();
            return sessions.Where(s =>
            {
                if (s.AttachedEntity is not { } ent)
                    return false;
                if (!mobStateQuery.TryComp(ent, out var mobState))
                    return false;
                return mobState.CurrentState == MobState.Alive;
            });
        }
    }
}

namespace Content.Server.Heretic.Components
{
    [RegisterComponent]
    public sealed partial class DamageOverTimeComponent : Component
    {
        [DataField]
        public DamageSpecifier Damage = new();

        [DataField]
        public float MultiplierIncrease;

        [DataField]
        public bool IgnoreResistances;
    }
}

namespace Content.Server.Heretic.Components.PathSpecific
{
    [RegisterComponent]
    public sealed partial class DisgustComponent : Component
    {
        [DataField]
        public float AccumulationMultiplier;
    }

    [RegisterComponent]
    public sealed partial class ImmovableVoidRodComponent : Component
    {
        [DataField]
        public EntityUid? User;
    }
}

namespace Content.Server.Polymorph.Systems
{
    using Robust.Shared.Serialization.Manager;

    public static class PolymorphSystemExtensions
    {
        public static void CopyPolymorphComponent<T>(this PolymorphSystem polymorphSystem, EntityUid oldEnt, EntityUid newEnt) where T : Component, new()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            if (entityManager.TryGetComponent<T>(oldEnt, out var component))
            {
                var newComp = entityManager.EnsureComponent<T>(newEnt);
                var serManager = IoCManager.Resolve<ISerializationManager>();
                serManager.CopyTo(component, ref newComp, notNullableOverride: true);
            }
        }
    }
}

namespace Content.Server.Weapons.Ranged.Systems
{
    public static class GunSystemExtensions
    {
        public static void SetTarget(this GunSystem gunSystem, EntityUid ent, EntityUid? target, out EntityUid? targetOut)
        {
            targetOut = null;
        }
    }
}

namespace Content.Shared.Weapons.Reflect
{
    using System.Reflection;
    using System.Numerics;

    public static class ReflectSystemExtensions
    {
        private static MethodInfo? _tryReflectProjectileMethod;
        private static MethodInfo? _tryReflectHitscanMethod;

        public static bool TryReflectProjectile(this ReflectSystem system, Robust.Shared.GameObjects.Entity<ReflectComponent> reflector, EntityUid user, EntityUid projectile)
        {
            _tryReflectProjectileMethod ??= typeof(ReflectSystem).GetMethod("TryReflectProjectile", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_tryReflectProjectileMethod == null)
                return false;

            var entManager = IoCManager.Resolve<IEntityManager>();
            var projComp = entManager.GetComponentOrNull<Content.Shared.Projectiles.ProjectileComponent>(projectile);
            var projEntity = new Robust.Shared.GameObjects.Entity<Content.Shared.Projectiles.ProjectileComponent?>(projectile, projComp);

            return (bool)_tryReflectProjectileMethod.Invoke(system, new object[] { reflector, user, projEntity })!;
        }

        public static bool TryReflectHitscan(this ReflectSystem system, Robust.Shared.GameObjects.Entity<ReflectComponent> reflector, EntityUid user, EntityUid? shooter, EntityUid shotSource, Vector2 direction, ReflectType hitscanReflectType, float damage, out Vector2? newDirection)
        {
            newDirection = null;
            _tryReflectHitscanMethod ??= typeof(ReflectSystem).GetMethod("TryReflectHitscan", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_tryReflectHitscanMethod == null)
                return false;

            var parameters = new object?[] { reflector, user, shooter, shotSource, direction, hitscanReflectType, null };
            var result = (bool)_tryReflectHitscanMethod.Invoke(system, parameters)!;
            newDirection = (Vector2?)parameters[6];
            return result;
        }
    }
}

namespace Content.Server.Ghost.Roles.Components
{
    using Robust.Shared.GameObjects;

    public sealed partial class GhostTakeoverAvailableComponent
    {
        [Access(Other = AccessPermissions.ReadWriteExecute)]
        public bool IgnoreMindCheck { get; set; }
    }
}

namespace Content.Server.Ghost.Roles
{
    using Content.Server.Ghost.Roles.Components;
    using System.Reflection;

    public static class GhostRoleSystemExtensions
    {
        private static PropertyInfo? _takenProperty;

        public static void SetTaken(this GhostRoleSystem system, GhostRoleComponent component, bool taken)
        {
            _takenProperty ??= typeof(GhostRoleComponent).GetProperty("Taken");
            _takenProperty?.SetValue(component, taken);
        }
    }
}
