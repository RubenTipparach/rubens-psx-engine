using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;

namespace rubens_psx_engine
{
    public static class Globals
    {
        public static string WINDOWNAME = "Rubens PSX Engine"; //Title that appears in window.
        public static string SETTINGSFOLDERNAME = "rubens_psx_engine"; //Folder that settings are saved in.

        public static Color COLOR_BACKGROUND = new Color(100, 149, 237);
        public static Vector3 CAMERAPOS = new Vector3(0, 0, 200);

        public static ScreenManager screenManager;

        public static string baseFolder = "Content/Assets"; //Tells FNA ContentManager what folder to load content from. Defaults to "base". Use this to load player-made mods.

        
        public static Texture2D white;
        public static Texture2D orange;


        public static SpriteFont fontNTR;

        public static Random random;
        
        public static Vector3 backgroundcolor;

        public static void Initialize(ContentManager Content)
        {
            random = new Random();

            fontNTR = Content.Load<SpriteFont>("fonts\\Arial");
            fontNTR.LineSpacing = 28;            

            orange = Content.Load<Texture2D>("textures\\orange");
        }
    }


}