using System;
using System.Collections.Generic;
using UnityEngine;

public class CHAIN_SharedData : MonoBehaviour
{
    public static bool DoesFlagExist(string flagName)
    {
        return GetFlags().Contains(flagName);
    }

    private static List<string> GetFlags()
    {
        //look for shareddata.data file in StreamingAssets
        string dataPath = Application.streamingAssetsPath + "/shareddata.data";

        if (!System.IO.File.Exists(dataPath))
        {
            return new List<string>();
        }

        return new List<string>(System.IO.File.ReadAllText(dataPath).Trim().Split('\n'));
    }

    private static void SaveFlags(List<string> flags)
    {
        //look for shareddata.data file in StreamingAssets
        string dataPath = Application.streamingAssetsPath + "/shareddata.data";
        System.IO.File.WriteAllText(dataPath, string.Join("\n", flags));
    }

    public static void CreateFlag(string flagName)
    {
        List<string> flags = GetFlags();

        if (!flags.Contains(flagName))
        {
            flags.Add(flagName);
            SaveFlags(flags);

            Debug.Log("Flag created: " + flagName);
        }
    }

    public static void DeleteFlag(string flagName)
    {
        List<string> flags = GetFlags();

        if (flags.Contains(flagName))
        {
            flags.Remove(flagName);

            Debug.Log("Flag deleted: " + flagName);
        }

        SaveFlags(flags);
    }
}
