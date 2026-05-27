using Content.Shared.Store;
using Content.Shared.Silicons.StationAi;

namespace Content.Server.Store.Conditions;

public sealed partial class MalfRollCoreCondition : ListingCondition
{
    public override bool Condition(ListingConditionArgs args)
    {
        if (args.StoreEntity == null)
            return true;

        if (args.EntityManager.TryGetComponent<MalfAiBrainComponent>(args.StoreEntity.Value, out var malf))
        {
            return !malf.HasRolledCore;
        }

        return true;
    }
}
