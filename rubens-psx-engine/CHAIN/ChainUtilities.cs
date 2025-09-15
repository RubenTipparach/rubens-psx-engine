using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace rubens_psx_engine.chain
{
    /// <summary>
    /// Simple CHAIN launcher utilities converted from Unity scripts
    /// These are standalone functions that you can use as needed
    /// </summary>
    public static class ChainUtilities
    {
        /// <summary>
        /// Path to the StreamingAssets folder (equivalent to Unity's Application.streamingAssetsPath)
        /// </summary>
        private static string StreamingAssetsPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StreamingAssets");

        #region Scene Selector Functions (from CHAIN_SceneSelector.cs)

        /// <summary>
        /// Simple door mapping class
        /// </summary>
        public class DoorMapping
        {
            public string DoorID { get; set; }
            public Vector3 startlocation{ get; set; }
            public Quaternion startRotation { get; set; }

            public DoorMapping(string doorId, Vector3 startLocaion, Quaternion startRotation)
            {
                DoorID = doorId;
                this.startRotation = startRotation;
                this.startlocation = startLocaion;
            }
        }

        /// <summary>
        /// Read the enter.door file and determine which scene to load
        /// </summary>
        /// <param name="doorMappings">List of door ID to scene name mappings</param>
        /// <param name="defaultScene">Scene to load if no door file exists or no mapping found</param>
        /// <returns>Scene name to load</returns>
        public static DoorMapping GetSceneFromDoorFile(List<DoorMapping> doorMappings, string defaultScene)
        {
            try
            {
                string doorPath = Path.Combine(StreamingAssetsPath, "enter.door");

                if (File.Exists(doorPath))
                {
                    string doorID = File.ReadAllText(doorPath).Trim();
                    Console.WriteLine($"Door ID found: {doorID}");

                    var matchingDoor = doorMappings?.FirstOrDefault(d => d.DoorID == doorID);
                    //if (matchingDoor != null)
                    //{
                    //    Console.WriteLine($"Loading scene: {matchingDoor.SceneName}");
                    //    return matchingDoor.SceneName;
                    //}

                    return matchingDoor;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading door file: {ex.Message}");
            }

            return doorMappings[0];
        }

        /// <summary>
        /// Create an exit.door file and quit the application
        /// </summary>
        /// <param name="doorID">ID of the next game to load</param>
        public static void ExitGame(string doorID)
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(StreamingAssetsPath);

                string doorPath = Path.Combine(StreamingAssetsPath, "exit.door");
                File.WriteAllText(doorPath, doorID);

                Console.WriteLine($"Exit door created for: {doorID}");

                Globals.screenManager.Exit();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating exit door: {ex.Message}");
            }

            // Exit the application
            Environment.Exit(0);
        }

        #endregion

        #region Shared Data Functions (from CHAIN_SharedData.cs)

        /// <summary>
        /// Check if a flag exists in the shared data
        /// </summary>
        /// <param name="flagName">Name of the flag to check</param>
        /// <returns>True if the flag exists</returns>
        public static bool DoesFlagExist(string flagName)
        {
            var flags = GetFlags();
            return flags.Contains(flagName);
        }

        /// <summary>
        /// Create a new flag in the shared data
        /// </summary>
        /// <param name="flagName">Name of the flag to create</param>
        public static void CreateFlag(string flagName)
        {
            var flags = GetFlags();

            if (!flags.Contains(flagName))
            {
                flags.Add(flagName);
                SaveFlags(flags);
                Console.WriteLine($"Flag created: {flagName}");
            }
        }

        /// <summary>
        /// Delete a flag from the shared data
        /// </summary>
        /// <param name="flagName">Name of the flag to delete</param>
        public static void DeleteFlag(string flagName)
        {
            var flags = GetFlags();

            if (flags.Contains(flagName))
            {
                flags.Remove(flagName);
                Console.WriteLine($"Flag deleted: {flagName}");
            }

            SaveFlags(flags);
        }

        /// <summary>
        /// Get all flags from the shared data file
        /// </summary>
        /// <returns>List of flag names</returns>
        private static List<string> GetFlags()
        {
            try
            {
                string dataPath = Path.Combine(StreamingAssetsPath, "shareddata.data");

                if (!File.Exists(dataPath))
                {
                    return new List<string>();
                }

                string content = File.ReadAllText(dataPath).Trim();
                if (string.IsNullOrEmpty(content))
                {
                    return new List<string>();
                }

                return content.Split('\n')
                             .Select(line => line.Trim())
                             .Where(line => !string.IsNullOrEmpty(line))
                             .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading shared data: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Save flags to the shared data file
        /// </summary>
        /// <param name="flags">List of flags to save</param>
        private static void SaveFlags(List<string> flags)
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(StreamingAssetsPath);

                string dataPath = Path.Combine(StreamingAssetsPath, "shareddata.data");
                string content = string.Join("\n", flags);
                File.WriteAllText(dataPath, content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving shared data: {ex.Message}");
            }
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Get all flags (for debugging)
        /// </summary>
        /// <returns>List of all flags</returns>
        public static List<string> GetAllFlags()
        {
            return GetFlags();
        }

        /// <summary>
        /// Clear all flags (for testing)
        /// </summary>
        public static void ClearAllFlags()
        {
            SaveFlags(new List<string>());
            Console.WriteLine("All flags cleared");
        }

        /// <summary>
        /// Create a properly formatted flag name
        /// </summary>
        /// <param name="gameId">Game ID (e.g., "A1", "X1")</param>
        /// <param name="flagName">Flag name</param>
        /// <returns>Formatted flag name: "GAMEID_flagName"</returns>
        public static string FormatFlagName(string gameId, string flagName)
        {
            return $"{gameId}_{flagName}";
        }

        #endregion
    }
}