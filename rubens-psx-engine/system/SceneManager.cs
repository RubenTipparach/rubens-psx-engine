using anakinsoft.game.scenes;
using rubens_psx_engine.system.config;
using rubens_psx_engine.game.scenes;

namespace rubens_psx_engine.system
{
    /// <summary>
    /// Manages scene creation and loading based on configuration
    /// </summary>
    public static class SceneManager
    {
        /// <summary>
        /// Create a scene screen based on scene type string
        /// </summary>
        /// <param name="sceneType">Type of scene to create</param>
        /// <returns>Screen instance for the scene</returns>
        public static Screen CreateScene(string sceneType)
        {
            return sceneType switch
            {
                "thirdPersonSandbox" => new ThirdPersonSandboxScreen(),
                "fpsSandbox" => new FPSSandboxScreen(),
                "basic" => new GraphicsTestSceneScreen(),
                "cameraTest" => new CameraTestScene(),
                "thirdPersonHallway" => new ThirdPersonHallwayScene(),
                "graphicsTest" => new GraphicsTestSceneScreen(),
                "corridor" => new CorridorScreen(),
                "wireframeTest" => new WireframeCubeTestScreen(),
                "staticMeshDemo" => new StaticMeshDemoScreen(),
                "interactiveTest" => new InteractiveTestSceneScreen(),
                "bepuInteraction" => new BepuInteractionScreen(),
                _ => new ThirdPersonSandboxScreen() // Default fallback
            };
        }

        /// <summary>
        /// Load the default scene from configuration
        /// </summary>
        /// <returns>Screen instance for the default scene</returns>
        public static Screen LoadDefaultScene()
        {
            var config = RenderingConfigManager.Config.Scene;
            return CreateScene(config.DefaultScene);
        }

        /// <summary>
        /// Load either the scene selection menu or default scene based on config
        /// </summary>
        /// <returns>Screen instance to load on startup</returns>
        public static Screen LoadStartupScene()
        {
            var config = RenderingConfigManager.Config.Scene;

            // Check for direct level loading
            //if (config.DirectLevel != "default")
            //{
            //    return CreateScene(config.DirectLevel);
            //}

            if (config.ShowSceneMenu)
            {
                return new SceneSelectionMenu();
            }
            else
            {
                return LoadDefaultScene();
            }
        }

        /// <summary>
        /// Get all available scene types
        /// </summary>
        /// <returns>Array of scene type strings</returns>
        public static string[] GetAvailableScenes()
        {
            return new string[]
            {
                "thirdPersonSandbox",
                "fpsSandbox",
                "basic",
                "cameraTest",
                "thirdPersonHallway",
                "graphicsTest",
                "corridor",
                "wireframeTest",
                "staticMeshDemo",
                "interactiveTest",
                "bepuInteraction"
            };
        }

        /// <summary>
        /// Get display name for a scene type
        /// </summary>
        /// <param name="sceneType">Scene type string</param>
        /// <returns>Human-readable scene name</returns>
        public static string GetSceneDisplayName(string sceneType)
        {
            return sceneType switch
            {
                "thirdPersonSandbox" => "Third Person Sandbox",
                "fpsSandbox" => "FPS Sandbox",
                "basic" => "Graphics Test Scene",
                "cameraTest" => "Camera Test Scene",
                "thirdPersonHallway" => "Third Person Hallway",
                "graphicsTest" => "Graphics Test Scene",
                "corridor" => "Corridor Multi-Material Scene",
                "wireframeTest" => "Wireframe Cube Test",
                "staticMeshDemo" => "Static Mesh Demo",
                "interactiveTest" => "Interactive Test Scene",
                "bepuInteraction" => "BEPU Physics Interaction",
                _ => "Unknown Scene"
            };
        }

        /// <summary>
        /// Check if scene menu access is enabled in configuration
        /// </summary>
        /// <returns>True if scene menu can be accessed, false if disabled</returns>
        public static bool IsSceneMenuEnabled()
        {
            return RenderingConfigManager.Config.Scene.ShowSceneMenu;
        }
    }
}