using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// Unified character profile containing all metadata for a character
    /// Includes dialogue, portrait, name, and extensible data for future states
    /// </summary>
    public class CharacterProfile
    {
        // Core Identity
        public string Id { get; set; }  // Unique identifier (e.g., "bartender", "pathologist")
        public string Name { get; set; }
        public string Role { get; set; }
        public string Species { get; set; }

        // Portrait system
        public string PortraitPath { get; set; }  // Path to portrait texture
        public string PortraitKey { get; set; }   // Key for portrait lookup (derived from name/path)
        public Texture2D Portrait { get; set; }   // Cached portrait texture

        // Personality & metadata
        public string Personality { get; set; }
        public string Pronouns { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }

        // Investigation data (for murder mystery)
        public string PublicStory { get; set; }
        public string Secret { get; set; }
        public bool IsKiller { get; set; }
        public string KillerType { get; set; }  // "primary", "accomplice", etc.
        public bool IsRedHerring { get; set; }
        public List<string> KeyEvidence { get; set; }

        // Dialogue states (extensible for future content)
        // Each key represents a dialogue state name, value is the sequence name
        public Dictionary<string, string> DialogueStates { get; set; }

        // Character flags (for tracking progress and unlocking dialogue)
        public Dictionary<string, bool> Flags { get; set; }

        // Custom data (extensible for game-specific needs)
        public Dictionary<string, object> CustomData { get; set; }

        public CharacterProfile()
        {
            KeyEvidence = new List<string>();
            DialogueStates = new Dictionary<string, string>();
            Flags = new Dictionary<string, bool>();
            CustomData = new Dictionary<string, object>();
        }

        /// <summary>
        /// Get a dialogue sequence name for a given state
        /// </summary>
        public string GetDialogueSequence(string stateName)
        {
            return DialogueStates.ContainsKey(stateName) ? DialogueStates[stateName] : null;
        }

        /// <summary>
        /// Check if character has a specific flag set
        /// </summary>
        public bool HasFlag(string flagName)
        {
            return Flags.ContainsKey(flagName) && Flags[flagName];
        }

        /// <summary>
        /// Set a character flag
        /// </summary>
        public void SetFlag(string flagName, bool value)
        {
            Flags[flagName] = value;
        }

        /// <summary>
        /// Get custom data value
        /// </summary>
        public T GetCustomData<T>(string key, T defaultValue = default)
        {
            if (CustomData.ContainsKey(key) && CustomData[key] is T value)
            {
                return value;
            }
            return defaultValue;
        }

        /// <summary>
        /// Set custom data value
        /// </summary>
        public void SetCustomData(string key, object value)
        {
            CustomData[key] = value;
        }

        /// <summary>
        /// Check if character is available for interrogation
        /// </summary>
        public bool IsInterrogatable => !string.IsNullOrEmpty(PublicStory) || KeyEvidence.Count > 0;

        /// <summary>
        /// Get a short description for UI display
        /// </summary>
        public string GetShortDescription()
        {
            return $"{Name} - {Role}";
        }

        /// <summary>
        /// Get full description including species
        /// </summary>
        public string GetFullDescription()
        {
            var parts = new List<string> { Name };
            if (!string.IsNullOrEmpty(Species)) parts.Add(Species);
            if (!string.IsNullOrEmpty(Role)) parts.Add(Role);
            return string.Join(" - ", parts);
        }
    }
}
