using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;

namespace rubens_psx_engine.system.controllers
{
    /// <summary>
    /// Interface for player controller implementations
    /// </summary>
    public interface IPlayerController
    {
        /// <summary>
        /// Update the controller with input and game time
        /// </summary>
        void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse);
        
        /// <summary>
        /// Update the camera for this controller
        /// </summary>
        void UpdateCamera(Camera camera);
        
        /// <summary>
        /// Get the current position of the player
        /// </summary>
        Vector3 GetPosition();
        
        /// <summary>
        /// Handle mouse lock/unlock
        /// </summary>
        void SetMouseLocked(bool locked);
        
        /// <summary>
        /// Get whether mouse is locked
        /// </summary>
        bool IsMouseLocked();
        
        /// <summary>
        /// Dispose resources
        /// </summary>
        void Dispose();
    }
}