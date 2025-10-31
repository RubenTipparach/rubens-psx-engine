using anakinsoft.system.cameras;
using anakinsoft.utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.system;
using rubens_psx_engine.system.config;
using anakinsoft.system;
using anakinsoft.game.scenes.lounge;
using anakinsoft.game.scenes.lounge.characters;
using anakinsoft.game.scenes.lounge.evidence;
using anakinsoft.game.scenes.lounge.ui;
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

        // Interrogation action UI
        InterrogationActionUI interrogationActionUI;

        // Confirmation dialog for accusations
        ConfirmationDialogUI confirmationDialog;

        // Character state machines
        anakinsoft.game.scenes.lounge.characters.LoungeCharactersData charactersData;
        BartenderStateMachine bartenderStateMachine;
        PathologistStateMachine pathologistStateMachine;

        // Suspect state machines (created during interrogation)
        CharacterStateMachine interrogationChar1StateMachine;
        CharacterStateMachine interrogationChar2StateMachine;

        // All interrogated character state machines (persistent across rounds)
        Dictionary<string, CharacterStateMachine> interrogatedCharacters;

        // Stress tracking for interrogation characters
        StressMeter char1StressMeter;
        StressMeter char2StressMeter;
        StressMeterUI char1StressUI;
        StressMeterUI char2StressUI;

        // Track active interrogation character (which one player is currently talking to)
        string activeInterrogationCharacter = null;
        bool isChar1Active = false; // true if char1, false if char2

        // Interrogation round management
        InterrogationRoundManager interrogationManager;
        ScreenFadeTransition fadeTransition;

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

            // Initialize interrogated characters dictionary
            interrogatedCharacters = new Dictionary<string, CharacterStateMachine>();

            // Debug: Skip to suspect selection if enabled
            var config = RenderingConfigManager.Config;
            if (config?.Development?.SkipToSuspectSelection == true)
            {
                gameProgress.CanSelectSuspects = true;
                gameProgress.HasSeenIntro = true;
                gameProgress.HasTalkedToBartender = true;
                gameProgress.PathologistSpawned = true;
                gameProgress.HasTalkedToPathologist = true;
                Console.WriteLine("[TheLoungeScreen] DEBUG: Skipping to suspect selection mode");
            }

            // Initialize inventory and dialogue choices
            inventory = new LoungeInventory();
            dialogueChoiceSystem = new DialogueChoiceSystem();
            transcriptReviewUI = new TranscriptReviewUI();

            // Initialize interrogation action UI
            interrogationActionUI = new InterrogationActionUI();
            interrogationActionUI.OnActionSelected += HandleInterrogationAction;

            // Initialize confirmation dialog
            confirmationDialog = new ConfirmationDialogUI();

            // Initialize stress meters
            char1StressUI = new StressMeterUI();
            char2StressUI = new StressMeterUI();

            // Wire up inventory swapping event
            inventory.OnItemSwappedOut += (interactableItem) =>
            {
                if (interactableItem != null)
                {
                    Console.WriteLine($"[TheLoungeScreen] Returning item to world: {interactableItem.Name}");
                    interactableItem.ReturnToWorld();
                }
                else
                {
                    // Item was autopsy report or other special item (no source to return to)
                    Console.WriteLine($"[TheLoungeScreen] Item swapped out but no source to return to");
                }
            };

            // Initialize interrogation management
            interrogationManager = new InterrogationRoundManager();
            fadeTransition = new ScreenFadeTransition(gd);

            // Load character data from YAML and initialize state machines
            LoadCharacterDataAndStateMachines();

            // Set up dialogue and camera transition event handlers
            SetupDialogueAndInteractionSystems();

            // Set up evidence item collection
            SetupEvidenceItems();

            // Set up autopsy report collection
            SetupAutopsyReport();

            // Set up suspects file interaction
            SetupSuspectsFile();

            // Set up interrogation system
            SetupInterrogationSystem();

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

                // Load character profiles into scene
                loungeScene.LoadCharacterProfiles(charactersData);

                // Load character profiles into character selection menu
                var profileManager = loungeScene.GetProfileManager();
                if (profileManager != null)
                {
                    characterSelectionMenu.LoadFromProfiles(profileManager);
                }

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
                Console.WriteLine("Dialogue ended");

                // Show interrogation action UI ONLY if we're interrogating a suspect character (not bartender/pathologist)
                // AND the character is not dismissed or at 100% stress
                // DO NOT transition camera back - stay in dialogue mode!
                // Keep portrait and stress meter visible during action selection
                if (interrogationManager.IsInterrogating && !string.IsNullOrEmpty(activeInterrogationCharacter))
                {
                    // Check if the active character is dismissed or at 100% stress
                    var activeChar = interrogationManager.CurrentPair?.Find(c => c.Name == activeInterrogationCharacter);
                    StressMeter activeMeter = isChar1Active ? char1StressMeter : char2StressMeter;
                    bool isStressed = activeMeter != null && activeMeter.IsMaxStress;

                    if ((activeChar != null && activeChar.IsDismissed) || isStressed)
                    {
                        Console.WriteLine($"[TheLoungeScreen] {activeInterrogationCharacter} is dismissed or at 100% stress - auto-dismissing");

                        // Auto-dismiss the character
                        if (isStressed && (activeChar == null || !activeChar.IsDismissed))
                        {
                            // Mark as dismissed if not already
                            interrogationManager.DismissCharacter(activeInterrogationCharacter);

                            // Show stress-out dialogue
                            var dismissDialogue = new DialogueSequence("StressedOutDismissal");
                            dismissDialogue.AddLine(activeInterrogationCharacter, "That's it! I'm done talking to you, Detective!");

                            dismissDialogue.OnSequenceComplete = () =>
                            {
                                Console.WriteLine($"[TheLoungeScreen] Stressed character dismissed - transitioning back to player");
                                activeInterrogationCharacter = null;
                                loungeScene.ClearActiveStressMeter();
                                loungeScene.ClearActiveDialogueCharacter();
                                cameraTransitionSystem.TransitionBackToPlayer(1.0f);
                            };

                            dialogueSystem.StartDialogue(dismissDialogue);
                        }
                        else
                        {
                            // Already dismissed - just transition back without additional dialogue
                            Console.WriteLine($"[TheLoungeScreen] Character already dismissed - transitioning back");
                            activeInterrogationCharacter = null;
                            loungeScene.ClearActiveStressMeter();
                            loungeScene.ClearActiveDialogueCharacter();
                            cameraTransitionSystem.TransitionBackToPlayer(1.0f);
                        }
                    }
                    else
                    {
                        Console.WriteLine("[TheLoungeScreen] Showing interrogation action UI - staying in dialogue mode");
                        interrogationActionUI.Show();
                        // Note: portrait and stress meter remain visible during action selection
                    }
                }
                else
                {
                    // Not interrogating - normal dialogue, clear portrait and transition camera back
                    Console.WriteLine("Returning camera to player (not interrogating)");
                    loungeScene.ClearActiveDialogueCharacter(); // This also clears stress meter
                    cameraTransitionSystem.TransitionBackToPlayer(1.0f);
                }
            };

            // Set up character selection menu events
            List<SelectableCharacter> pendingInterrogationCharacters = null;

            characterSelectionMenu.OnCharactersSelected += (characters) =>
            {
                Console.WriteLine($"{characters.Count} character(s) selected for interrogation:");
                foreach (var character in characters)
                {
                    Console.WriteLine($"  - {character.Name}");
                }

                // Store selected characters for the round
                pendingInterrogationCharacters = characters;

                // Start fade out, then begin interrogation round
                fadeTransition.FadeOut(1.0f);
            };

            // Update fade out handler to use pending characters
            fadeTransition.OnFadeOutComplete += () =>
            {
                if (pendingInterrogationCharacters != null && pendingInterrogationCharacters.Count > 0)
                {
                    Console.WriteLine("[TheLoungeScreen] Fade out complete - starting interrogation round");
                    interrogationManager.StartRound(pendingInterrogationCharacters);
                    pendingInterrogationCharacters = null;
                }
            };

            characterSelectionMenu.OnMenuClosed += () =>
            {
                Console.WriteLine("Character selection menu closed");
            };
        }

        private void SetupEvidenceItems()
        {
            // Wire up all evidence items to inventory system
            SetupEvidenceItem(loungeScene.GetEvidenceVial());
            SetupEvidenceItem(loungeScene.GetSecurityLog());
            SetupEvidenceItem(loungeScene.GetDatapad());
            SetupEvidenceItem(loungeScene.GetKeycard());
        }

        private void SetupEvidenceItem(InteractableItem item)
        {
            if (item != null)
            {
                item.OnItemCollected += (collectedItem) =>
                {
                    Console.WriteLine($"Collected item: {collectedItem.Item.Name}");
                    inventory.PickUpItem(collectedItem.Item, collectedItem);
                };
            }
        }

        private void SetupAutopsyReport()
        {
            var autopsyReport = loungeScene.GetAutopsyReport();
            if (autopsyReport != null)
            {
                autopsyReport.OnReportCollected += (report) =>
                {
                    Console.WriteLine($"Collected {report.ReportTitle}");

                    // Add to inventory
                    var reportItem = new InventoryItem(
                        id: "autopsy_report",
                        name: "Autopsy Report",
                        description: "Dr. Harmon Kerrigan's preliminary autopsy findings."
                    );
                    inventory.PickUpItem(reportItem, null); // null source since it can't be swapped back

                    // Update pathologist state machine
                    pathologistStateMachine.OnAutopsyReportDelivered();

                    // Trigger dialogue with pathologist
                    Console.WriteLine("[TheLoungeScreen] Autopsy report collected - talk to pathologist to continue");
                };

                // Handle transcript viewing after round 1
                autopsyReport.OnTranscriptViewed += (report) =>
                {
                    Console.WriteLine($"[TheLoungeScreen] Viewing autopsy report transcript");

                    // Gather all character state machines
                    var stateMachines = new Dictionary<string, CharacterStateMachine>();

                    // Add bartender and pathologist
                    if (bartenderStateMachine != null)
                        stateMachines["Bartender Zix"] = bartenderStateMachine;
                    if (pathologistStateMachine != null)
                        stateMachines["Dr. Harmon Kerrigan"] = pathologistStateMachine;

                    // Add all interrogated characters from persistent dictionary
                    foreach (var kvp in interrogatedCharacters)
                    {
                        stateMachines[kvp.Key] = kvp.Value;
                    }

                    // Open transcript UI with state machines
                    transcriptReviewUI.Open(stateMachines);
                    Console.WriteLine($"[TheLoungeScreen] Opened transcript review UI with {stateMachines.Count} characters");
                };
            }
        }

        private void SetupSuspectsFile()
        {
            var file = loungeScene.GetSuspectsFile();
            if (file != null)
            {
                file.OnFileOpened += (suspectsFile) =>
                {
                    Console.WriteLine("Opening suspects file");

                    // Suspects file ALWAYS shows character selection when allowed
                    if (gameProgress.CanSelectSuspects)
                    {
                        Console.WriteLine("[TheLoungeScreen] Showing character selection menu");
                        characterSelectionMenu.Show();
                    }
                    else
                    {
                        // Cannot select suspects yet - just show message or nothing
                        Console.WriteLine("[TheLoungeScreen] Cannot select suspects yet");
                        // TODO: Show message to player that they can't select suspects yet
                    }
                };

                // Debug: Enable suspects file if skipping to suspect selection
                var config = RenderingConfigManager.Config;
                if (config?.Development?.SkipToSuspectSelection == true)
                {
                    file.CanInteract = true;
                    Console.WriteLine("[TheLoungeScreen] DEBUG: Suspects file enabled for interaction");
                }
            }
        }

        private void SetupInterrogationDialogue()
        {
            var char1 = loungeScene.GetInterrogationCharacter1();
            var char2 = loungeScene.GetInterrogationCharacter2();

            if (char1 != null && char1.Interaction != null)
            {
                // Get dialogue from state machine
                var dialogue1 = GetInterrogationDialogue(interrogationChar1StateMachine, char1.Name);
                if (dialogue1 != null)
                {
                    // Set dialogue on the character
                    char1.Interaction.SetDialogue(dialogue1);
                    Console.WriteLine($"[TheLoungeScreen] Set dialogue for {char1.Name}: {dialogue1.SequenceName}");
                }

                char1.Interaction.OnDialogueTriggered += (sequence) =>
                {
                    Console.WriteLine($"[TheLoungeScreen] Starting interrogation with {char1.Name}");

                    // Set active character
                    activeInterrogationCharacter = char1.Name;
                    isChar1Active = true;

                    // Check if character is dismissed or at 100% stress - auto-dismiss them
                    var char1SelectableChar = interrogationManager.CurrentPair?.Find(c => c.Name == char1.Name);
                    bool isStressed = char1StressMeter != null && char1StressMeter.IsMaxStress;

                    if ((char1SelectableChar != null && char1SelectableChar.IsDismissed) || isStressed)
                    {
                        Console.WriteLine($"[TheLoungeScreen] {char1.Name} is dismissed or stressed - auto-dismissing and transitioning back");

                        // Create a brief dismissal dialogue
                        var dismissedDialogue = new DialogueSequence("CharacterAutoDismissed");
                        if (isStressed && !char1SelectableChar.IsDismissed)
                        {
                            dismissedDialogue.AddLine(char1.Name, "That's it! I'm done talking to you, Detective!");
                            // Mark as dismissed
                            interrogationManager.DismissCharacter(char1.Name);
                        }
                        else
                        {
                            dismissedDialogue.AddLine(char1.Name, "I've said all I have to say, Detective.");
                        }

                        // Auto-transition back after dismissed dialogue completes - no interrogation options
                        dismissedDialogue.OnSequenceComplete = () =>
                        {
                            Console.WriteLine($"[TheLoungeScreen] Dismissed dialogue complete - transitioning back to player");
                            activeInterrogationCharacter = null;
                            loungeScene.ClearActiveStressMeter();
                            cameraTransitionSystem.TransitionBackToPlayer(1.0f);
                        };

                        char1.Interaction.SetDialogue(dismissedDialogue);
                        dialogueSystem.StartDialogue(dismissedDialogue);
                        return;
                    }

                    // Get fresh dialogue from state machine in case state changed
                    var currentDialogue = GetInterrogationDialogue(interrogationChar1StateMachine, char1.Name);
                    if (currentDialogue != null)
                    {
                        // Update character's dialogue
                        char1.Interaction.SetDialogue(currentDialogue);

                        // Note: No auto-dismiss - player must manually dismiss or reach 100% stress
                        // The OnSequenceComplete just updates state machine state

                        // Set active dialogue character to show portrait
                        var portraitKey = GetCharacterPortraitKey(char1.Name);
                        if (!string.IsNullOrEmpty(portraitKey))
                        {
                            loungeScene.SetActiveDialogueCharacter(portraitKey);
                        }

                        // Start camera transition and begin dialogue after transition completes
                        cameraTransitionSystem.TransitionToInteraction(char1.Interaction.CameraInteractionPosition,
                            char1.Interaction.CameraInteractionLookAt, 1.0f);

                        // Subscribe to transition complete event to start dialogue
                        Action onTransitionComplete = null;
                        onTransitionComplete = () =>
                        {
                            cameraTransitionSystem.OnTransitionToInteractionComplete -= onTransitionComplete;

                            // Set stress meter on UI manager for portrait display (char1)
                            loungeScene.SetActiveStressMeter(char1StressMeter);

                            dialogueSystem.StartDialogue(currentDialogue);
                        };
                        cameraTransitionSystem.OnTransitionToInteractionComplete += onTransitionComplete;
                    }
                };
            }

            if (char2 != null && char2.Interaction != null)
            {
                // Get dialogue from state machine
                var dialogue2 = GetInterrogationDialogue(interrogationChar2StateMachine, char2.Name);
                if (dialogue2 != null)
                {
                    // Set dialogue on the character
                    char2.Interaction.SetDialogue(dialogue2);
                    Console.WriteLine($"[TheLoungeScreen] Set dialogue for {char2.Name}: {dialogue2.SequenceName}");
                }

                char2.Interaction.OnDialogueTriggered += (sequence) =>
                {
                    Console.WriteLine($"[TheLoungeScreen] Starting interrogation with {char2.Name}");

                    // Set active character
                    activeInterrogationCharacter = char2.Name;
                    isChar1Active = false;

                    // Check if character is dismissed or at 100% stress - auto-dismiss them
                    var char2SelectableChar = interrogationManager.CurrentPair?.Find(c => c.Name == char2.Name);
                    bool isStressed = char2StressMeter != null && char2StressMeter.IsMaxStress;

                    if ((char2SelectableChar != null && char2SelectableChar.IsDismissed) || isStressed)
                    {
                        Console.WriteLine($"[TheLoungeScreen] {char2.Name} is dismissed or stressed - auto-dismissing and transitioning back");

                        // Create a brief dismissal dialogue
                        var dismissedDialogue = new DialogueSequence("CharacterAutoDismissed");
                        if (isStressed && !char2SelectableChar.IsDismissed)
                        {
                            dismissedDialogue.AddLine(char2.Name, "That's it! I'm done talking to you, Detective!");
                            // Mark as dismissed
                            interrogationManager.DismissCharacter(char2.Name);
                        }
                        else
                        {
                            dismissedDialogue.AddLine(char2.Name, "I've said all I have to say, Detective.");
                        }

                        // Auto-transition back after dismissed dialogue completes - no interrogation options
                        dismissedDialogue.OnSequenceComplete = () =>
                        {
                            Console.WriteLine($"[TheLoungeScreen] Dismissed dialogue complete - transitioning back to player");
                            activeInterrogationCharacter = null;
                            loungeScene.ClearActiveStressMeter();
                            cameraTransitionSystem.TransitionBackToPlayer(1.0f);
                        };

                        char2.Interaction.SetDialogue(dismissedDialogue);
                        dialogueSystem.StartDialogue(dismissedDialogue);
                        return;
                    }

                    // Get fresh dialogue from state machine in case state changed
                    var currentDialogue = GetInterrogationDialogue(interrogationChar2StateMachine, char2.Name);
                    if (currentDialogue != null)
                    {
                        // Update character's dialogue
                        char2.Interaction.SetDialogue(currentDialogue);

                        // Note: No auto-dismiss - player must manually dismiss or reach 100% stress
                        // The OnSequenceComplete just updates state machine state

                        // Set active dialogue character to show portrait
                        var portraitKey = GetCharacterPortraitKey(char2.Name);
                        if (!string.IsNullOrEmpty(portraitKey))
                        {
                            loungeScene.SetActiveDialogueCharacter(portraitKey);
                        }

                        // Start camera transition and begin dialogue after transition completes
                        cameraTransitionSystem.TransitionToInteraction(char2.Interaction.CameraInteractionPosition,
                            char2.Interaction.CameraInteractionLookAt, 1.0f);

                        // Subscribe to transition complete event to start dialogue
                        Action onTransitionComplete = null;
                        onTransitionComplete = () =>
                        {
                            cameraTransitionSystem.OnTransitionToInteractionComplete -= onTransitionComplete;

                            // Set stress meter on UI manager for portrait display (char2)
                            loungeScene.SetActiveStressMeter(char2StressMeter);

                            dialogueSystem.StartDialogue(currentDialogue);
                        };
                        cameraTransitionSystem.OnTransitionToInteractionComplete += onTransitionComplete;
                    }
                };
            }
        }

        private void SetupInterrogationSystem()
        {
            fadeTransition.OnFadeInComplete += () =>
            {
                Console.WriteLine("[TheLoungeScreen] Fade in complete - interrogation round active");
            };

            // Wire up interrogation round events
            interrogationManager.OnRoundStarted += (hoursRemaining) =>
            {
                Console.WriteLine($"[TheLoungeScreen] Round started - {hoursRemaining} hours remaining");

                // Mark interrogation in progress
                characterSelectionMenu.SetInterrogationInProgress(true);

                // On first round, convert autopsy report to transcript mode and clear inventory
                if (interrogationManager.CurrentRound == 1)
                {
                    var autopsyReport = loungeScene.GetAutopsyReport();
                    if (autopsyReport != null)
                    {
                        autopsyReport.ConvertToTranscriptMode();
                        Console.WriteLine("[TheLoungeScreen] Autopsy report converted to transcript mode");
                    }

                    // Clear inventory (autopsy report was delivered to pathologist)
                    inventory.Clear();
                    Console.WriteLine("[TheLoungeScreen] Inventory cleared for interrogation");
                }

                // TODO: Display time message to player
            };

            interrogationManager.OnRoundEnded += (hoursRemaining) =>
            {
                Console.WriteLine($"[TheLoungeScreen] Round ended - {hoursRemaining} hours remaining");

                // Despawn interrogation characters
                loungeScene.DespawnInterrogationCharacters();

                // Hide stress meters
                char1StressUI.Hide();
                char2StressUI.Hide();

                // Mark interrogation no longer in progress
                characterSelectionMenu.SetInterrogationInProgress(false);

                // Calculate hours passed and show time passage message
                int hoursPassed = interrogationManager.CurrentRound;
                loungeScene.ShowTimePassageMessage(hoursPassed, hoursRemaining);

                // Note: Character selection is NOT auto-opened
                // Player must interact with suspects file to select next round
                if (hoursRemaining > 0)
                {
                    Console.WriteLine("[TheLoungeScreen] Round complete - player can select more suspects via suspects file");
                }
            };

            interrogationManager.OnAllRoundsComplete += () =>
            {
                Console.WriteLine("[TheLoungeScreen] All interrogation rounds complete - time to deduce the killer");
                // TODO: Transition to deduction phase
            };

            interrogationManager.OnCharactersSpawned += (characters) =>
            {
                Console.WriteLine($"[TheLoungeScreen] Spawning {characters.Count} characters for interrogation");

                // Reset player and camera to starting position
                loungeScene.ResetPlayerPosition();
                fpsCamera.SetRotation(loungeScene.GetCharacterInitialRotation());
                Console.WriteLine("[TheLoungeScreen] Reset player and camera for new round");

                // Create state machines for interrogation characters
                CreateInterrogationStateMachines(characters);

                // Create and show stress meters for interrogation characters
                CreateStressMeters(characters);

                // Spawn characters at interrogation positions (pass round number)
                loungeScene.SpawnInterrogationCharacters(characters, interrogationManager.CurrentRound);

                // Wire up dialogue completion to dismiss characters
                SetupInterrogationDialogue();

                // Fade back in after spawning
                fadeTransition.FadeIn(1.0f);
            };
        }

        /// <summary>
        /// Create state machines for interrogation characters based on their character keys
        /// </summary>
        private void CreateInterrogationStateMachines(List<SelectableCharacter> characters)
        {
            if (characters == null || characters.Count == 0)
            {
                Console.WriteLine("[TheLoungeScreen] ERROR: No characters provided for state machine creation");
                return;
            }

            // Create state machine for character 1
            if (characters.Count > 0)
            {
                var char1Key = GetCharacterKey(characters[0].Name);
                var char1Config = charactersData?.GetCharacter(char1Key);

                if (char1Config != null)
                {
                    interrogationChar1StateMachine = CreateStateMachineForCharacter(char1Key, char1Config);
                    if (interrogationChar1StateMachine != null)
                    {
                        // Add to persistent interrogated characters dictionary
                        interrogatedCharacters[characters[0].Name] = interrogationChar1StateMachine;
                        Console.WriteLine($"[TheLoungeScreen] Created state machine for {characters[0].Name} (key: {char1Key})");
                    }
                    else
                    {
                        Console.WriteLine($"[TheLoungeScreen] ERROR: State machine is NULL for {characters[0].Name} (key: {char1Key})");
                    }
                }
                else
                {
                    Console.WriteLine($"[TheLoungeScreen] WARNING: Could not find config for character: {char1Key}");
                }
            }

            // Create state machine for character 2
            if (characters.Count > 1)
            {
                var char2Key = GetCharacterKey(characters[1].Name);
                var char2Config = charactersData?.GetCharacter(char2Key);

                if (char2Config != null)
                {
                    interrogationChar2StateMachine = CreateStateMachineForCharacter(char2Key, char2Config);
                    if (interrogationChar2StateMachine != null)
                    {
                        // Add to persistent interrogated characters dictionary
                        interrogatedCharacters[characters[1].Name] = interrogationChar2StateMachine;
                        Console.WriteLine($"[TheLoungeScreen] Created state machine for {characters[1].Name} (key: {char2Key})");
                    }
                    else
                    {
                        Console.WriteLine($"[TheLoungeScreen] ERROR: State machine is NULL for {characters[1].Name} (key: {char2Key})");
                    }
                }
                else
                {
                    Console.WriteLine($"[TheLoungeScreen] WARNING: Could not find config for character: {char2Key}");
                }
            }
        }

        /// <summary>
        /// Create stress meters for interrogation characters
        /// </summary>
        private void CreateStressMeters(List<SelectableCharacter> characters)
        {
            if (characters == null || characters.Count == 0)
            {
                Console.WriteLine("[TheLoungeScreen] ERROR: No characters provided for stress meter creation");
                return;
            }

            // Create stress meter for character 1
            if (characters.Count > 0)
            {
                char1StressMeter = new StressMeter();

                // Get character config for occupation
                var char1Key = GetCharacterKey(characters[0].Name);
                var char1Config = charactersData?.GetCharacter(char1Key);
                string occupation1 = char1Config?.role ?? "Suspect";

                // Pass portrait key for looking up portrait texture
                char1StressUI.Show(char1StressMeter, characters[0].Name, occupation1, characters[0].PortraitKey);

                // Wire up max stress event to auto-dismiss
                char1StressMeter.OnMaxStressReached += () =>
                {
                    Console.WriteLine($"[TheLoungeScreen] {characters[0].Name} reached max stress - dismissing");
                    interrogationManager.DismissCharacter(characters[0].Name);
                };

                Console.WriteLine($"[TheLoungeScreen] Created stress meter for {characters[0].Name}");
            }

            // Create stress meter for character 2
            if (characters.Count > 1)
            {
                char2StressMeter = new StressMeter();

                // Get character config for occupation
                var char2Key = GetCharacterKey(characters[1].Name);
                var char2Config = charactersData?.GetCharacter(char2Key);
                string occupation2 = char2Config?.role ?? "Suspect";

                // Pass portrait key for looking up portrait texture
                char2StressUI.Show(char2StressMeter, characters[1].Name, occupation2, characters[1].PortraitKey);

                // Wire up max stress event to auto-dismiss
                char2StressMeter.OnMaxStressReached += () =>
                {
                    Console.WriteLine($"[TheLoungeScreen] {characters[1].Name} reached max stress - dismissing");
                    interrogationManager.DismissCharacter(characters[1].Name);
                };

                Console.WriteLine($"[TheLoungeScreen] Created stress meter for {characters[1].Name}");
            }
        }

        /// <summary>
        /// Create the appropriate state machine for a character based on their key
        /// </summary>
        private CharacterStateMachine CreateStateMachineForCharacter(string characterKey, CharacterConfig config)
        {
            return characterKey switch
            {
                "commander_von" => new CommanderVonStateMachine(config),
                "dr_thorne" => new DrThorneStateMachine(config),
                "lt_webb" => new LtWebbStateMachine(config),
                "ensign_tork" => new EnsignTorkStateMachine(config),
                "maven_kilroth" => new MavenKilrothStateMachine(config),
                "chief_solis" => new ChiefSolisStateMachine(config),
                "tvora" => new TehvoraStateMachine(config),
                "lucky_chen" => new LuckyChenStateMachine(config),
                _ => null
            };
        }

        /// <summary>
        /// Convert character display name to character key for data lookup
        /// </summary>
        private string GetCharacterKey(string displayName)
        {
            // Map display names to character keys
            // Handles both old hardcoded names and new profile names
            return displayName switch
            {
                "Commander Sylar Von" => "commander_von",
                "Commander Sylara Von" => "commander_von",  // New profile name
                "Dr. Thorne" => "dr_thorne",
                "Dr. Lyssa Thorne" => "dr_thorne",  // New profile name
                "Lt. Marcus Webb" => "lt_webb",
                "Lieutenant Marcus Webb" => "lt_webb",  // New profile name
                "Ensign Tork" => "ensign_tork",
                "Maven Kilroth" => "maven_kilroth",
                "Chief Kala Solis" => "chief_solis",
                "Chief Petty Officer Raina Solis" => "chief_solis",  // New profile name
                "Tehvora" => "tvora",
                "T'Vora" => "tvora",  // New profile name
                "Lucky Chen" => "lucky_chen",
                _ => displayName.ToLower().Replace(" ", "_").Replace(".", "")
            };
        }

        /// <summary>
        /// Get portrait key for a character name from the profile manager
        /// </summary>
        private string GetCharacterPortraitKey(string characterName)
        {
            var profileManager = loungeScene.GetProfileManager();
            if (profileManager == null) return null;

            var characterKey = GetCharacterKey(characterName);
            var profile = profileManager.GetProfile(characterKey);
            return profile?.PortraitKey;
        }

        /// <summary>
        /// Handle interrogation action selection
        /// </summary>
        private void HandleInterrogationAction(InterrogationAction action)
        {
            Console.WriteLine($"[TheLoungeScreen] Interrogation action selected: {action}");

            switch (action)
            {
                case InterrogationAction.Alibi:
                    HandleAlibiAction();
                    break;
                case InterrogationAction.Relationship:
                    HandleRelationshipAction();
                    break;
                case InterrogationAction.Doubt:
                    HandleDoubtAction();
                    break;
                case InterrogationAction.Accuse:
                    HandleAccuseAction();
                    break;
                case InterrogationAction.StepAway:
                    HandleStepAwayAction();
                    break;
                case InterrogationAction.Dismiss:
                    HandleDismissAction();
                    break;
            }
        }

        /// <summary>
        /// Handle Alibi action - ask about whereabouts
        /// </summary>
        private void HandleAlibiAction()
        {
            Console.WriteLine("[TheLoungeScreen] Handling Alibi action");
            var dialogue = GetDialogueForAction("alibi");
            if (dialogue != null)
            {
                dialogueSystem.StartDialogue(dialogue);
            }
        }

        /// <summary>
        /// Handle Relationship action - ask about relationship with victim
        /// </summary>
        private void HandleRelationshipAction()
        {
            Console.WriteLine("[TheLoungeScreen] Handling Relationship action");
            var dialogue = GetDialogueForAction("relationship");
            if (dialogue != null)
            {
                dialogueSystem.StartDialogue(dialogue);
            }
        }

        /// <summary>
        /// Handle Doubt action - press for more information
        /// </summary>
        private void HandleDoubtAction()
        {
            Console.WriteLine("[TheLoungeScreen] Handling Doubt action");
            var dialogue = GetDialogueForAction("doubt");
            if (dialogue != null)
            {
                dialogueSystem.StartDialogue(dialogue);
            }
        }

        /// <summary>
        /// Handle Accuse action - directly accuse of murder (with confirmation)
        /// </summary>
        private void HandleAccuseAction()
        {
            Console.WriteLine("[TheLoungeScreen] Handling Accuse action - showing confirmation");

            // Clear any previous handlers and set new ones
            confirmationDialog.OnYes += ProceedWithAccusation;
            confirmationDialog.OnNo += CancelAccusation;

            confirmationDialog.Show("Accuse without proper evidence?");
        }

        /// <summary>
        /// Cancel the accusation
        /// </summary>
        private void CancelAccusation()
        {
            Console.WriteLine("[TheLoungeScreen] Accusation cancelled");
            // Clear handlers
            confirmationDialog.OnYes -= ProceedWithAccusation;
            confirmationDialog.OnNo -= CancelAccusation;
        }

        /// <summary>
        /// Actually perform the accusation after confirmation
        /// </summary>
        private void ProceedWithAccusation()
        {
            Console.WriteLine("[TheLoungeScreen] Proceeding with accusation");

            // Clear handlers
            confirmationDialog.OnYes -= ProceedWithAccusation;
            confirmationDialog.OnNo -= CancelAccusation;

            var dialogue = GetDialogueForAction("accuse");
            if (dialogue != null)
            {
                dialogueSystem.StartDialogue(dialogue);
            }
        }

        /// <summary>
        /// Get dialogue for a specific action from the active character's config
        /// </summary>
        private DialogueSequence GetDialogueForAction(string actionType)
        {
            if (string.IsNullOrEmpty(activeInterrogationCharacter))
            {
                Console.WriteLine($"[TheLoungeScreen] ERROR: No active character for action {actionType}");
                return null;
            }

            // Get the active character's state machine
            CharacterStateMachine stateMachine = isChar1Active ? interrogationChar1StateMachine : interrogationChar2StateMachine;
            if (stateMachine == null)
            {
                Console.WriteLine($"[TheLoungeScreen] ERROR: No state machine for active character");
                return null;
            }

            // Get character config
            var characterKey = GetCharacterKey(activeInterrogationCharacter);
            var config = charactersData.GetCharacter(characterKey);
            if (config == null || config.dialogue == null)
            {
                Console.WriteLine($"[TheLoungeScreen] ERROR: No config/dialogue for {characterKey}");
                return null;
            }

            // Find dialogue sequence matching this action
            var dialogueSequence = config.dialogue.Find(d => d.action == actionType);
            if (dialogueSequence == null)
            {
                Console.WriteLine($"[TheLoungeScreen] WARNING: No {actionType} dialogue for {characterKey}, using fallback");
                // Fallback dialogue
                var fallback = new DialogueSequence($"{actionType}_fallback");
                fallback.AddLine(activeInterrogationCharacter, $"I have nothing to say about that, Detective.");
                return fallback;
            }

            // Apply stress for doubt/accuse actions (happens regardless of correctness)
            if (actionType == "accuse")
            {
                Console.WriteLine($"[TheLoungeScreen] Accuse action - increasing stress to max");
                IncreaseCurrentCharacterStress(100f); // Max stress on accusation
            }
            else if (actionType == "doubt")
            {
                Console.WriteLine($"[TheLoungeScreen] Doubt action - increasing stress");
                IncreaseCurrentCharacterStress(15f); // Moderate stress on doubt
            }

            // Convert to DialogueSequence
            var sequence = new DialogueSequence(dialogueSequence.sequence_name);
            foreach (var line in dialogueSequence.lines)
            {
                sequence.AddLine(line.speaker, line.text);
            }

            return sequence;
        }

        /// <summary>
        /// Handle Step Away action - temporarily leave interrogation to check evidence
        /// </summary>
        private void HandleStepAwayAction()
        {
            Console.WriteLine("[TheLoungeScreen] Handling Step Away action");

            if (string.IsNullOrEmpty(activeInterrogationCharacter))
            {
                Console.WriteLine("[TheLoungeScreen] ERROR: No active interrogation character");
                return;
            }

            // Hide action UI
            interrogationActionUI.Hide();

            // Keep portrait and stress visible
            // Don't clear activeInterrogationCharacter - we're just stepping away temporarily
            Console.WriteLine($"[TheLoungeScreen] Player stepping away from {activeInterrogationCharacter} to check evidence");

            // Transition camera back to player - but interrogation remains active
            cameraTransitionSystem.TransitionBackToPlayer(1.0f);
        }

        /// <summary>
        /// Handle Dismiss action - end interrogation
        /// </summary>
        private void HandleDismissAction()
        {
            Console.WriteLine("[TheLoungeScreen] Handling Dismiss action");

            if (string.IsNullOrEmpty(activeInterrogationCharacter))
            {
                Console.WriteLine("[TheLoungeScreen] ERROR: No active interrogation character to dismiss");
                return;
            }

            // Dismiss the active character
            Console.WriteLine($"[TheLoungeScreen] Player dismissed {activeInterrogationCharacter}");
            interrogationManager.DismissCharacter(activeInterrogationCharacter);

            // Clear active character and stress meter from portrait
            activeInterrogationCharacter = null;
            loungeScene.ClearActiveStressMeter();

            // Now transition camera back to player - interrogation session ended
            Console.WriteLine("[TheLoungeScreen] Transitioning camera back after dismiss");
            cameraTransitionSystem.TransitionBackToPlayer(1.0f);
        }

        /// <summary>
        /// Increase stress for the currently active interrogation character
        /// </summary>
        private void IncreaseCurrentCharacterStress(float amount)
        {
            if (string.IsNullOrEmpty(activeInterrogationCharacter))
            {
                Console.WriteLine("[TheLoungeScreen] ERROR: No active interrogation character for stress increase");
                return;
            }

            // Increase stress for the correct character
            StressMeter activeMeter = isChar1Active ? char1StressMeter : char2StressMeter;
            if (activeMeter != null)
            {
                Console.WriteLine($"[TheLoungeScreen] Increasing {activeInterrogationCharacter} stress by {amount}");
                activeMeter.IncreaseStress(amount);
            }
        }

        /// <summary>
        /// Get dialogue from state machine for interrogation character
        /// </summary>
        private DialogueSequence GetInterrogationDialogue(CharacterStateMachine stateMachine, string characterName)
        {
            if (stateMachine == null)
            {
                var characterKey = GetCharacterKey(characterName);
                Console.WriteLine($"[TheLoungeScreen] ERROR: State machine not initialized for {characterName} (key: {characterKey})");
                Console.WriteLine($"[TheLoungeScreen] interrogationChar1StateMachine is null: {interrogationChar1StateMachine == null}");
                Console.WriteLine($"[TheLoungeScreen] interrogationChar2StateMachine is null: {interrogationChar2StateMachine == null}");
                return null;
            }

            // Get current dialogue from state machine
            var yamlDialogue = stateMachine.GetCurrentDialogue();
            if (yamlDialogue == null)
            {
                Console.WriteLine($"[TheLoungeScreen] No dialogue available from {characterName} state machine");
                return null;
            }

            // Convert YAML dialogue to DialogueSequence
            var sequence = new DialogueSequence(yamlDialogue.sequence_name);
            foreach (var line in yamlDialogue.lines)
            {
                sequence.AddLine(line.speaker, line.text);
            }

            // Wire up completion callback to state machine
            sequence.OnSequenceComplete = () =>
            {
                Console.WriteLine($"[TheLoungeScreen] {characterName} dialogue '{yamlDialogue.sequence_name}' complete");
                stateMachine.OnDialogueComplete(yamlDialogue.sequence_name);
            };

            return sequence;
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

                    // Handle completion actions
                    if (yamlDialogue.on_complete == "show_report")
                    {
                        Console.WriteLine("[TheLoungeScreen] Pathologist asks you to get autopsy report - enabling it now");

                        // Disable suspects file
                        var file = loungeScene.GetSuspectsFile();
                        if (file != null)
                        {
                            file.CanInteract = false;
                            Console.WriteLine("[TheLoungeScreen] Suspects file is disabled");
                        }

                        // Enable the autopsy report for interaction
                        var autopsyReport = loungeScene.GetAutopsyReport();
                        if (autopsyReport != null)
                        {
                            autopsyReport.CanInteract = true;
                            Console.WriteLine("[TheLoungeScreen] Autopsy report is now interactable");
                        }
                    }
                    else if (yamlDialogue.on_complete == "show_character_selection")
                    {
                        Console.WriteLine("[TheLoungeScreen] Pathologist evidence presented - enabling suspects file");
                        gameProgress.CanSelectSuspects = true;

                        // Enable the suspects file for interaction
                        var file = loungeScene.GetSuspectsFile();
                        if (file != null)
                        {
                            file.CanInteract = true;
                            Console.WriteLine("[TheLoungeScreen] Suspects file is now interactable");
                        }

                        // Disable the autopsy report (already delivered)
                        var autopsyReport = loungeScene.GetAutopsyReport();
                        if (autopsyReport != null)
                        {
                            autopsyReport.CanInteract = false;
                            Console.WriteLine("[TheLoungeScreen] Autopsy report is disabled");
                        }
                    }
                };
            }

            return sequence;
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
                // Set active dialogue character to show portrait during conversation
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
                // Set active dialogue character to show portrait during conversation
                loungeScene.SetActiveDialogueCharacter("DrHarmon");
                dialogueSystem.StartDialogue(pathologist.DialogueSequence);
            }
        }

        public override void Update(GameTime gameTime)
        {
            // Update confirmation dialog first (highest priority)
            if (confirmationDialog.IsActive)
            {
                confirmationDialog.Update(gameTime);
                return; // Don't update other systems while dialog is active
            }

            // Update transcript review UI (high priority when open)
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

            // Update interrogation action UI (but don't return - let animations continue)
            if (interrogationActionUI.IsActive)
            {
                interrogationActionUI.Update(gameTime);
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
            // Include interrogation action UI as "dialogue active" to prevent movement but allow animations
            bool isDialogueActive = dialogueSystem.IsActive || dialogueChoiceSystem.IsActive || transcriptReviewUI.IsActive || interrogationActionUI.IsActive;
            loungeScene.UpdateWithCamera(gameTime, fpsCamera, isDialogueActive);

            // Update camera transition system
            cameraTransitionSystem.Update(gameTime);

            // Mount FPS camera to character controller (only when not showing intro and not in dialogue/interaction mode)
            if (!loungeScene.IsShowingIntroText() && !cameraTransitionSystem.IsInInteractionMode && !cameraTransitionSystem.IsTransitioning)
            {
                UpdateCameraMountedToCharacter();
            }

            // Update fade transition
            fadeTransition.Update(gameTime);

            // Update time passage message
            loungeScene.UpdateTimePassageMessage(gameTime);

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

            // Show mouse cursor when character selection menu, transcript review, interrogation actions, or confirmation dialog are active
            if (characterSelectionMenu.IsActive || transcriptReviewUI.IsActive || interrogationActionUI.IsActive || confirmationDialog.IsActive)
            {
                Globals.screenManager.IsMouseVisible = true;
            }
            else
            {
                Globals.screenManager.IsMouseVisible = false;
            }

            // Only update FPS camera when not showing intro, not in dialogue, not in menus, and not transitioning
            if (!loungeScene.IsShowingIntroText() && !dialogueSystem.IsActive &&
                !cameraTransitionSystem.IsInInteractionMode && !cameraTransitionSystem.IsTransitioning &&
                !characterSelectionMenu.IsActive && !transcriptReviewUI.IsActive && !interrogationActionUI.IsActive)
            {
                fpsCamera.Update(gameTime);
            }

            if (InputManager.GetKeyboardClick(Keys.Escape))
            {
                // Don't allow ESC during dialogue - causes serious bugs
                // Player must complete dialogue normally
                if (!dialogueSystem.IsActive && !dialogueChoiceSystem.IsActive)
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

            // Draw UI (pass dialogue active state to hide interaction prompts during dialogue AND interrogation actions)
            bool isInDialogueMode = dialogueSystem.IsActive || dialogueChoiceSystem.IsActive || interrogationActionUI.IsActive;
            loungeScene.DrawUI(gameTime, fpsCamera, spriteBatch, isInDialogueMode);

            // Draw dialogue UI on top
            // Note: Stress bar now integrated with portrait, not in dialogue box
            if (dialogueSystem.IsActive && font != null)
            {
                dialogueSystem.Draw(spriteBatch, font);
            }

            // Draw dialogue choice UI
            if (dialogueChoiceSystem.IsActive && font != null)
            {
                dialogueChoiceSystem.Draw(spriteBatch, font);
            }

            // Draw inventory at center-left of screen
            if (inventory.HasItem && font != null && !transcriptReviewUI.IsActive)
            {
                var viewport = Globals.screenManager.GraphicsDevice.Viewport;
                string inventoryText = inventory.GetDisplayText();
                Vector2 textSize = font.MeasureString(inventoryText);

                // Position at center-left with some padding
                Vector2 position = new Vector2(
                    30, // Left padding
                    (viewport.Height - textSize.Y) / 2 // Vertically centered
                );

                // Draw background box (darker for better readability)
                Rectangle bgRect = new Rectangle(
                    (int)position.X - 10,
                    (int)position.Y - 5,
                    (int)textSize.X + 20,
                    (int)textSize.Y + 10
                );
                DrawFilledRectangle(spriteBatch, bgRect, Color.Black * 0.9f);
                DrawRectangleBorder(spriteBatch, bgRect, Color.Yellow, 2);

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

            // Note: Stress meter is now integrated into dialogue UI and shown during action selection
            // No need for standalone stress meter drawing

            // Draw interrogation action UI
            if (interrogationActionUI.IsActive && font != null)
            {
                interrogationActionUI.Draw(spriteBatch, font);
            }

            // Draw confirmation dialog (on top of everything except fade)
            if (confirmationDialog.IsActive && font != null)
            {
                confirmationDialog.Draw(spriteBatch, font);
            }

            // Draw fade transition (always last, on top of everything)
            var viewportBounds = Globals.screenManager.GraphicsDevice.Viewport;
            fadeTransition.Draw(spriteBatch, new Rectangle(0, 0, viewportBounds.Width, viewportBounds.Height));
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

        /// <summary>
        /// Helper method to draw a filled rectangle
        /// </summary>
        private void DrawFilledRectangle(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            var texture = new Microsoft.Xna.Framework.Graphics.Texture2D(Globals.screenManager.GraphicsDevice, 1, 1);
            texture.SetData(new[] { Color.White });
            spriteBatch.Draw(texture, rect, color);
            texture.Dispose();
        }

        /// <summary>
        /// Helper method to draw a rectangle border
        /// </summary>
        private void DrawRectangleBorder(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            var texture = new Microsoft.Xna.Framework.Graphics.Texture2D(Globals.screenManager.GraphicsDevice, 1, 1);
            texture.SetData(new[] { Color.White });

            // Top
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            // Left
            spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            spriteBatch.Draw(texture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);

            texture.Dispose();
        }
    }
}
