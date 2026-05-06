using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.Species.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Chat;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Temperature.Components;
using Content.Server.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Interaction;
using Content.Shared.FixedPoint;
using Robust.Shared.Log;
using System;

namespace Content.Server.Species
{
    public sealed class SteamgemSystem : EntitySystem
    {
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly AtmosphereSystem _atmos = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("species.steamgem");

            SubscribeLocalEvent<SteamgemComponent, EntitySpokeEvent>(OnEntitySpoke);
            SubscribeLocalEvent<SteamgemComponent, SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<SteamgemComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
            SubscribeLocalEvent<SteamgemComponent, EmoteEvent>(OnEmote);
            SubscribeLocalEvent<SteamgemComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnEmote(EntityUid uid, SteamgemComponent component, ref EmoteEvent args)
        {
            if (args.Handled)
                return;

            if (args.Emote.ID == "Scream")
            {
                _audio.PlayPvs(new SoundPathSpecifier("/Textures/Mobs/Species/Steamgem/sfx/scream.ogg"), uid);
                args.Handled = true;
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<SteamgemComponent, FlammableComponent, TemperatureComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var steamgem, out var flammable, out var temp, out var xform))
            {
                if (TryComp<MobStateComponent>(uid, out var mobState) && mobState.CurrentState != MobState.Alive)
                {
                    steamgem.IsHeaterActive = false;
                    continue;
                }

                // Internal Heater Logic
                if (temp.CurrentTemperature < 275f)
                {
                    if (_timing.CurTime >= steamgem.NextIgniteAttempt)
                    {
                        steamgem.NextIgniteAttempt = _timing.CurTime + TimeSpan.FromSeconds(5);
                        
                        var air = _atmos.GetContainingMixture(uid);
                        var hasOxygen = air != null && air.GetMoles(Gas.Oxygen) > 0.05f;

                        if (!hasOxygen && TryComp<InternalsComponent>(uid, out var internals))
                        {
                            if (internals.GasTankEntity != null)
                                hasOxygen = true;
                        }

                        if (hasOxygen && !steamgem.IsHeaterActive)
                        {
                            steamgem.IsHeaterActive = true;
                            _popup.PopupEntity("A flame ignites in your chest!", uid, uid, PopupType.Medium);
                            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/generator-tug-1.ogg"), uid);
                        }
                    }
                }

                if (steamgem.IsHeaterActive)
                {
                    temp.CurrentTemperature += 8f * frameTime;

                    if (temp.CurrentTemperature >= 310.15f)
                    {
                        steamgem.IsHeaterActive = false;
                    }
                }

                // Reservoir & Fire logic
                if (flammable.OnFire)
                {
                    bool hasWater = false;
                    if (TryComp<SteamgemReservoirComponent>(uid, out var res))
                    {
                        if (res.Water > 0)
                            hasWater = true;
                    }

                    if (hasWater && steamgem.WaterStream == null)
                    {
                        steamgem.WaterStream = _audio.PlayPvs(steamgem.WaterSound, uid, AudioParams.Default.WithLoop(true))?.Entity;
                        _popup.PopupEntity("Steam begins venting from your joints!", uid, uid, PopupType.Medium);
                        _movementSpeed.RefreshMovementSpeedModifiers(uid);
                    }
                    
                    if (hasWater && _timing.CurTime >= steamgem.NextSteamTime)
                    {
                        EntityManager.SpawnEntity("Smoke", _transform.GetMoverCoordinates(uid));
                        steamgem.NextSteamTime = _timing.CurTime + TimeSpan.FromSeconds(1.5f);

                        if (TryComp<SteamgemReservoirComponent>(uid, out var reservoir))
                        {
                            reservoir.Water = Math.Max(0, (float)reservoir.Water - 20);
                        }
                    }
                }
                else if (steamgem.WaterStream != null)
                {
                    _audio.Stop(steamgem.WaterStream);
                    steamgem.WaterStream = null;
                    _movementSpeed.RefreshMovementSpeedModifiers(uid);
                }
            }
        }

        private void OnRefreshSpeed(EntityUid uid, SteamgemComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            if (component.WaterStream != null)
            {
                args.ModifySpeed(0.6f, 0.6f);
            }
        }

        private void OnAfterInteract(EntityUid uid, SteamgemComponent component, AfterInteractEvent args)
        {
            if (args.Target != uid || !args.Used.Valid || !args.CanReach)
                return;

            if (!TryComp<SteamgemReservoirComponent>(uid, out var reservoir))
                return;

            if (!_solution.TryGetSolution(args.Used, "drink", out var solEnt, out var solComp) &&
                !_solution.TryGetSolution(args.Used, "food", out solEnt, out solComp))
            {
                return;
            }

            var waterAmount = solComp.GetTotalPrototypeQuantity("Water");
            if (waterAmount <= 0)
            {
                _popup.PopupEntity("This doesn't contain usable water!", uid, uid);
                return;
            }

            var transfer = Math.Min((float)waterAmount, (float)(reservoir.MaxWater - reservoir.Water));
            if (transfer <= 0)
            {
                _popup.PopupEntity("Internal reservoir is full!", uid, uid);
                return;
            }

            _solution.RemoveReagent(solEnt.Value, new ReagentId("Water", null), (FixedPoint2)transfer);
            reservoir.Water += (FixedPoint2)transfer;
            
            _popup.PopupEntity($"Refilled {transfer} units of water.", uid, uid, PopupType.Medium);
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/refill.ogg"), uid);
            args.Handled = true;
        }

        private void OnEntitySpoke(EntityUid uid, SteamgemComponent component, EntitySpokeEvent args)
        {
            if (!TryComp<SpeechComponent>(uid, out var speech))
                return;

            var currentTime = _timing.CurTime;
            var cooldown = TimeSpan.FromSeconds(speech.SoundCooldownTime);

            if (currentTime - speech.LastTimeSoundPlayed < cooldown)
                return;

            speech.LastTimeSoundPlayed = currentTime;
            _audio.PlayPvs(new SoundPathSpecifier("/Textures/Mobs/Species/Steamgem/sfx/voice.ogg"), uid, AudioParams.Default.WithVolume(-3f));
        }

        private void OnSpeakAttempt(EntityUid uid, SteamgemComponent component, SpeakAttemptEvent args)
        {
            if (args.Cancelled)
            {
                _audio.PlayPvs(component.PrintingSound, uid);
                _popup.PopupEntity(Loc.GetString("steamgem-blank-paper"), uid, uid, PopupType.Medium);
            }
        }
    }
}
