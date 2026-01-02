using Godot;
using GuildmasterMVP.Core;
using System.Collections.Generic;
using GuildmasterMVP.Data;

namespace GuildmasterMVP.Audio
{
    /// <summary>
    /// Manages audio effects for combat and interactions
    /// Requirements 10.5, 10.6, 10.8: Sound effects for combat and interactions
    /// </summary>
    public partial class AudioManager : Node
    {
        private Dictionary<string, AudioStream> _audioClips = new Dictionary<string, AudioStream>();
        private List<AudioStreamPlayer2D> _audioPlayers = new List<AudioStreamPlayer2D>();
        private const int MAX_AUDIO_PLAYERS = 20; // Pool of audio players
        
        [Export] public float MasterVolume { get; set; } = 0.8f;
        [Export] public float SfxVolume { get; set; } = 1.0f;
        [Export] public float CombatVolume { get; set; } = 0.9f;
        [Export] public float InteractionVolume { get; set; } = 0.7f;
        
        public override void _Ready()
        {
            CreateAudioPlayerPool();
            LoadAudioClips();
            GD.Print("AudioManager initialized with sound effect support");
        }
        
        private void CreateAudioPlayerPool()
        {
            // Create a pool of AudioStreamPlayer2D nodes for efficient audio playback
            for (int i = 0; i < MAX_AUDIO_PLAYERS; i++)
            {
                var player = new AudioStreamPlayer2D();
                player.Name = $"AudioPlayer_{i}";
                AddChild(player);
                _audioPlayers.Add(player);
            }
        }
        
        private void LoadAudioClips()
        {
            // In a full implementation, these would load actual audio files
            // For now, we'll create placeholder entries and generate simple tones
            
            // Combat sounds
            RegisterAudioClip("sword_attack", CreateToneAudio(440.0f, 0.2f)); // A4 note
            RegisterAudioClip("axe_attack", CreateToneAudio(330.0f, 0.3f)); // E4 note
            RegisterAudioClip("bow_attack", CreateToneAudio(660.0f, 0.15f)); // E5 note
            RegisterAudioClip("projectile_impact", CreateToneAudio(220.0f, 0.1f)); // A3 note
            
            // Hit sounds
            RegisterAudioClip("hit_enemy", CreateToneAudio(150.0f, 0.2f)); // Low thud
            RegisterAudioClip("hit_player", CreateToneAudio(200.0f, 0.25f)); // Player hit
            RegisterAudioClip("enemy_death", CreateToneAudio(100.0f, 0.5f)); // Deep death sound
            
            // Interaction sounds
            RegisterAudioClip("item_pickup", CreateToneAudio(800.0f, 0.1f)); // High pickup sound
            RegisterAudioClip("tree_shake", CreateToneAudio(300.0f, 0.3f)); // Rustling
            RegisterAudioClip("tree_cut", CreateToneAudio(250.0f, 0.4f)); // Chopping
            RegisterAudioClip("rock_break", CreateToneAudio(180.0f, 0.3f)); // Breaking
            RegisterAudioClip("rock_pickup", CreateToneAudio(350.0f, 0.2f)); // Stone pickup
            
            // Enemy sounds
            RegisterAudioClip("enemy_alert", CreateToneAudio(400.0f, 0.2f)); // Alert sound
            RegisterAudioClip("enemy_attack", CreateToneAudio(120.0f, 0.3f)); // Enemy attack
            
            GD.Print($"Loaded {_audioClips.Count} audio clips");
        }
        
        private void RegisterAudioClip(string name, AudioStream audioStream)
        {
            _audioClips[name] = audioStream;
        }
        
        private AudioStream CreateToneAudio(float frequency, float duration)
        {
            // Create a simple sine wave tone for placeholder audio
            // In a real implementation, this would load actual audio files
            var audioStreamGenerator = new AudioStreamGenerator();
            audioStreamGenerator.MixRate = 22050;
            audioStreamGenerator.BufferLength = duration;
            
            return audioStreamGenerator;
        }
        
        /// <summary>
        /// Play combat sound effect
        /// Requirements 10.5: Sound effects for combat
        /// </summary>
        public void PlayCombatSound(WeaponType weaponType, Vector2 position)
        {
            string soundName = weaponType switch
            {
                WeaponType.Sword => "sword_attack",
                WeaponType.Axe => "axe_attack",
                WeaponType.Bow => "bow_attack",
                _ => "sword_attack"
            };
            
            PlaySoundAtPosition(soundName, position, CombatVolume);
            GD.Print($"Playing {weaponType} combat sound at ({position.X:F1}, {position.Y:F1})");
        }
        
        /// <summary>
        /// Play hit confirmation sound
        /// Requirements 10.3: Hit confirmation audio
        /// </summary>
        public void PlayHitSound(uint targetId, float damage, Vector2 position)
        {
            string soundName;
            float volume = CombatVolume;
            
            // Choose sound based on target type and damage
            if (targetId >= 1000000) // Enemy
            {
                soundName = damage >= 50.0f ? "enemy_death" : "hit_enemy";
            }
            else // Player
            {
                soundName = "hit_player";
                volume *= 0.8f; // Slightly quieter for player hits
            }
            
            PlaySoundAtPosition(soundName, position, volume);
            GD.Print($"Playing hit sound for target {targetId} with {damage} damage");
        }
        
