using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace rubens_psx_engine.system
{
    /// <summary>
    /// Base class for menu screens that handles mouse state and ensures only one menu is visible
    /// </summary>
    public abstract class MenuScreen : Screen
    {
        private bool previousMouseLockState;

        public MenuScreen()
        {
            // Close any other open menus when this menu opens
            CloseOtherMenus();
            
            // Store previous mouse lock state and unlock mouse for menu interaction
            StoreAndUnlockMouse();
        }

        /// <summary>
        /// Close any other open menus when this menu opens
        /// </summary>
        private void CloseOtherMenus()
        {
            var screens = Globals.screenManager.GetScreens();
            
            for (int i = screens.Count - 1; i >= 0; i--)
            {
                var screen = screens[i];
                if (screen is MenuScreen && screen != this && screen.getState != ScreenState.Deactivated)
                {
                    screen.ExitScreen();
                }
            }
        }

        /// <summary>
        /// Store the previous mouse lock state and unlock mouse for menu interaction
        /// </summary>
        private void StoreAndUnlockMouse()
        {
            var config = rubens_psx_engine.system.config.RenderingConfigManager.Config;
            previousMouseLockState = config.Input.LockMouse;
            
            // Temporarily unlock mouse while in menu
            config.Input.LockMouse = false;
            
            // Immediately update mouse visibility to ensure it takes effect
            Globals.screenManager.IsMouseVisible = true;
        }

        /// <summary>
        /// Restore the previous mouse lock state when exiting menu
        /// </summary>
        private void RestoreMouseState()
        {
            var config = rubens_psx_engine.system.config.RenderingConfigManager.Config;
            config.Input.LockMouse = previousMouseLockState;
            
            // Immediately update mouse visibility based on restored state
            Globals.screenManager.IsMouseVisible = !previousMouseLockState;
        }

        public override void ExitScreen()
        {
            // Restore mouse state before exiting
            RestoreMouseState();
            base.ExitScreen();
        }

        public override void KillScreen()
        {
            // Restore mouse state before killing screen
            RestoreMouseState();
            base.KillScreen();
        }
    }
}