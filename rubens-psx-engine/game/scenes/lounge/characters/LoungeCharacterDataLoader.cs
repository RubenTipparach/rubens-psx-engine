using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// Loads character configuration from YAML file
    /// </summary>
    public static class LoungeCharacterDataLoader
    {
        private static LoungeCharactersData cachedData = null;

        /// <summary>
        /// Load character data from YAML file
        /// </summary>
        public static LoungeCharactersData LoadCharacters(string yamlFilePath)
        {
            // Return cached data if already loaded
            if (cachedData != null)
            {
                return cachedData;
            }

            try
            {
                Console.WriteLine($"Loading character data from: {yamlFilePath}");

                // Read YAML file
                string yamlContent = File.ReadAllText(yamlFilePath);

                // Create deserializer with underscore naming convention
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                // Deserialize to data classes
                cachedData = deserializer.Deserialize<LoungeCharactersData>(yamlContent);

                Console.WriteLine($"Successfully loaded character data:");
                Console.WriteLine($"  - Bartender: {cachedData.bartender?.name}");
                Console.WriteLine($"  - Pathologist: {cachedData.pathologist?.name}");
                Console.WriteLine($"  - Total suspects: 8");

                return cachedData;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: Failed to load character data from YAML: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.ForegroundColor = ConsoleColor.Gray;
                throw;
            }
        }

        /// <summary>
        /// Clear cached data (for testing/reloading)
        /// </summary>
        public static void ClearCache()
        {
            cachedData = null;
        }
    }
}
