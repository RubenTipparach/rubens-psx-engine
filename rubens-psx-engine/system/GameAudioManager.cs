using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace rubens_psx_engine.system
{
    /// <summary>
    /// Manages game audio including sound effects and background music
    /// </summary>
    public class GameAudioManager
    {
        // Sound effects (for short, loopable sounds)
        private SoundEffect textBlipSound;
        private SoundEffectInstance textBlipInstance;
        private SoundEffect shipRumblingSound;
        private SoundEffectInstance shipRumblingInstance;
        private SoundEffect warpSpeedSound;

        // Songs (for long music tracks)
        private Song backgroundMusic;
        private Song finaleIntroMusic;
        private Song winJingle;
        private Song loseJingle;

        private ContentManager content;
        private bool isInitialized = false;

        public GameAudioManager(ContentManager contentManager)
        {
            content = contentManager;
        }

        /// <summary>
        /// Load all audio assets
        /// </summary>
        public void LoadContent()
        {
            try
            {
                Console.WriteLine("[GameAudioManager] Starting to load audio content...");

                // Load sound effects
                Console.WriteLine("[GameAudioManager] Loading text blip...");
                textBlipSound = content.Load<SoundEffect>("sound/high-text-blip");
                textBlipInstance = textBlipSound.CreateInstance();
                textBlipInstance.IsLooped = false; // Play individual blips, not continuous loop
                textBlipInstance.Volume = AudioSettings.TextBlipVolume;
                Console.WriteLine("[GameAudioManager] ✓ Text blip loaded");

                Console.WriteLine("[GameAudioManager] Loading ship rumbling...");
                shipRumblingSound = content.Load<SoundEffect>("sound/ship_rumbling");
                shipRumblingInstance = shipRumblingSound.CreateInstance();
                shipRumblingInstance.IsLooped = true;
                shipRumblingInstance.Volume = AudioSettings.ShipRumblingVolume;
                Console.WriteLine("[GameAudioManager] ✓ Ship rumbling loaded");

                Console.WriteLine("[GameAudioManager] Loading warp speed...");
                warpSpeedSound = content.Load<SoundEffect>("sound/warp-speed");
                Console.WriteLine("[GameAudioManager] ✓ Warp speed loaded");

                // Load background music
                Console.WriteLine("[GameAudioManager] Loading background music...");
                backgroundMusic = content.Load<Song>("sound/music_audio_4");
                Console.WriteLine("[GameAudioManager] ✓ Background music loaded");

                Console.WriteLine("[GameAudioManager] Loading finale intro music...");
                finaleIntroMusic = content.Load<Song>("sound/audio_8_mystery_ending");
                Console.WriteLine("[GameAudioManager] ✓ Finale intro music loaded");

                Console.WriteLine("[GameAudioManager] Loading win jingle...");
                winJingle = content.Load<Song>("sound/audio_5_win_jingle");
                Console.WriteLine("[GameAudioManager] ✓ Win jingle loaded");

                Console.WriteLine("[GameAudioManager] Loading lose jingle...");
                loseJingle = content.Load<Song>("sound/audio_6_loose");
                Console.WriteLine("[GameAudioManager] ✓ Lose jingle loaded");

                isInitialized = true;
                Console.WriteLine("[GameAudioManager] ✓✓✓ ALL Audio content loaded successfully! ✓✓✓");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameAudioManager] *** FATAL ERROR loading audio: {ex.Message}");
                Console.WriteLine($"[GameAudioManager] *** Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[GameAudioManager] *** Inner exception: {ex.InnerException.Message}");
                }
                isInitialized = false;

                // Re-throw the exception so the game crashes with a clear error message
                throw new Exception($"Failed to load audio content. See console for details.", ex);
            }
        }

        /// <summary>
        /// Play a single text blip sound (call every frame during typing for continuous blips)
        /// </summary>
        public void PlayTextBlip()
        {
            if (!isInitialized || textBlipInstance == null) return;

            // Only play if not currently playing (prevents overlapping blips)
            if (textBlipInstance.State != SoundState.Playing)
            {
                textBlipInstance.Play();
            }
        }

        /// <summary>
        /// Stop the text blip sound (usually not needed since blips are short and non-looping)
        /// </summary>
        public void StopTextBlip()
        {
            if (!isInitialized || textBlipInstance == null) return;

            if (textBlipInstance.State == SoundState.Playing)
            {
                textBlipInstance.Stop();
            }
        }

        /// <summary>
        /// Start playing background music
        /// </summary>
        public void PlayBackgroundMusic()
        {
            if (!isInitialized || backgroundMusic == null) return;

            try
            {
                // Stop any currently playing music first (MonoGame best practice)
                if (MediaPlayer.State == MediaState.Playing)
                {
                    MediaPlayer.Stop();
                }

                MediaPlayer.Volume = AudioSettings.MusicVolume;
                MediaPlayer.IsRepeating = false;
                MediaPlayer.Play(backgroundMusic);
                //Console.WriteLine("[GameAudioManager] Background music started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameAudioManager] Error playing music: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop background music
        /// </summary>
        public void StopBackgroundMusic()
        {
            if (!isInitialized) return;

            try
            {
                MediaPlayer.Stop();
                Console.WriteLine("[GameAudioManager] Background music stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameAudioManager] Error stopping music: {ex.Message}");
            }
        }

        /// <summary>
        /// Start playing ship rumbling ambient sound (loops continuously)
        /// </summary>
        public void PlayShipRumbling()
        {
            if (!isInitialized || shipRumblingInstance == null) return;

            if (shipRumblingInstance.State != SoundState.Playing)
            {
                shipRumblingInstance.Play();
                Console.WriteLine("[GameAudioManager] Ship rumbling started");
            }
        }

        /// <summary>
        /// Stop ship rumbling sound
        /// </summary>
        public void StopShipRumbling()
        {
            if (!isInitialized || shipRumblingInstance == null) return;

            if (shipRumblingInstance.State == SoundState.Playing)
            {
                shipRumblingInstance.Stop();
                Console.WriteLine("[GameAudioManager] Ship rumbling stopped");
            }
        }

        /// <summary>
        /// Play warp speed sound effect (one-shot)
        /// </summary>
        public void PlayWarpSpeed()
        {
            if (!isInitialized || warpSpeedSound == null) return;

            try
            {
                warpSpeedSound.Play(AudioSettings.WarpSpeedVolume, 0f, 0f);
                Console.WriteLine("[GameAudioManager] Warp speed sound played");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameAudioManager] Error playing warp speed: {ex.Message}");
            }
        }

        /// <summary>
        /// Start playing finale intro music (plays when "judgement is here" text appears)
        /// </summary>
        public void PlayFinaleIntroMusic()
        {
            if (!isInitialized || finaleIntroMusic == null) return;

            try
            {
                // Stop any currently playing music first
                if (MediaPlayer.State == MediaState.Playing)
                {
                    MediaPlayer.Stop();
                }

                MediaPlayer.Volume = AudioSettings.FinaleIntroMusicVolume;
                MediaPlayer.IsRepeating = false;
                MediaPlayer.Play(finaleIntroMusic);
                Console.WriteLine("[GameAudioManager] Finale intro music started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameAudioManager] Error playing finale intro music: {ex.Message}");
            }
        }

        /// <summary>
        /// Play win jingle (plays on successful case resolution)
        /// </summary>
        public void PlayWinJingle()
        {
            if (!isInitialized || winJingle == null) return;

            try
            {
                // Stop any currently playing music first
                if (MediaPlayer.State == MediaState.Playing)
                {
                    MediaPlayer.Stop();
                }

                MediaPlayer.Volume = AudioSettings.WinJingleVolume;
                MediaPlayer.IsRepeating = false;
                MediaPlayer.Play(winJingle);
                Console.WriteLine("[GameAudioManager] Win jingle started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameAudioManager] Error playing win jingle: {ex.Message}");
            }
        }

        /// <summary>
        /// Play lose jingle (plays on failed case resolution)
        /// </summary>
        public void PlayLoseJingle()
        {
            if (!isInitialized || loseJingle == null) return;

            try
            {
                // Stop any currently playing music first
                if (MediaPlayer.State == MediaState.Playing)
                {
                    MediaPlayer.Stop();
                }

                MediaPlayer.Volume = AudioSettings.LoseJingleVolume;
                MediaPlayer.IsRepeating = false;
                MediaPlayer.Play(loseJingle);
                Console.WriteLine("[GameAudioManager] Lose jingle started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameAudioManager] Error playing lose jingle: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop all audio
        /// </summary>
        public void StopAll()
        {
            StopTextBlip();
            StopBackgroundMusic();
            StopShipRumbling();
        }

        /// <summary>
        /// Clean up audio resources
        /// </summary>
        public void Dispose()
        {
            StopAll();
            textBlipInstance?.Dispose();
            shipRumblingInstance?.Dispose();
        }
    }
}
