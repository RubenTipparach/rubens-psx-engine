using System;

namespace rubens_psx_engine.system
{
    /// <summary>
    /// Static audio volume settings for game sounds and music
    /// </summary>
    public static class AudioSettings
    {
        /// <summary>
        /// Volume for text typing blip sound (loops during teletype effect)
        /// Range: 0.0 to 1.0
        /// </summary>
        public const float TextBlipVolume = 0.5f;

        /// <summary>
        /// Volume for background music (plays when game starts after pressing E)
        /// Range: 0.0 to 1.0
        /// </summary>
        public const float MusicVolume = 0.5f;

        /// <summary>
        /// Volume for ship ambient rumbling (loops continuously throughout game)
        /// Range: 0.0 to 1.0
        /// </summary>
        public const float ShipRumblingVolume = 1.0f;

        /// <summary>
        /// Volume for warp speed sound effect (plays when Odysseus ship spawns)
        /// Range: 0.0 to 1.0
        /// </summary>
        public const float WarpSpeedVolume = 0.5f;

        /// <summary>
        /// Volume for finale intro music (plays when "judgement is here" text appears)
        /// Range: 0.0 to 1.0
        /// </summary>
        public const float FinaleIntroMusicVolume = 0.6f;

        /// <summary>
        /// Volume for win jingle (plays on successful case resolution)
        /// Range: 0.0 to 1.0
        /// </summary>
        public const float WinJingleVolume = 0.7f;

        /// <summary>
        /// Volume for lose jingle (plays on failed case resolution)
        /// Range: 0.0 to 1.0
        /// </summary>
        public const float LoseJingleVolume = 0.7f;
    }
}
