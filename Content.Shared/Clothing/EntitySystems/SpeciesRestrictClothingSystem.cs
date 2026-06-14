using Content.Shared.Humanoid;
using Content.Shared.Inventory.Events;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class SpeciesRestrictClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpeciesRestrictClothingComponent, BeingEquippedAttemptEvent>(OnBeingEquippedAttempt);
    }

    private void OnBeingEquippedAttempt(EntityUid uid, SpeciesRestrictClothingComponent component, BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // Check if target has HumanoidAppearanceComponent to determine their species
        if (!TryComp<HumanoidAppearanceComponent>(args.EquipTarget, out var appearance))
        {
            // If they aren't humanoid (e.g. non-humanoid mobs), they can't wear it
            args.Reason = "clothing-species-restrict-fail";
            args.Cancel();
            return;
        }

        if (!component.AllowedSpecies.Contains(appearance.Species))
        {
            args.Reason = "clothing-species-restrict-fail";
            args.Cancel();
        }
    }
}
