using anakinsoft.system.cameras;
using anakinsoft.utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.system;
using anakinsoft.system;
using anakinsoft.game.scenes.lounge.characters;
using System;
using System.Collections.Generic;
using System.IO;

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

        // Game progress tracker
        LoungeGameProgress gameProgress;

        // Inventory and dialogue choices
        LoungeInventory inventory;
        DialogueChoiceSystem dialogueChoiceSystem;

        // Transcript review system
        TranscriptReviewUI transcriptReviewUI;

        // Character state machines
        anakinsoft.game.scenes.lounge.characters.LoungeCharactersData charactersData;
        BartenderStateMachine bartenderStateMachine;
        PathologistStateMachine pathologistStateMachine;

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

            // Initialize game progress tracker
            gameProgress = new LoungeGameProgress();

            // Initialize inventory and dialogue choices
            inventory = new LoungeInventory();
            dialogueChoiceSystem = new DialogueChoiceSystem();
            transcriptReviewUI = new TranscriptReviewUI();

            // Load character data from YAML and initialize state machines
            LoadCharacterDataAndStateMachines();

            // Set up dialogue and camera transition event handlers
            SetupDialogueAndInteractionSystems();

            // Set up evidence vial item collection
            SetupEvidenceVial();

            // Set up crime scene file interaction
            SetupCrimeSceneFile();

            // Hide mouse cursor for immersive FPS experience
            Globals.screenManager.IsMouseVisible = false;
        }

        private void LoadCharacterDataAndStateMachines()
        {
            try
            {
                // Load character data from YAML
                string yamlPath = Path.Combine("Content", "Data", "Lounge", "characters.yml");
                charactersData = LoungeCharacterDataLoader.LoadCharacters(yamlPath);

                // Initialize bartender state machine
                if (charactersData.bartender != null)
                {
                    bartenderStateMachine = new BartenderStateMachine(charactersData.bartender);
                    Console.WriteLine($"[TheLoungeScreen] Initialized bartender state machine for {charactersData.bartender.name}");
                }

                // Initialize pathologist state machine
                if (charactersData.pathologist != null)
                {
                    pathologistStateMachine = new PathologistStateMachine(charactersData.pathologist);
                    Console.WriteLine($"[TheLoungeScreen] Initialized pathologist state machine for {charactersData.pathologist.name}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[TheLoungeScreen] ERROR: Failed to load character data: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        private void SetupDialogueAndInteractionSystems()
        {
            var bartender = loungeScene.GetBartender();
            if (bartender != null && bartenderStateMachine != null)
            {
                // Set up bartender dialogue - dialogue will be fetched dynamically from state machine
                bartender.OnDialogueTriggered += OnBartenderDialogueTriggered;

                // Set initial dialogue from state machine so character appears interactable
                var initialDialogue = GetBartenderDialogue();
                if (initialDialogue != null)
                {
                    bartender.SetDialogue(initialDialogue);
                    Console.WriteLine($"[TheLoungeScreen] Set initial bartender dialogue: {initialDialogue.SequenceName}");
                }
                else
                {
                    Console.WriteLine("[TheLoungeScreen] WARNING: Could not get initial bartender dialogue");
                }
            }

            // Pathologist dialogue will be set up after spawning (in SetupPathologistDialogue)
            // because pathologist doesn't exist at initialization time

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

        private void SetupEvidenceVial()
        {
            var vial = loungeScene.GetEvidenceVial();
            if (vial != null)
            {
                vial.OnItemCollected += (item) =>
                {
                    Console.WriteLine($"Collected item: {item.Item.Name}");
                    inventory.PickUpItem(item.Item);
                };
            }
        }

        private void SetupCrimeSceneFile()
        {
            var file = loungeScene.GetCrimeSceneFile();
            if (file != null)
            {
                file.OnFileOpened += (crimeFile) =>
                {
                    Console.WriteLine("Opening crime scene file");
                    transcriptReviewUI.Open(crimeFile);
                };
            }
        }

        private void SetupPathologistDialogue()
        {
            var pathologist = loungeScene.GetPathologist();
            if (pathologist != null && pathologistStateMachine != null)
            {
                Console.WriteLine("Setting up pathologist dialogue");

                // Set up pathologist dialogue - dialogue will be fetched dynamically from state machine
                pathologist.OnDialogueTriggered += OnPathologistDialogueTriggered;

                // Set initial dialogue from state machine so character appears interactable
                var initialDialogue = GetPathologistDialogue();
                if (initialDialogue != null)
                {
                    pathologist.SetDialogue(initialDialogue);
                    Console.WriteLine($"[TheLoungeScreen] Set initial pathologist dialogue: {initialDialogue.SequenceName}");
                }
                else
                {
                    Console.WriteLine("[TheLoungeScreen] WARNING: Could not get initial pathologist dialogue");
                }
            }
            else
            {
                Console.WriteLine("ERROR: Pathologist not found or state machine not initialized");
            }
        }

        private DialogueSequence GetBartenderDialogue()
        {
            if (bartenderStateMachine == null)
            {
                Console.WriteLine("[TheLoungeScreen] ERROR: Bartender state machine not initialized");
                return null;
            }

            // Get current dialogue from state machine
            var yamlDialogue = bartenderStateMachine.GetCurrentDialogue();
            if (yamlDialogue == null)
            {
                Console.WriteLine("[TheLoungeScreen] No dialogue available from bartender state machine");
                return null;
            }

            // Convert YAML dialogue to DialogueSequence
            var sequence = new DialogueSequence(yamlDialogue.sequence_name);
            foreach (var line in yamlDialogue.lines)
            {
                sequence.AddLine(line.speaker, line.text);
            }

            // Wire up completion callback to state machine
            if (!string.IsNullOrEmpty(yamlDialogue.on_complete))
            {
                sequence.OnSequenceComplete = () =>
                {
                    Console.WriteLine($"[TheLoungeScreen] Bartender dialogue '{yamlDialogue.sequence_name}' complete");

                    // Update game progress
                    gameProgress.HasTalkedToBartender = true;
                    gameProgress.HasSeenIntro = true;

                    // Notify state machine
                    bartenderStateMachine.OnDialogueComplete(yamlDialogue.sequence_name);

                    // Handle completion actions
                    if (yamlDialogue.on_complete == "spawn_pathologist")
                    {
                        gameProgress.PathologistSpawned = true;
                        loungeScene.SpawnPathologist();
                        SetupPathologistDialogue();
                    }

                    gameProgress.LogProgress();
                };
            }

            return sequence;
        }

        private DialogueSequence GetPathologistDialogue()
        {
            if (pathologistStateMachine == null)
            {
                Console.WriteLine("[TheLoungeScreen] ERROR: Pathologist state machine not initialized");
                return null;
            }

            // Get current dialogue from state machine
            var yamlDialogue = pathologistStateMachine.GetCurrentDialogue();
            if (yamlDialogue == null)
            {
                Console.WriteLine("[TheLoungeScreen] No dialogue available from pathologist state machine");
                return null;
            }

            // Convert YAML dialogue to DialogueSequence
            var sequence = new DialogueSequence(yamlDialogue.sequence_name);
            foreach (var line in yamlDialogue.lines)
            {
                sequence.AddLine(line.speaker, line.text);
            }

            // Wire up completion callback to state machine
            if (!string.IsNullOrEmpty(yamlDialogue.on_complete))
            {
                sequence.OnSequenceComplete = () =>
                {
                    Console.WriteLine($"[TheLoungeScreen] Pathologist dialogue '{yamlDialogue.sequence_name}' complete");

                    // Notify state machine
                    pathologistStateMachine.OnDialogueComplete(yamlDialogue.sequence_name);
                };
            }

            return sequence;
        }

        /// <summary>
        /// Show dialogue choice to present item or not
        /// </summary>
        private void ShowItemConfirmation()
        {
            var choices = new List<DialogueOption>
            {
                new DialogueOption($"Show {inventory.CurrentItem.Name}", () => PresentEvidenceToPathologist()),
                new DialogueOption("Don't show anything", () => PathologistNoEvidence()),
                new DialogueOption("Say something else", () => PathologistSmallTalk())
            };

            dialogueChoiceSystem.ShowChoices("What do you want to do?", choices);
        }

        /// <summary>
        /// Player presents the vial to pathologist
        /// </summary>
        private void PresentEvidenceToPathologist()
        {
            var sequence = new DialogueSequence("PathologistVialResponse");

            sequence.AddLine("Dr. Harmon Kerrigan", "Perfect! This is exactly the kind of evidence you'll need to gather.");
            sequence.AddLine("Dr. Harmon Kerrigan", "Now, let me brief you on what we found at the crime scene...");

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
                gameProgress.HasTalkedToPathologist = true;
                gameProgress.CanInterrogate = true;
                characterSelectionMenu.Show();
                gameProgress.LogProgress();
            };

            dialogueSystem.StartDialogue(sequence);
        }

        /// <summary>
        /// Player chooses not to show evidence
        /// </summary>
        private void PathologistNoEvidence()
        {
            var sequence = new DialogueSequence("PathologistNoEvidence");

            sequence.AddLine("Dr. Harmon Kerrigan", "Keeping your cards close to your chest? I respect that.");
            sequence.AddLine("Dr. Harmon Kerrigan", "But you'll need to start gathering and presenting evidence if you want to solve this case.");
            sequence.AddLine("Dr. Harmon Kerrigan", "Now, let me tell you what we found...");

            // Continue with same evidence briefing
            AddEvidenceBriefingLines(sequence);

            dialogueSystem.StartDialogue(sequence);
        }

        /// <summary>
        /// Player chooses small talk option
        /// </summary>
        private void PathologistSmallTalk()
        {
            var sequence = new DialogueSequence("PathologistSmallTalk");

            sequence.AddLine("Dr. Harmon Kerrigan", "Detective, we don't have time for pleasantries.");
            sequence.AddLine("Dr. Harmon Kerrigan", "The Telirians will be here in hours, and we need answers.");
            sequence.AddLine("Dr. Harmon Kerrigan", "Let me brief you on what we know...");

            // Continue with same evidence briefing
            AddEvidenceBriefingLines(sequence);

            dialogueSystem.StartDialogue(sequence);
        }

        /// <summary>
        /// Add all the evidence briefing lines (reusable across branches)
        /// </summary>
        private void AddEvidenceBriefingLines(DialogueSequence sequence)
        {
            // Opening statement
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

            // Conclusion
            sequence.AddLine("Dr. Harmon Kerrigan", "Look for someone with medical training, access to breturium, diplomatic codes, and knowledge of Telirian customs.");
            sequence.AddLine("Dr. Harmon Kerrigan", "That's all I have for now, Detective. The rest is up to you. Good luck.");

            // Mark progression
            sequence.OnSequenceComplete = () =>
            {
                Console.WriteLine("Pathologist dialogue complete - showing character selection");
                gameProgress.HasTalkedToPathologist = true;
                gameProgress.CanInterrogate = true;
                characterSelectionMenu.Show();
                gameProgress.LogProgress();
            };
        }

        private void OnBartenderDialogueTriggered(DialogueSequence sequence)
        {
            Console.WriteLine($"Bartender dialogue triggered");

            // Get current dialogue from state machine
            var currentDialogue = GetBartenderDialogue();
            if (currentDialogue == null)
            {
                Console.WriteLine("[TheLoungeScreen] No dialogue available from bartender state machine");
                return;
            }

            Console.WriteLine($"[TheLoungeScreen] Retrieved dialogue: {currentDialogue.SequenceName} from state: {bartenderStateMachine.CurrentState}");

            // Transition camera to bartender
            var bartender = loungeScene.GetBartender();
            if (bartender != null)
            {
                // Update the character's dialogue to the current state machine dialogue
                bartender.SetDialogue(currentDialogue);

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
            Console.WriteLine($"Pathologist dialogue triggered");

            // Get current dialogue from state machine
            var currentDialogue = GetPathologistDialogue();
            if (currentDialogue == null)
            {
                Console.WriteLine("[TheLoungeScreen] No dialogue available from pathologist state machine");
                return;
            }

            Console.WriteLine($"[TheLoungeScreen] Retrieved dialogue: {currentDialogue.SequenceName} from state: {pathologistStateMachine.CurrentState}");

            // Transition camera to pathologist
            var pathologist = loungeScene.GetPathologist();
            if (pathologist != null)
            {
                // Update the character's dialogue to the current state machine dialogue
                pathologist.SetDialogue(currentDialogue);

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
                dialogueSystem.StartDialogue(pathologist.DialogueSequence);
            }
        }

        public override void Update(GameTime gameTime)
        {
            // Update transcript review UI first (highest priority when open)
            if (transcriptReviewUI.IsActive)
            {
                transcriptReviewUI.Update(gameTime);
                return; // Don't update other systems while reviewing transcripts
            }

            // Update character selection menu (has highest priority after transcript UI)
            if (characterSelectionMenu.IsActive)
            {
                characterSelectionMenu.Update(gameTime);
                return; // Don't update other systems while menu is active
            }

            // Update dialogue choice system (higher priority than regular dialogue)
            if (dialogueChoiceSystem.IsActive)
            {
                dialogueChoiceSystem.Update(gameTime);
                return; // Don't update other systems while making a choice
            }

            // Update dialogue system
            if (dialogueSystem.IsActive)
            {
                dialogueSystem.Update(gameTime);
            }

            // Update scene with camera for character movement (pass dialogue active state to disable interactions)
            loungeScene.UpdateWithCamera(gameTime, fpsCamera, dialogueSystem.IsActive || dialogueChoiceSystem.IsActive || transcriptReviewUI.IsActive);

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
            loungeScene.DrawUI(gameTime, fpsCamera, spriteBatch, dialogueSystem.IsActive || dialogueChoiceSystem.IsActive);

            // Draw dialogue UI on top
            if (dialogueSystem.IsActive && font != null)
            {
                dialogueSystem.Draw(spriteBatch, font);
            }

            // Draw dialogue choice UI
            if (dialogueChoiceSystem.IsActive && font != null)
            {
                dialogueChoiceSystem.Draw(spriteBatch, font);
            }

            // Draw inventory at bottom of screen
            if (inventory.HasItem && font != null && !transcriptReviewUI.IsActive)
            {
                var viewport = Globals.screenManager.GraphicsDevice.Viewport;
                string inventoryText = inventory.GetDisplayText();
                Vector2 textSize = font.MeasureString(inventoryText);
                Vector2 position = new Vector2(
                    (viewport.Width - textSize.X) / 2,
                    viewport.Height - textSize.Y - 20
                );

                // Draw shadow
                spriteBatch.DrawString(font, inventoryText, position + new Vector2(2, 2), Color.Black);
                // Draw text
                spriteBatch.DrawString(font, inventoryText, position, Color.Yellow);
            }

            // Draw character selection menu
            if (characterSelectionMenu.IsActive && font != null)
            {
                var portraits = loungeScene.GetCharacterPortraits();
                characterSelectionMenu.Draw(spriteBatch, font, portraits);
            }

            // Draw transcript review UI on top of everything
            if (transcriptReviewUI.IsActive && font != null)
            {
                transcriptReviewUI.Draw(spriteBatch, font);
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
