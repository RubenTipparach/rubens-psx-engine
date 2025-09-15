these tools for unity will make your game work with the chain launcher. when your game is being entered through a door from another game. refer to the map (https://chain3map.netlify.app/) to check the IDs for each game. the launcher will put a text file in your StreamingAssets folder called "enter.door", the contents of which are the ID of the game that led to your game.

for example: if the player was playing the game with the ID 'A1', the launcher will make a "enter.door" file in your StreamingAssets folder that reads 'A1'.

setup:
0. make a folder in your Assets Folder called StreamingAssets (exact name).

1. make the sceneselector scene the first scene in your, in the SCENESELECTOR object, you can specify which scene to load for each game / door that the player enters your game from. simply put the ID with the scene name, and the scene selector will do the rest. if your game is all in one scene (or you want to test without a .door file) put the name of the scene in "default scene".

2. in the scene where your actual game is, put a SPAWNPOINT object where the player should spawn and then attach a script where you call your logic for setting the player position and rotation and call it with the OnSpawn unity event.

-- if a scene only has one entrance point, you dont have to use a spawnpoint.

-- if your game is one that doesnt have any exits you can omit using any of these tools entirely as the launcher will handle everything for you.

3. finally, when you exit your game through a door, call the static ExitGame() function in the CHAIN_SceneSelector. and pass the ID of the next game. the script will make an exit.door file for you.

4. when building your game, make sure the StreamingAssets folder (even if its empty) exists in your build. for unity builds this will be in yourgamename_Data/StreamingAssets. unity has the tendency to not make the folder if it's empty, so double check! in the godot version of this plugin, make a StreamingAssets folder in the root directory.

----- (new) shared data -----
in this updated version of the plugin, i've added functionality to share data between games. we will work with a simple flag system. the launcher will put a "shareddata.data" file in your games StreamingAssets folder. you can modify it with this plugin and the launcher will then save your changes for you. 

a flag is just a simple string, and you can check in your game if a flag exists, make a new flag or delete an existing flag. you can name it anything, but for clarity use this format: "GAMEID_flagName"

--> example: say you are making game X1 and you have a mysterious button in your game. when you press the mysterious button you create the flag "X1_mysteriousButtonPressed". another developer checks for this flag in their game and makes a mysterious hatch open that leads to a secret area. they also have a button next to the hatch that closes it again, so they delete the flag when you press it.

usage:
from any point in your game you can call the following functions

-- CHAIN_SharedData.CreateFlag(string flagName)
will add a flag to the shared data

-- CHAIN_SharedData.DeleteFlag(string flagName)
will remove a flag from the shared data

-- CHAIN_SharedData.DoesFlagExist(string flagName)
will return true if the flag exists

make sure to add the flags to this shared sheet with a short description of what it's tied to, and also write down if you manipulate any existing flags.
(link: https://docs.google.com/spreadsheets/d/1dpW24T5lsOGn2VfevVdI4zt9Hn87leZOaT6lNfFUgpA/edit?usp=sharing)