using Content.Shared.Tag;
using Content.Shared.Clothing.Components;
using Content.Shared.Climbing.Components;
using Content.Shared.Stacks;
using Content.Shared.Placeable;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.EntitySystems;

public sealed class HereticTagAssignerSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TagComponent, ComponentStartup>(OnTagStartup);
        SubscribeLocalEvent<PlaceableSurfaceComponent, ComponentInit>(OnPlaceableSurfaceInit);
        SubscribeLocalEvent<ClothingComponent, ComponentInit>(OnClothingInit);
        SubscribeLocalEvent<StackComponent, ComponentInit>(OnStackInit);
    }

    private void OnTagStartup(EntityUid uid, TagComponent component, ComponentStartup args)
    {
        AssignHereticTags(uid);
    }

    private void OnPlaceableSurfaceInit(EntityUid uid, PlaceableSurfaceComponent component, ComponentInit args)
    {
        AssignHereticTags(uid);
    }

    private void OnClothingInit(EntityUid uid, ClothingComponent component, ComponentInit args)
    {
        AssignHereticTags(uid);
    }

    private void OnStackInit(EntityUid uid, StackComponent component, ComponentInit args)
    {
        AssignHereticTags(uid);
    }

    private void AssignHereticTags(EntityUid uid)
    {
        var proto = Prototype(uid);
        if (proto == null)
            return;

        var id = proto.ID;

        // 1. Table
        if (HasComp<PlaceableSurfaceComponent>(uid) && HasComp<ClimbableComponent>(uid))
        {
            _tagSystem.AddTag(uid, "Table");
        }

        // 2. GasMask
        if (TryComp<ClothingComponent>(uid, out var clothing) &&
            clothing.Slots.HasFlag(SlotFlags.MASK) &&
            id.Contains("gas", StringComparison.OrdinalIgnoreCase) &&
            id.Contains("mask", StringComparison.OrdinalIgnoreCase))
        {
            _tagSystem.AddTag(uid, "GasMask");
        }

        // 3. SheetGlass
        if (TryComp<StackComponent>(uid, out var stack))
        {
            var stackType = stack.StackTypeId;
            if (stackType.ToString() == "Glass")
            {
                _tagSystem.AddTag(uid, "SheetGlass");
            }
        }
        else if (id.StartsWith("SheetGlass", StringComparison.OrdinalIgnoreCase))
        {
            _tagSystem.AddTag(uid, "SheetGlass");
        }

        // 4. Steel
        if (TryComp<StackComponent>(uid, out var stackSteel))
        {
            var stackType = stackSteel.StackTypeId;
            if (stackType.ToString() == "Steel")
            {
                _tagSystem.AddTag(uid, "Steel");
            }
        }
        else if (id.StartsWith("SheetSteel", StringComparison.OrdinalIgnoreCase))
        {
            _tagSystem.AddTag(uid, "Steel");
        }

        // 5. CableCoil
        if (TryComp<StackComponent>(uid, out var stackCable))
        {
            var stackType = stackCable.StackTypeId;
            if (stackType.ToString().StartsWith("Cable"))
            {
                _tagSystem.AddTag(uid, "CableCoil");
            }
        }
        else if (id.StartsWith("Cable", StringComparison.OrdinalIgnoreCase) &&
                 (id.Contains("Stack", StringComparison.OrdinalIgnoreCase) || id.Contains("Coil", StringComparison.OrdinalIgnoreCase)))
        {
            _tagSystem.AddTag(uid, "CableCoil");
        }

        // 6. Matchstick
        if (id.Contains("match", StringComparison.OrdinalIgnoreCase))
        {
            _tagSystem.AddTag(uid, "Matchstick");
        }

        // 7. Lighter
        if (id.Contains("lighter", StringComparison.OrdinalIgnoreCase))
        {
            _tagSystem.AddTag(uid, "Lighter");
        }

        // 8. Stunbaton
        if (id.Contains("stunbaton", StringComparison.OrdinalIgnoreCase))
        {
            _tagSystem.AddTag(uid, "Stunbaton");
        }

        // 9. Muzzle
        if (id.Contains("muzzle", StringComparison.OrdinalIgnoreCase))
        {
            _tagSystem.AddTag(uid, "Muzzle");
        }

        // 10. Flare
        if (id.Contains("flare", StringComparison.OrdinalIgnoreCase))
        {
            _tagSystem.AddTag(uid, "Flare");
        }

        // 11. Ash
        if (id.Equals("Ash", StringComparison.OrdinalIgnoreCase) || id.StartsWith("StackAsh", StringComparison.OrdinalIgnoreCase))
        {
            _tagSystem.AddTag(uid, "Ash");
        }

        // 12. Bedsheet
        if (id.Contains("bedsheet", StringComparison.OrdinalIgnoreCase))
        {
            _tagSystem.AddTag(uid, "Bedsheet");
        }

        // 13. WinterCoat
        if (id.Contains("wintercoat", StringComparison.OrdinalIgnoreCase))
        {
            _tagSystem.AddTag(uid, "WinterCoat");
        }

        // 14. SheetPlasma
        if (TryComp<StackComponent>(uid, out var stackPlasma))
        {
            var stackType = stackPlasma.StackTypeId;
            if (stackType.ToString() == "Plasma")
            {
                _tagSystem.AddTag(uid, "SheetPlasma");
            }
        }
        else if (id.StartsWith("SheetPlasma", StringComparison.OrdinalIgnoreCase))
        {
            _tagSystem.AddTag(uid, "SheetPlasma");
        }

        // 15. WaterTank
        if (id.Contains("watertank", StringComparison.OrdinalIgnoreCase))
        {
            _tagSystem.AddTag(uid, "WaterTank");
        }
    }
}
