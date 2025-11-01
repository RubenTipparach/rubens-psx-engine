using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace anakinsoft.game.scenes.lounge.evidence
{
    /// <summary>
    /// Loads evidence configuration from YAML file
    /// </summary>
    public static class EvidenceDataLoader
    {
        private static EvidenceData cachedData;

        /// <summary>
        /// Load evidence from YAML file
        /// </summary>
        public static EvidenceData LoadEvidence(string yamlPath = "Content/Data/Lounge/evidence.yml")
        {
            if (cachedData != null)
            {
                Console.WriteLine("[EvidenceDataLoader] Using cached evidence data");
                return cachedData;
            }

            try
            {
                Console.WriteLine($"[EvidenceDataLoader] Loading evidence data from {yamlPath}");

                // Read the YAML file
                string yamlContent = File.ReadAllText(yamlPath);

                // Create deserializer with underscore naming convention
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                // Deserialize
                cachedData = deserializer.Deserialize<EvidenceData>(yamlContent);

                Console.WriteLine($"[EvidenceDataLoader] Successfully loaded {cachedData.evidence.Count} evidence items");

                return cachedData;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[EvidenceDataLoader] Failed to load evidence data: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.ForegroundColor = ConsoleColor.Gray;
                return null;
            }
        }

        /// <summary>
        /// Clear cached data (useful for hot-reloading)
        /// </summary>
        public static void ClearCache()
        {
            cachedData = null;
            Console.WriteLine("[EvidenceDataLoader] Cache cleared");
        }
    }
}