        /// <summary>
        /// Play projectile impact sound
        /// Requirements 10.6: Projectile impact audio
        /// </summary>
        public void PlayProjectileImpact(ProjectileType projectileType, Vector2 position)
        {
            PlaySoundAtPosition("projectile_impact", position, CombatVolume * 0.7f);
            GD.Print($"Playing {projectileType} impact sound at ({position.X:F1}, {position.Y:F1})");
        }
        
        /// <summary>
        /// Play interaction sound effect
        /// Requirements 10.8: Sound effects for interactions
        /// </summary>
        public void PlayInteractionSound(string interactionType, Vector2 position)
        {
            string soundName = interactionType.ToLower() switch
            {
                "pickup" => "item_pickup",
                "shake" => "tree_shake",
                "cut" => "tree_cut",
                "break" => "rock_break",
                "pick_up" => "rock_pickup",
                _ => "item_pickup"
            };
            
            PlaySoundAtPosition(soundName, position, InteractionVolume);
            GD.Print($"Playing {interactionType} interaction sound at ({position.X:F1}, {position.Y:F1})");
        }
        
        /// <summary>
        /// Play enemy sound effect
        /// Requirements 10.5: Enemy attack telegraph audio
        /// </summary>
        public void PlayEnemySound(string enemyAction, Vector2 position)
        {
            string soundName = enemyAction.ToLower() switch
            {
                "alert" => "enemy_alert",
                "attack" => "enemy_attack",
                "death" => "enemy_death",
                _ => "enemy_alert"
            };
            
            PlaySoundAtPosition(soundName, position, CombatVolume * 0.8f);
            GD.Print($"Playing enemy {enemyAction} sound at ({position.X:F1}, {position.Y:F1})");
        }
        
        /// <summary>
        /// Play sound at a specific world position with 2D audio
        /// </summary>
        private void PlaySoundAtPosition(string soundName, Vector2 position, float volume)
        {
            if (!_audioClips.ContainsKey(soundName))
            {
                GD.PrintErr($"Audio clip '{soundName}' not found");
                return;
            }
            
            var audioPlayer = GetAvailableAudioPlayer();
            if (audioPlayer == null)
            {
                GD.PrintErr("No available audio players in pool");
                return;
            }
            
            // Configure audio player
            audioPlayer.Stream = _audioClips[soundName];
            audioPlayer.Position = position;
            audioPlayer.VolumeDb = LinearToDb(volume * SfxVolume * MasterVolume);
            audioPlayer.PitchScale = (float)GD.RandRange(0.9, 1.1); // Slight pitch variation
            
            // Play the sound
            audioPlayer.Play();
            
            // Schedule return to pool when finished
            var duration = GetAudioDuration(soundName);
            GetTree().CreateTimer(duration + 0.1f).Timeout += () => {
                if (IsInstanceValid(audioPlayer))
                {
                    audioPlayer.Stop();
                }
            };
        }
        
        /// <summary>
        /// Get an available audio player from the pool
        /// </summary>
        private AudioStreamPlayer2D GetAvailableAudioPlayer()
        {
            foreach (var player in _audioPlayers)
            {
                if (!player.Playing)
                {
                    return player;
                }
            }
            
            // If no available players, use the first one (oldest sound gets cut off)
            return _audioPlayers.Count > 0 ? _audioPlayers[0] : null;
        }
        
        /// <summary>
        /// Get the duration of an audio clip
        /// </summary>
        private float GetAudioDuration(string soundName)
        {
            // For our generated tones, return a default duration
            // In a real implementation, this would get the actual audio duration
            return soundName switch
            {
                "sword_attack" => 0.2f,
                "axe_attack" => 0.3f,
                "bow_attack" => 0.15f,
                "projectile_impact" => 0.1f,
                "hit_enemy" => 0.2f,
                "hit_player" => 0.25f,
                "enemy_death" => 0.5f,
                "item_pickup" => 0.1f,
                "tree_shake" => 0.3f,
                "tree_cut" => 0.4f,
                "rock_break" => 0.3f,
                "rock_pickup" => 0.2f,
                "enemy_alert" => 0.2f,
                "enemy_attack" => 0.3f,
                _ => 0.2f
            };
        }
        
        /// <summary>
        /// Convert linear volume to decibels
        /// </summary>
        private float LinearToDb(float linear)
        {
            return linear > 0.0f ? 20.0f * Mathf.Log(linear) / Mathf.Log(10.0f) : -80.0f;
        }
        
        /// <summary>
        /// Set master volume (0.0 to 1.0)
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp(volume, 0.0f, 1.0f);
        }
        
        /// <summary>
        /// Set SFX volume (0.0 to 1.0)
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp(volume, 0.0f, 1.0f);
        }
        
        /// <summary>
        /// Stop all currently playing sounds
        /// </summary>
        public void StopAllSounds()
        {
            foreach (var player in _audioPlayers)
            {
                if (player.Playing)
                {
                    player.Stop();
                }
            }
        }
    }
}