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
using Content.Server.Body.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
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
        [Dependency] private readonly Content.Server.Atmos.EntitySystems.AtmosphereSystem _atmos = default!;
        [Dependency] private readonly Content.Server.Atmos.EntitySystems.FlammableSystem _flammable = default!;
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
                var scream = _audio.PlayPvs(new SoundPathSpecifier("/Textures/Mobs/Species/Steamgem/sfx/scream.ogg"), uid);
                if (scream != null)
                {
                    Timer.Spawn(1000, () => _audio.Stop(scream.Value.Entity));
                }
                args.Handled = true;
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<SteamgemComponent, FlammableComponent, TemperatureComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var steamgem, out var flammable, out var temp, out var xform))
            {
                // Don't do anything if dead or in crit
                if (TryComp<MobStateComponent>(uid, out var mobState) && mobState.CurrentState != MobState.Alive)
                {
                    steamgem.IsHeaterActive = false;
                    if (TryComp<PointLightComponent>(uid, out var light))
                        light.Enabled = false;
                    continue;
                }

                // Internal Heater Logic
                // Threshold: 275K (approx 2C)
                if (temp.CurrentTemperature < 275f)
                {
                    if (_timing.CurTime >= steamgem.NextIgniteAttempt)
                    {
                        steamgem.NextIgniteAttempt = _timing.CurTime + TimeSpan.FromSeconds(5);
                        
                        var air = _atmos.GetContainingMixture(uid);
                        var hasOxygen = air != null && air.GetMoles(Gas.Oxygen) > 0.05f;

                        // Check internals if environment is vacuum
                        if (!hasOxygen && TryComp<InternalsComponent>(uid, out var internals))
                        {
                            if (internals.GasTankEntity != null)
                                hasOxygen = true;
                        }

                        if (hasOxygen)
                        {
                            if (!steamgem.IsHeaterActive)
                            {
                                steamgem.IsHeaterActive = true;
                                _popup.PopupEntity("A flame ignites in your chest!", uid, uid, PopupType.Medium);
                                try { _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/generator-tug-1.ogg"), uid); } catch { }
                            }
                        }
                        else if (!steamgem.IsHeaterActive)
                        {
                            if (_timing.CurTime >= steamgem.NextIgniteMessage)
                            {
                                steamgem.NextIgniteMessage = _timing.CurTime + TimeSpan.FromSeconds(5);
                                _popup.PopupEntity("The flame won't ignite!", uid, uid, PopupType.MediumCaution);
                                try { _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/generator-tug-1-empty.ogg"), uid); } catch { }
                            }
                        }
                    }
                }

                if (steamgem.IsHeaterActive)
                {
                    // Warm up!
                    temp.CurrentTemperature += 8f * frameTime;

                    var light = EnsureComp<PointLightComponent>(uid);
                    light.Enabled = true;
                    light.Color = Color.FromHex("#ffaa55");
                    light.Radius = 3.5f;
                    light.Energy = 2.0f;

                    if (temp.CurrentTemperature >= 310.15f)
                    {
                        steamgem.IsHeaterActive = false;
                        light.Enabled = false;
                    }

                    // Check if we lost oxygen while heating
                    if (_timing.CurTime >= steamgem.NextIgniteAttempt)
                    {
                         var air = _atmos.GetContainingMixture(uid);
                         var hasOxygen = air != null && air.GetMoles(Gas.Oxygen) > 0.02f;

                         if (!hasOxygen && TryComp<InternalsComponent>(uid, out var internals))
                         {
                             if (internals.GasTankEntity != null)
                                 hasOxygen = true;
                         }

                         if (!hasOxygen)
                         {
                             steamgem.IsHeaterActive = false;
                             light.Enabled = false;
                             _popup.PopupEntity("The internal flame died out!", uid, uid, PopupType.MediumCaution);
                         }
                    }
                }

                bool isThirsty = false;
                if (TryComp<ThirstComponent>(uid, out var thirst))
                {
                    if (thirst.CurrentThirstThreshold <= ThirstThreshold.Thirsty)
                        isThirsty = true;
                }

                // Check reservoir
                bool hasWater = true;
                if (TryComp<SteamgemReservoirComponent>(uid, out var reservoir))
                {
                    if (reservoir.Water <= 0)
                        hasWater = false;
                }

                if (flammable.OnFire && (hasWater || !isThirsty))
                {
                    if (steamgem.WaterStream == null)
                    {
                        steamgem.WaterStream = _audio.PlayPvs(steamgem.WaterSound, uid, AudioParams.Default.WithLoop(true))?.Entity;
                        _popup.PopupEntity(Loc.GetString("steamgem-filling-water"), uid, uid, PopupType.Medium);
                        
                        // Slower extinction (was -2.0f)
                        flammable.FirestackFade = -0.4f;
                        _movementSpeed.RefreshMovementSpeedModifiers(uid);
                    }
                    
                    // Periodic steam (every 1.5 seconds)
                    if (_timing.CurTime >= steamgem.NextSteamTime)
                    {
                        EntityManager.SpawnEntity("Smoke", _transform.GetMoverCoordinates(uid));
                        steamgem.NextSteamTime = _timing.CurTime + TimeSpan.FromSeconds(1.5f);

                        // Consume water
                        if (TryComp<SteamgemReservoirComponent>(uid, out var res))
                        {
                            res.Water -= 20;
                            if (res.Water < 0) res.Water = 0;
                        }

                        try
                        {
                            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/smoke.ogg"), uid, AudioParams.Default.WithVolume(-8f));
                        }
                        catch (Exception e)
                        {
                            _sawmill.Error($"Steam sound error: {e}");
                        }
                    }
                }
                else if (steamgem.WaterStream != null)
                {
                    _audio.Stop(steamgem.WaterStream);
                    steamgem.WaterStream = null;
                    
                    flammable.FirestackFade = -0.1f;
                    _movementSpeed.RefreshMovementSpeedModifiers(uid);
                }
            }
        }

        private void OnRefreshSpeed(EntityUid uid, SteamgemComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            if (component.WaterStream != null)
            {
                args.ModifySpeed(0.5f, 0.5f);
            }
        }

        private void OnAfterInteract(EntityUid uid, SteamgemComponent component, AfterInteractEvent args)
        {
            if (args.Target != uid || args.Used == null || !args.CanReach)
                return;

            if (!TryComp<SteamgemReservoirComponent>(uid, out var reservoir))
                return;

            if (_solution.TryGetSolution(args.Used.Value, "drink", out var solEnt, out var solution) ||
                _solution.TryGetSolution(args.Used.Value, "food", out var solEnt2, out var solution2))
            {
                var sol = solution ?? solution2;
                if (sol == null) return;

                // Check for water
                var waterAmount = sol.GetReagentQuantity("Water");
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

                _solution.RemoveReagent(solEnt ?? solEnt2!.Value, "Water", (Content.Shared.FixedPoint.FixedPoint2)transfer);
                reservoir.Water += (Content.Shared.FixedPoint.FixedPoint2)transfer;
                
                _popup.PopupEntity($"Refilled {transfer} units of water into internal reservoir.", uid, uid, PopupType.Medium);
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/refill.ogg"), uid);
                args.Handled = true;
            }
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

            try
            {
                var audioParams = AudioParams.Default
                    .WithVolume(-2f);
                
                _audio.PlayPvs(new SoundPathSpecifier("/Textures/Mobs/Species/Steamgem/sfx/voice.ogg"), uid, audioParams);
            }
            catch (Exception e)
            {
                // File might be corrupted or invalid OGG
                _sawmill.Error($"Steamgem audio crash: {e}");
            }
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
