using rubens_psx_engine.entities;
using System;

namespace rubens_psx_engine.system
{
    /// <summary>
    /// Base class for screens that contain physics-enabled scenes.
    /// Automatically handles proper disposal of physics resources to prevent memory leaks.
    /// </summary>
    public abstract class PhysicsScreen : Screen
    {
        /// <summary>
        /// The physics-enabled scene managed by this screen
        /// </summary>
        protected Scene scene;

        /// <summary>
        /// Gets the managed scene
        /// </summary>
        public Scene Scene => scene;

        /// <summary>
        /// Sets the scene to be managed by this physics screen.
        /// The previous scene will be disposed if it exists.
        /// </summary>
        /// <param name="newScene">The new scene to manage</param>
        protected void SetScene(Scene newScene)
        {
            // Dispose the previous scene if it exists
            scene?.Dispose();
            scene = newScene;
        }

        public override void ExitScreen()
        {
            // Dispose physics resources before exiting
            DisposePhysicsResources();
            base.ExitScreen();
        }

        public override void KillScreen()
        {
            // Dispose physics resources before killing
            DisposePhysicsResources();
            base.KillScreen();
        }

        /// <summary>
        /// Disposes physics resources associated with this screen.
        /// Called automatically when the screen exits or is killed.
        /// </summary>
        protected virtual void DisposePhysicsResources()
        {
            scene?.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Ensure physics resources are cleaned up during disposal
                DisposePhysicsResources();
            }
            base.Dispose(disposing);
        }
    }
}