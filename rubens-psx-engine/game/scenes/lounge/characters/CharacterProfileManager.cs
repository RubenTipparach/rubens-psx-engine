using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine;
using System;
using System.Collections.Generic;
using System.Linq;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// Manages character profiles and integrates dialogue, portraits, and metadata
    /// </summary>
    public class CharacterProfileManager
    {
        private Dictionary<string, CharacterProfile> profiles;
        private Dictionary<string, Texture2D> portraitCache;

        public CharacterProfileManager()
        {
            profiles = new Dictionary<string, CharacterProfile>();
            portraitCache = new Dictionary<string, Texture2D>();
        }

        /// <summary>
        /// Load all character profiles from YAML configuration
        /// </summary>
        public void LoadFromYaml(LoungeCharactersData yamlData)
        {
            profiles.Clear();

            // Helper to create profile from YAML
            void CreateProfile(string id, CharacterConfig config)
            {
                if (config == null) return;

                var profile = new CharacterProfile
                {
                    Id = id,
                    Name = config.name,
                    Role = config.role,
                    Species = config.species,
                    PortraitPath = config.portrait,
                    Personality = config.personality,
                    Pronouns = config.pronouns,
                    Gender = config.gender,
                    Age = config.age,
                    PublicStory = config.public_story,
                    Secret = config.secret,
                    IsKiller = config.is_killer,
                    KillerType = config.killer_type,
                    IsRedHerring = config.red_herring
                };

                // Generate portrait key from name (normalize for lookup)
                profile.PortraitKey = GeneratePortraitKey(config.name);

                // Copy key evidence
                if (config.key_evidence != null)
                {
                    profile.KeyEvidence.AddRange(config.key_evidence);
                }

                // Map dialogue sequences to states
                if (config.dialogue != null)
                {
                    foreach (var seq in config.dialogue)
                    {
                        profile.DialogueStates[seq.sequence_name] = seq.sequence_name;
                    }
                }

                profiles[id] = profile;
                Console.WriteLine($"[CharacterProfileManager] Loaded profile: {profile.Name} ({id})");
            }

            // Load all characters
            CreateProfile("bartender", yamlData.bartender);
            CreateProfile("pathologist", yamlData.pathologist);
            CreateProfile("commander_von", yamlData.commander_von);
            CreateProfile("dr_thorne", yamlData.dr_thorne);
            CreateProfile("lt_webb", yamlData.lt_webb);
            CreateProfile("ensign_tork", yamlData.ensign_tork);
            CreateProfile("maven_kilroth", yamlData.maven_kilroth);
            CreateProfile("chief_solis", yamlData.chief_solis);
            CreateProfile("tvora", yamlData.tvora);
            CreateProfile("lucky_chen", yamlData.lucky_chen);

            Console.WriteLine($"[CharacterProfileManager] Loaded {profiles.Count} character profiles");
        }

        /// <summary>
        /// Load all portraits from Content Manager
        /// </summary>
        public void LoadPortraits()
        {
            portraitCache.Clear();

            foreach (var profile in profiles.Values)
            {
                if (string.IsNullOrEmpty(profile.PortraitPath))
                    continue;

                try
                {
                    var texture = Globals.screenManager.Content.Load<Texture2D>(profile.PortraitPath);
                    profile.Portrait = texture;
                    portraitCache[profile.PortraitKey] = texture;
                    Console.WriteLine($"[CharacterProfileManager] Loaded portrait: {profile.PortraitKey} from {profile.PortraitPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CharacterProfileManager] Failed to load portrait for {profile.Name}: {ex.Message}");
                }
            }

            Console.WriteLine($"[CharacterProfileManager] Loaded {portraitCache.Count} portraits");
        }

        /// <summary>
        /// Get character profile by ID
        /// </summary>
        public CharacterProfile GetProfile(string id)
        {
            return profiles.ContainsKey(id) ? profiles[id] : null;
        }

        /// <summary>
        /// Get character profile by name
        /// </summary>
        public CharacterProfile GetProfileByName(string name)
        {
            return profiles.Values.FirstOrDefault(p =>
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get all character profiles
        /// </summary>
        public IEnumerable<CharacterProfile> GetAllProfiles()
        {
            return profiles.Values;
        }

        /// <summary>
        /// Get all interrogatable characters
        /// </summary>
        public IEnumerable<CharacterProfile> GetInterrogatableProfiles()
        {
            return profiles.Values.Where(p => p.IsInterrogatable);
        }

        /// <summary>
        /// Get portrait texture by key
        /// </summary>
        public Texture2D GetPortrait(string portraitKey)
        {
            return portraitCache.ContainsKey(portraitKey) ? portraitCache[portraitKey] : null;
        }

        /// <summary>
        /// Get all portrait textures
        /// </summary>
        public Dictionary<string, Texture2D> GetAllPortraits()
        {
            return new Dictionary<string, Texture2D>(portraitCache);
        }

        /// <summary>
        /// Generate a portrait key from a character name
        /// Normalizes names for consistent lookup
        /// </summary>
        private string GeneratePortraitKey(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Unknown";

            // Remove special characters and spaces, keep alphanumeric
            var key = new string(name.Where(c => char.IsLetterOrDigit(c)).ToArray());

            // Special case mappings for existing portrait keys
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Zix", "NPC_Bartender" },
                { "DrHarmonKerrigan", "DrHarmon" },
                { "CommanderSylaraVon", "CommanderSylar" },
                { "DrLyssaThorne", "NPC_DrThorne" },
                { "LieutenantMarcusWebb", "LtWebb" },
                { "EnsignTork", "EnsignTork" },
                { "MavenKilroth", "MavenKilroth" },
                { "ChiefPettyOfficerRainaSolis", "ChiefSolis" },
                { "TVora", "Tehvora" },
                { "LuckyChen", "LuckyChen" }
            };

            return mappings.ContainsKey(key) ? mappings[key] : key;
        }

        /// <summary>
        /// Check if a profile exists
        /// </summary>
        public bool HasProfile(string id)
        {
            return profiles.ContainsKey(id);
        }

        /// <summary>
        /// Get count of loaded profiles
        /// </summary>
        public int ProfileCount => profiles.Count;
    }
}
