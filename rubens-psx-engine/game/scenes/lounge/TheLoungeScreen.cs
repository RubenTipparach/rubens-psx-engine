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

        // Character selection menu
        CharacterSelectionMenu characterSelectionMenu;

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

            // Initialize character selection menu
            characterSelectionMenu = new CharacterSelectionMenu();

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

            var pathologist = loungeScene.GetPathologist();
            if (pathologist != null)
            {
                // Set up pathologist dialogue
                pathologist.OnDialogueTriggered += OnPathologistDialogueTriggered;

                // Create pathologist evidence dialogue
                var pathologistSequence = CreatePathologistDialogue();
                pathologist.SetDialogue(pathologistSequence);
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
                loungeScene.ClearActiveDialogueCharacter();
                cameraTransitionSystem.TransitionBackToPlayer(1.0f);
            };

            // Set up character selection menu events
            characterSelectionMenu.OnCharacterSelected += (character) =>
            {
                Console.WriteLine($"Character selected for interrogation: {character.Name}");
                // TODO: Start interrogation dialogue for selected character
            };

            characterSelectionMenu.OnMenuClosed += () =>
            {
                Console.WriteLine("Character selection menu closed");
            };
        }

        private DialogueSequence CreateIntroDialogue()
        {
            var sequence = new DialogueSequence("BartenderIntro");

            sequence.AddLine("Bartender", "Welcome to the Lounge. You are a detective on board the UEFS Marron.");
            sequence.AddLine("Bartender", "The Telirian ambassador is dead. This is no accident.");
            sequence.AddLine("Bartender", "You need to question the suspects and determine motive, means, and opportunity.");
            sequence.AddLine("Bartender", "Determine who is guilty before the Telirians arrive. Failure to do so will mean all out war.");
            sequence.AddLine("Bartender", "I've called the pathologist. They should be arriving at the table shortly with preliminary findings.");
            sequence.AddLine("Bartender", "Time is running out, detective. Good luck.");

            // Spawn pathologist after bartender dialogue completes
            sequence.OnSequenceComplete = () =>
            {
                Console.WriteLine("Bartender dialogue complete - spawning pathologist");
                loungeScene.SpawnPathologist();
            };

            return sequence;
        }

        private DialogueSequence CreatePathologistDialogue()
        {
            var sequence = new DialogueSequence("PathologistEvidence");

            // Opening statement (from evidence.md - Dr. Harmon Kerrigan sample dialogue)
            sequence.AddLine("Dr. Harmon Kerrigan", "Breturium shards? In an injection? That's not murder, Detective, that's a statement.");
            sequence.AddLine("Dr. Harmon Kerrigan", "Whoever did this wanted it personal, painful, and invisible to scanners.");
            sequence.AddLine("Dr. Harmon Kerrigan", "They also had to know Telirian physiology. Breturium reacts with their copper-based blood in a way that... let's say 'cascading organ failure' doesn't quite cover it.");

            // Time of death
            sequence.AddLine("Dr. Harmon Kerrigan", "Time of death: approximately 0300 hours. He was found collapsed near his bed, diplomatic robes disheveled.");

            // Injection details
            sequence.AddLine("Dr. Harmon Kerrigan", "The injection site shows faint scorch marks on his neck. Right-handed attacker, close personal range. This required medical knowledge.");

            // Sedative findings
            sequence.AddLine("Dr. Harmon Kerrigan", "Trace amounts of sedative in his system. Not lethal, but enough to make him drowsy. Someone prepared him for the kill.");

            // The crime scene
            sequence.AddLine("Dr. Harmon Kerrigan", "His personal PADD shows a meeting at 2100 hours with someone marked as 'T.B.' in his calendar.");
            sequence.AddLine("Dr. Harmon Kerrigan", "There was a half-empty glass of Telirian ceremonial wine on the nightstand. The wine contained the sedative.");

            // Security findings
            sequence.AddLine("Dr. Harmon Kerrigan", "His diplomatic lockbox was open. It requires both biometric scan and a 6-digit code. Someone with intimate knowledge opened it.");
            sequence.AddLine("Dr. Harmon Kerrigan", "Door logs show 4 different access codes used that night: his own, one override code, and two diplomatic access codes.");

            // Conclusion and what to do next
            sequence.AddLine("Dr. Harmon Kerrigan", "Look for someone with medical training, access to breturium, diplomatic codes, and knowledge of Telirian customs.");
            sequence.AddLine("Dr. Harmon Kerrigan", "That's all I have for now, Detective. The rest is up to you. Good luck.");

            // Set up callback for when dialogue ends - show character selection menu
            sequence.OnSequenceComplete = () =>
            {
                Console.WriteLine("Pathologist dialogue complete - showing character selection");
                characterSelectionMenu.Show();
            };

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
                cameraTransitionSystem.OnTransitionToInteractionComplete += StartBartenderDialogueAfterTransition;
            }
        }

        private void OnPathologistDialogueTriggered(DialogueSequence sequence)
        {
            Console.WriteLine($"Pathologist dialogue triggered: {sequence.SequenceName}");

            // Transition camera to pathologist
            var pathologist = loungeScene.GetPathologist();
            if (pathologist != null)
            {
                cameraTransitionSystem.TransitionToInteraction(
                    pathologist.CameraInteractionPosition,
                    pathologist.CameraInteractionLookAt,
                    1.0f);

                // Start dialogue when transition is complete
                cameraTransitionSystem.OnTransitionToInteractionComplete += StartPathologistDialogueAfterTransition;
            }
        }

        private void StartBartenderDialogueAfterTransition()
        {
            // Unsubscribe from this event to avoid multiple triggers
            cameraTransitionSystem.OnTransitionToInteractionComplete -= StartBartenderDialogueAfterTransition;

            var bartender = loungeScene.GetBartender();
            if (bartender?.DialogueSequence != null)
            {
                loungeScene.SetActiveDialogueCharacter("NPC_Bartender");
                dialogueSystem.StartDialogue(bartender.DialogueSequence);
            }
        }

        private void StartPathologistDialogueAfterTransition()
        {
            // Unsubscribe from this event to avoid multiple triggers
            cameraTransitionSystem.OnTransitionToInteractionComplete -= StartPathologistDialogueAfterTransition;

            var pathologist = loungeScene.GetPathologist();
            if (pathologist?.DialogueSequence != null)
            {
                loungeScene.SetActiveDialogueCharacter("DrHarmon");
                dialogueSystem.StartDialogue(pathologist.DialogueSequence);
            }
        }

        public override void Update(GameTime gameTime)
        {
            // Update character selection menu first (has highest priority)
            if (characterSelectionMenu.IsActive)
            {
                characterSelectionMenu.Update(gameTime);
                return; // Don't update other systems while menu is active
            }

            // Update dialogue system
            if (dialogueSystem.IsActive)
            {
                dialogueSystem.Update(gameTime);
            }

            // Update scene with camera for character movement (pass dialogue active state to disable interactions)
            loungeScene.UpdateWithCamera(gameTime, fpsCamera, dialogueSystem.IsActive);

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
            var spriteBatch = Globals.screenManager.getSpriteBatch;
            var font = Globals.fontNTR;

            // Draw UI (pass dialogue active state to hide interaction prompts during dialogue)
            loungeScene.DrawUI(gameTime, fpsCamera, spriteBatch, dialogueSystem.IsActive);

            // Draw dialogue UI on top
            if (dialogueSystem.IsActive && font != null)
            {
                dialogueSystem.Draw(spriteBatch, font);
            }

            // Draw character selection menu on top of everything
            if (characterSelectionMenu.IsActive && font != null)
            {
                var portraits = loungeScene.GetCharacterPortraits();
                characterSelectionMenu.Draw(spriteBatch, font, portraits);
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
