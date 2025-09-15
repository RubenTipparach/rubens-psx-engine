using System;
using System.Linq;
using UnityEngine;

public class CHAIN_SceneSelector : MonoBehaviour
{
    // HOW TO USE:
    // make sure you run this script on an empty scene (or use 'sceneselector' scene in the package)
    // and make sure that this is the first scene in your build settings
    // it will read the enter.door file in the StreamingAssets folder
    // it will then set the variable for the SpawnPoints, and you can direct each entry point to a scene
    // if your game runs in one scene, you can leave _doorScenes empty and just use the _defaultScene
    // also, default scene is the scene that will be loaded if there is no door file

    [Serializable]
    private class ChainDoor
    {
        public string doorID; //set to the ID from the map of the game the player enters your game from, eg: 'START' for hub game, or 'A1', 'F5', etc
        public string sceneName; //which scene will be loaded if game is loaded from this door
    }

    [SerializeField] private ChainDoor[] _doorScenes;
    [SerializeField, Tooltip("Will go to this scene if there is no enter door file in directory.")] private string _defaultScene;
    
    public static string DoorID = "";

    private void Start()
    {
        //look for enter.door file in StreamingAssets
        string doorPath = Application.streamingAssetsPath + "/enter.door";

        if (System.IO.File.Exists(doorPath))
        {
            string doorID = System.IO.File.ReadAllText(doorPath).Trim();
            
            DoorID = doorID;

            Debug.Log("DoorID: " + doorID);

            var matchingDoor = _doorScenes.FirstOrDefault(d => d.doorID == doorID);
            if (matchingDoor != null)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(matchingDoor.sceneName);
                return;
            }
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(_defaultScene);
    }

    public static void ExitGame(string doorID)
    { 
        //create a exit.door file in StreamingAssets
        string doorPath = Application.streamingAssetsPath + "/exit.door";
        System.IO.File.WriteAllText(doorPath, doorID);

        //exit the game
        Application.Quit();
    }
}
