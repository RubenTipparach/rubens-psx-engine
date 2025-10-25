using anakinsoft.system.cameras;
using anakinsoft.utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.system;
using anakinsoft.system;
using System;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Screen for The Lounge scene with FPS camera
    /// </summary>
    public class TheLoungeScreen : PhysicsScreen
    {
        FPSCamera fpsCamera;
        public Camera GetCamera { get { return fpsCamera; } }

        TheLoungeScene loungeScene;

        // Camera offset configuration
        public Vector3 CameraOffset = new Vector3(0, 16.0f, 0); // Y offset to mount camera above character center
        public Vector3 CameraLookOffset = new Vector3(0, -3, 0); // Additional offset for look direction

        // Dialogue and camera transition systems
        DialogueSystem dialogueSystem;
        CameraTransitionSystem cameraTransitionSystem;
        bool hasPlayedIntro = false;

        public TheLoungeScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;
            fpsCamera = new FPSCamera(gd, new Vector3(0, 10, 0));

            loungeScene = new TheLoungeScene();
            SetScene(loungeScene); // Register scene with physics screen for automatic disposal

            // Create camera and set its rotation from the character's initial rotation
            fpsCamera.SetRotation(loungeScene.GetCharacterInitialRotation());

            // Initialize dialogue system
            dialogueSystem = new DialogueSystem();
            cameraTransitionSystem = new CameraTransitionSystem(fpsCamera);

            // Set up dialogue and camera transition event handlers
            SetupDialogueAndInteractionSystems();

            // Hide mouse cursor for immersive FPS experience
            Globals.screenManager.IsMouseVisible = false;
        }

        private void SetupDialogueAndInteractionSystems()
        {
            var bartender = loungeScene.GetBartender();
            if (bartender != null)
            {
                // Set up bartender dialogue
                bartender.OnDialogueTriggered += OnBartenderDialogueTriggered;

                // Create intro dialogue sequence
                var introSequence = CreateIntroDialogue();
                bartender.SetDialogue(introSequence);
            }

            // Set up camera transition events
            cameraTransitionSystem.OnTransitionToInteractionComplete += () =>
            {
                Console.WriteLine("Camera reached interaction position, ready for dialogue");
            };

            cameraTransitionSystem.OnTransitionToPlayerComplete += () =>
            {
                Console.WriteLine("Camera returned to player control");
            };

            // Set up dialogue events
            dialogueSystem.OnDialogueStart += () =>
            {
                Console.WriteLine("Dialogue started");
            };

            dialogueSystem.OnDialogueEnd += () =>
            {
                Console.WriteLine("Dialogue ended, returning camera to player");
                cameraTransitionSystem.TransitionBackToPlayer(1.0f);
            };
        }

        private DialogueSequence CreateIntroDialogue()
        {
            var sequence = new DialogueSequence("BartenderIntro");

            sequence.AddLine("Bartender", "Welcome to the Lounge. You are a detective on board the UEFS Marron.");
            sequence.AddLine("Bartender", "The Telirian ambassador is dead. This is no accident.");
            sequence.AddLine("Bartender", "You need to question the suspects and determine motive, means, and opportunity.");
            sequence.AddLine("Bartender", "Determine who is guilty before the Telirians arrive. Failure to do so will mean all out war.");
            sequence.AddLine("Bartender", "The pathologist is waiting for you in the medical bay. They have preliminary findings on the body.");
            sequence.AddLine("Bartender", "Time is running out, detective. Good luck.");

            return sequence;
        }

        private void OnBartenderDialogueTriggered(DialogueSequence sequence)
        {
            Console.WriteLine($"Bartender dialogue triggered: {sequence.SequenceName}");

            // Transition camera to bartender
            var bartender = loungeScene.GetBartender();
            if (bartender != null)
            {
                cameraTransitionSystem.TransitionToInteraction(
                    bartender.CameraInteractionPosition,
                    bartender.CameraInteractionLookAt,
                    1.0f);

                // Start dialogue when transition is complete
                cameraTransitionSystem.OnTransitionToInteractionComplete += StartDialogueAfterTransition;
            }
        }

        private void StartDialogueAfterTransition()
        {
            // Unsubscribe from this event to avoid multiple triggers
            cameraTransitionSystem.OnTransitionToInteractionComplete -= StartDialogueAfterTransition;

            var bartender = loungeScene.GetBartender();
            if (bartender?.DialogueSequence != null)
            {
                dialogueSystem.StartDialogue(bartender.DialogueSequence);
            }
        }

        public override void Update(GameTime gameTime)
        {
            // Update scene with camera for character movement
            loungeScene.UpdateWithCamera(gameTime, fpsCamera);

            // Update dialogue system
            if (dialogueSystem.IsActive)
            {
                dialogueSystem.Update(gameTime);
            }

            // Update camera transition system
            cameraTransitionSystem.Update(gameTime);

            // Mount FPS camera to character controller (only when not showing intro and not in dialogue/interaction mode)
            if (!loungeScene.IsShowingIntroText() && !cameraTransitionSystem.IsInInteractionMode && !cameraTransitionSystem.IsTransitioning)
            {
                UpdateCameraMountedToCharacter();
            }

            base.Update(gameTime);
        }

        private void UpdateCameraMountedToCharacter()
        {
            var character = loungeScene.GetCharacter();
            if (character.HasValue)
            {
                // Get character position and orientation
                var characterPos = character.Value.Body.Pose.Position.ToVector3();
                var characterOrientation = character.Value.Body.Pose.Orientation.ToQuaternion();

                // Apply camera offset relative to character center
                var offsetInWorldSpace = Vector3.Transform(CameraOffset, Matrix.CreateFromQuaternion(characterOrientation));

                // Set camera position to character center + offset
                fpsCamera.Position = characterPos + offsetInWorldSpace;

                // Optional: Add additional look offset for targeting
                var lookOffsetInWorldSpace = Vector3.Transform(CameraLookOffset, Matrix.CreateFromQuaternion(characterOrientation));
                // Note: FPS camera handles its own look direction via mouse input
            }
        }

        public override void UpdateInput(GameTime gameTime)
        {
            if (!Globals.screenManager.IsActive)
                return;

            // Only update FPS camera when not showing intro, not in dialogue, and not transitioning
            if (!loungeScene.IsShowingIntroText() && !dialogueSystem.IsActive &&
                !cameraTransitionSystem.IsInInteractionMode && !cameraTransitionSystem.IsTransitioning)
            {
                fpsCamera.Update(gameTime);
            }

            if (InputManager.GetKeyboardClick(Keys.Escape))
            {
                // If in dialogue, close dialogue instead of opening pause menu
                if (dialogueSystem.IsActive)
                {
                    dialogueSystem.StopDialogue();
                }
                else
                {
                    Globals.screenManager.AddScreen(new PauseMenu());
                }
            }

            // Add F1 key to switch to scene selection (only if enabled in config)
            if (InputManager.GetKeyboardClick(Keys.F1) && SceneManager.IsSceneMenuEnabled())
            {
                Globals.screenManager.AddScreen(new SceneSelectionMenu());
            }

            // Handle L key to toggle bounding box visualization
            if (InputManager.GetKeyboardClick(Keys.L))
            {
                if (loungeScene.BoundingBoxRenderer != null)
                {
                    System.Console.WriteLine("TheLoungeScreen: L key pressed - toggling bounding boxes");
                    loungeScene.BoundingBoxRenderer.ToggleBoundingBoxes();

                    if (loungeScene.Physics?.Simulation?.BroadPhase != null)
                    {
                        var activeCount = loungeScene.Physics.Simulation.BroadPhase.ActiveTree.LeafCount;
                        var staticCount = loungeScene.Physics.Simulation.BroadPhase.StaticTree.LeafCount;
                        System.Console.WriteLine($"TheLoungeScreen: Physics bodies - Active: {activeCount}, Static: {staticCount}");
                    }
                }
                else
                {
                    System.Console.WriteLine("TheLoungeScreen: No BoundingBoxRenderer available");
                }
            }
        }

        public override void Draw2D(GameTime gameTime)
        {
            // Draw UI
            var spriteBatch = Globals.screenManager.getSpriteBatch;
            loungeScene.DrawUI(gameTime, fpsCamera, spriteBatch);

            // Draw dialogue UI on top
            if (dialogueSystem.IsActive)
            {
                var font = Globals.fontNTR;
                if (font != null)
                {
                    dialogueSystem.Draw(spriteBatch, font);
                }
            }
        }

        public override void Draw3D(GameTime gameTime)
        {
            // Draw the lounge scene
            loungeScene.Draw(gameTime, fpsCamera);
        }

        public override Color? GetBackgroundColor()
        {
            // Return the lounge scene's background color
            return loungeScene.BackgroundColor;
        }

        public override void ExitScreen()
        {
            // PhysicsScreen base class will automatically dispose physics resources
            base.ExitScreen();
        }

        public override void KillScreen()
        {
            // PhysicsScreen base class will automatically dispose physics resources
            base.KillScreen();
        }
    }
}
