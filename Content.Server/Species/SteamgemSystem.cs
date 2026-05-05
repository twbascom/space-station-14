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

namespace Content.Server.Species
{
    public sealed class SteamgemSystem : EntitySystem
    {
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SteamgemComponent, EntitySpokeEvent>(OnEntitySpoke);
            SubscribeLocalEvent<SteamgemComponent, SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<SteamgemComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
            SubscribeLocalEvent<SteamgemComponent, EmoteEvent>(OnEmote);
        }

        private void OnEmote(EntityUid uid, SteamgemComponent component, ref EmoteEvent args)
        {
            if (args.Handled)
                return;

            if (args.Emote.ID == "Scream")
            {
                var scream = _audio.PlayPvs("/Textures/Mobs/Species/Steamgem/sfx/scream.ogg", uid);
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

            var query = EntityQueryEnumerator<SteamgemComponent, FlammableComponent>();
            while (query.MoveNext(out var uid, out var steamgem, out var flammable))
            {
                bool isThirsty = false;
                if (TryComp<ThirstComponent>(uid, out var thirst))
                {
                    if (thirst.CurrentThirstThreshold <= ThirstThreshold.Thirsty)
                        isThirsty = true;
                }

                if (flammable.OnFire && !isThirsty)
                {
                    if (steamgem.WaterStream == null)
                    {
                        steamgem.WaterStream = _audio.PlayPvs(steamgem.WaterSound, uid, AudioParams.Default.WithLoop(true))?.Entity;
                        _popup.PopupEntity(Loc.GetString("steamgem-filling-water"), uid, uid, PopupType.Medium);
                        
                        EntityManager.SpawnEntity("EffectSmoke", Transform(uid).Coordinates);

                        // Faster extinction
                        flammable.FirestackFade = -2.0f;
                        _movementSpeed.RefreshMovementSpeedModifiers(uid);
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

        private void OnEntitySpoke(EntityUid uid, SteamgemComponent component, EntitySpokeEvent args)
        {
            var startOffset = 2f + _random.NextFloat(0f, 30f); 
            var audioParams = AudioParams.Default
                .WithVolume(-2f)
                .WithPlayOffset(startOffset);
            
            _audio.PlayPvs("/Textures/Mobs/Species/Steamgem/sfx/voice.ogg", uid, audioParams);

            var humanSounds = new[] { "/Audio/Voice/Human/male_say1.ogg", "/Audio/Voice/Human/female_say1.ogg" };
            _audio.PlayPvs(_random.Pick(humanSounds), uid, AudioParams.Default.WithVolume(-12f));
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
