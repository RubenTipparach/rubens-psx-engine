using Microsoft.Xna.Framework;
using rubens_psx_engine;
using rubens_psx_engine.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace anakinsoft.system
{
    /// <summary>
    /// Position data from YAML
    /// </summary>
    public class PositionData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3 ToVector3(float scale = 1.0f)
        {
            return new Vector3(X * scale, Y * scale, Z * scale);
        }
    }

    /// <summary>
    /// Rotation data from YAML (in degrees)
    /// </summary>
    public class RotationData
    {
        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public float Roll { get; set; }

        public Quaternion ToQuaternion()
        {
            return QuaternionExtensions.CreateFromYawPitchRollDegrees(Yaw, Pitch, Roll);
        }
    }

    /// <summary>
    /// Collider dimensions from YAML
    /// </summary>
    public class ColliderData
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float Depth { get; set; }
    }

    /// <summary>
    /// Dialogue line from YAML
    /// </summary>
    public class DialogueLineData
    {
        public string Speaker { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// Dialogue sequence from YAML
    /// </summary>
    public class DialogueData
    {
        public string SequenceName { get; set; }
        public List<DialogueLineData> Lines { get; set; }
        public string OnComplete { get; set; }

        public DialogueData()
        {
            Lines = new List<DialogueLineData>();
        }
    }

    /// <summary>
    /// Character data from YAML
    /// </summary>
    public class CharacterData
    {
        // Basic info
        public string Name { get; set; }
        public string Pronouns { get; set; }
        public string Species { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }
        public string Role { get; set; }
        public string Personality { get; set; }
        public string Portrait { get; set; }
        public string Model { get; set; }

        // Transform data
        public PositionData Position { get; set; }
        public RotationData Rotation { get; set; }
        public float Scale { get; set; }

        // Camera data
        public PositionData CameraPosition { get; set; }
        public PositionData CameraLookAt { get; set; }

        // Collider data
        public ColliderData Collider { get; set; }

        // Investigation data
        public string PublicStory { get; set; }
        public string Secret { get; set; }
        public bool IsKiller { get; set; }
        public string KillerType { get; set; }
        public bool RedHerring { get; set; }
        public List<string> KeyEvidence { get; set; }

        // Dialogue
        public DialogueData Dialogue { get; set; }

        public CharacterData()
        {
            KeyEvidence = new List<string>();
        }
    }

    /// <summary>
    /// Game settings from YAML
    /// </summary>
    public class GameSettingsData
    {
        public float LevelScale { get; set; }
        public float PositionScale { get; set; }
        public List<string> SpawnSequence { get; set; }

        public GameSettingsData()
        {
            SpawnSequence = new List<string>();
        }
    }

    /// <summary>
    /// Root YAML data structure
    /// </summary>
    public class LoungeCharactersData
    {
        public CharacterData Bartender { get; set; }
        public CharacterData Pathologist { get; set; }
        public CharacterData CommanderVon { get; set; }
        public CharacterData DrThorne { get; set; }
        public CharacterData LtWebb { get; set; }
        public CharacterData EnsignTork { get; set; }
        public CharacterData MavenKilroth { get; set; }
        public CharacterData ChiefSolis { get; set; }
        public CharacterData Tvora { get; set; }
        public CharacterData LuckyChen { get; set; }
        public GameSettingsData GameSettings { get; set; }

        /// <summary>
        /// Get character by ID
        /// </summary>
        public CharacterData GetCharacter(string characterId)
        {
            return characterId.ToLower() switch
            {
                "bartender" => Bartender,
                "pathologist" => Pathologist,
                "commander_von" => CommanderVon,
                "dr_thorne" => DrThorne,
                "lt_webb" => LtWebb,
                "ensign_tork" => EnsignTork,
                "maven_kilroth" => MavenKilroth,
                "chief_solis" => ChiefSolis,
                "tvora" => Tvora,
                "lucky_chen" => LuckyChen,
                _ => null
            };
        }

        /// <summary>
        /// Get all characters
        /// </summary>
        public List<CharacterData> GetAllCharacters()
        {
            var characters = new List<CharacterData>();
            if (Bartender != null) characters.Add(Bartender);
            if (Pathologist != null) characters.Add(Pathologist);
            if (CommanderVon != null) characters.Add(CommanderVon);
            if (DrThorne != null) characters.Add(DrThorne);
            if (LtWebb != null) characters.Add(LtWebb);
            if (EnsignTork != null) characters.Add(EnsignTork);
            if (MavenKilroth != null) characters.Add(MavenKilroth);
            if (ChiefSolis != null) characters.Add(ChiefSolis);
            if (Tvora != null) characters.Add(Tvora);
            if (LuckyChen != null) characters.Add(LuckyChen);
            return characters;
        }
    }

    /// <summary>
    /// Loads character data from YAML file
    /// </summary>
    public static class CharacterDataLoader
    {
        private static LoungeCharactersData cachedData = null;

        /// <summary>
        /// Load character data from YAML file
        /// </summary>
        public static LoungeCharactersData LoadCharacterData(string yamlPath = "Content/Data/Lounge/characters.yml")
        {
            if (cachedData != null)
            {
                Console.WriteLine("CharacterDataLoader: Using cached character data");
                return cachedData;
            }

            try
            {
                Console.WriteLine($"CharacterDataLoader: Loading character data from {yamlPath}");

                // Read the YAML file
                string yamlContent = File.ReadAllText(yamlPath);

                // Create deserializer with underscore naming convention
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                // Deserialize
                cachedData = deserializer.Deserialize<LoungeCharactersData>(yamlContent);

                Console.WriteLine($"CharacterDataLoader: Successfully loaded character data");
                Console.WriteLine($"  - Loaded {cachedData.GetAllCharacters().Count} characters");
                Console.WriteLine($"  - Level Scale: {cachedData.GameSettings?.LevelScale}");
                Console.WriteLine($"  - Position Scale: {cachedData.GameSettings?.PositionScale}");

                return cachedData;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"CharacterDataLoader: Failed to load character data: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.ForegroundColor = ConsoleColor.Gray;
                return null;
            }
        }

        /// <summary>
        /// Create dialogue sequence from character data
        /// </summary>
        public static DialogueSequence CreateDialogueFromData(CharacterData characterData)
        {
            if (characterData?.Dialogue == null)
                return null;

            var sequence = new DialogueSequence(characterData.Dialogue.SequenceName);

            foreach (var line in characterData.Dialogue.Lines)
            {
                sequence.AddLine(line.Speaker, line.Text);
            }

            return sequence;
        }

        /// <summary>
        /// Clear cached data (useful for reloading)
        /// </summary>
        public static void ClearCache()
        {
            cachedData = null;
            Console.WriteLine("CharacterDataLoader: Cache cleared");
        }
    }
}
