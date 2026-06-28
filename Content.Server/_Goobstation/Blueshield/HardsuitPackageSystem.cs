using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Goobstation.Blueshield;

public sealed class HardsuitPackageSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HardsuitPackageComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HardsuitPackageComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnUseInHand(EntityUid uid, HardsuitPackageComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        _popup.PopupEntity("Right-click this package to choose which hardsuit to unwrap!", uid, args.User);
        args.Handled = true;
    }

    private void OnGetVerbs(EntityUid uid, HardsuitPackageComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        AlternativeVerb heavyVerb = new()
        {
            Act = () => Unwrap(uid, component, args.User, component.HeavyProto),
            Text = "Unwrap BSO heavy hardsuit",
            Priority = 2
        };

        AlternativeVerb lightVerb = new()
        {
            Act = () => Unwrap(uid, component, args.User, component.LightProto),
            Text = "Unwrap BSO light hardsuit",
            Priority = 1
        };

        args.Verbs.Add(heavyVerb);
        args.Verbs.Add(lightVerb);
    }

    private void Unwrap(EntityUid uid, HardsuitPackageComponent component, EntityUid user, string hardsuitProto)
    {
        var userCoords = Transform(user).Coordinates;
        
        // Spawn the selected hardsuit
        var spawned = Spawn(hardsuitProto, userCoords);
        
        // Play unwrap sound
        _audio.PlayPvs(component.UnwrapSound, uid);
        
        // Delete the package
        QueueDel(uid);

        _popup.PopupEntity($"You unwrapped the {Name(spawned)}!", user, user);
    }
}
