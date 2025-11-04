using anakinsoft.system.cameras;
using anakinsoft.utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.system;
using rubens_psx_engine.system.config;
using anakinsoft.system;
using anakinsoft.game.scenes.lounge;
using anakinsoft.game.scenes.lounge.characters;
using anakinsoft.game.scenes.lounge.evidence;
using anakinsoft.game.scenes.lounge.ui;
using anakinsoft.entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        public GameAudioManager GetAudioManager { get { return audioManager; } }

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

        // Evidence selection UI
        EvidenceSelectionUI evidenceSelectionUI;
        string pendingEvidenceId = null;

        // Character state machines
        anakinsoft.game.scenes.lounge.characters.LoungeCharactersData charactersData;
        BartenderStateMachine bartenderStateMachine;
        PathologistStateMachine pathologistStateMachine;

        // Suspect state machines (created during interrogation)
        CharacterStateMachine interrogationChar1StateMachine;
        CharacterStateMachine interrogationChar2StateMachine;

        // All interrogated character state machines (persistent across rounds)
        Dictionary<string, CharacterStateMachine> interrogatedCharacters;

        // Stress UI for interrogation characters (stress tracked in state machines)
        StressMeterUI char1StressUI;
        StressMeterUI char2StressUI;

        // Track active interrogation character (which one player is currently talking to)
        string activeInterrogationCharacter = null;
        bool isChar1Active = false; // true if char1, false if char2

        // Interrogation round management
        InterrogationRoundManager interrogationManager;
        ScreenFadeTransition fadeTransition;

        // Audio manager
        rubens_psx_engine.system.GameAudioManager audioManager;

        // Finale trigger
        bool hasTriggeredFinale = false;

        public TheLoungeScreen()
        {
            var gd = Globals.screenManager.getGraphicsDevice.GraphicsDevice;

            // Initialize fade transition FIRST to ensure black screen from the very start
            fadeTransition = new ScreenFadeTransition(gd);
            fadeTransition.SetBlack();

            fpsCamera = new FPSCamera(gd, new Vector3(0, 20.0f, 0));
            loungeScene = new TheLoungeScene();
            SetScene(loungeScene); // Register scene with physics screen for automatic disposal

            // Initialize audio manager
            audioManager = new GameAudioManager(Globals.screenManager.Content);
            audioManager.LoadContent();

            // Pass audio manager to UI manager for text blip sounds
            loungeScene.GetUIManager().SetAudioManager(audioManager);

            // Subscribe to intro text completion event to start fade-in and background music
            loungeScene.GetUIManager().OnIntroTextComplete += () =>
            {
                Console.WriteLine("[TheLoungeScreen] Intro text complete - starting fade in and background music");
                fadeTransition.FadeIn(1.0f);
                audioManager.PlayBackgroundMusic();
            };

            // Create camera and set its rotation from the character's initial rotation
            fpsCamera.SetRotation(loungeScene.GetCharacterInitialRotation());

            // Initialize dialogue system
            dialogueSystem = new DialogueSystem();
            dialogueSystem.SetAudioManager(audioManager); // Wire audio manager for text blip sounds
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

            // Initialize evidence selection UI
            evidenceSelectionUI = new EvidenceSelectionUI();
            evidenceSelectionUI.OnEvidenceSelected += HandleEvidenceSelected;
            evidenceSelectionUI.OnCancelled += HandleEvidenceSelectionCancelled;

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
            interrogationManager = new InterrogationRoundManager(gameProgress);

            // DON'T start fade here - will start in first Update() call after initialization

            // Load character data from YAML and initialize state machines
            LoadCharacterDataAndStateMachines();

            // Set up dialogue and camera transition event handlers
            SetupDialogueAndInteractionSystems();

            // Set up evidence item collection
            SetupEvidenceItems();

            // Set up autopsy report collection
            SetupAutopsyReport();

            // Evidence items are now read-only (hover to view description only)
            // Evidence presentation happens through dialogue system

            // Set up suspects file interaction
            SetupSuspectsFile();

            // Set up interrogation system
            SetupInterrogationSystem();

            // Set up finale restart handler
            loungeScene.OnRestartInvestigationRequested += OnRestartInvestigationRequested;

            // Hide mouse cursor for immersive FPS experience
            Globals.screenManager.IsMouseVisible = false;
        }

        private void OnRestartInvestigationRequested()
        {
            Console.WriteLine("[TheLoungeScreen] Restarting investigation - loading new scene");
            // Exit current screen and load a fresh TheLoungeScreen
            ExitScreen();
            Globals.screenManager.AddScreen(new TheLoungeScreen());
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
                // Camera transition state is now checked automatically via cameraTransitionSystem.IsTransitioning
            };

            cameraTransitionSystem.OnTransitionToPlayerComplete += () =>
            {
                Console.WriteLine("Camera returned to player control");
                // Camera transition state is now checked automatically via cameraTransitionSystem.IsTransitioning
            };

            // Set up dialogue events
            dialogueSystem.OnDialogueStart += () =>
            {
                Console.WriteLine("Dialogue started");
            };

            dialogueSystem.OnDialogueEnd += () =>
            {
                Console.WriteLine($"Dialogue ended - IsInterrogating: {interrogationManager.IsInterrogating}, activeChar: {activeInterrogationCharacter}");

                // Show interrogation action UI ONLY if we're interrogating a suspect character (not bartender/pathologist)
                // AND the character is not dismissed or at 100% stress
                // DO NOT transition camera back - stay in dialogue mode!
                // Keep portrait and stress meter visible during action selection
                if (!string.IsNullOrEmpty(activeInterrogationCharacter))
                {
                    // Check if the active character is dismissed or at 100% stress
                    var activeChar = interrogationManager.CurrentPair?.Find(c => c.Name == activeInterrogationCharacter);
                    bool isStressed = false;
                    if (interrogatedCharacters.TryGetValue(activeInterrogationCharacter, out var activeStateMachine))
                    {
                        isStressed = activeStateMachine.IsMaxStress;
                    }

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

            // Handle finale button click (no longer used - finale starts automatically after round 3)
            characterSelectionMenu.OnFinaleButtonClicked += () =>
            {
                Console.WriteLine("[TheLoungeScreen] Finale button clicked (deprecated - should not happen)");
                characterSelectionMenu.Hide();
            };

            // Pass audio manager to finale intro sequence for warp speed sound and music
            loungeScene.GetFinaleIntroSequence().SetAudioManager(audioManager);

            // Pass audio manager to finale ending screen for win/lose jingles
            loungeScene.GetFinaleEndingScreen().SetAudioManager(audioManager);

            // Handle finale intro sequence completion
            loungeScene.GetFinaleIntroSequence().OnSequenceComplete += () =>
            {
                Console.WriteLine("[TheLoungeScreen] Finale intro sequence complete - setting bartender to finale ready");
                // Transition bartender to finale ready state so player can talk to Zix
                if (bartenderStateMachine != null)
                {
                    bartenderStateMachine.SetFinaleReady();

                    // Update bartender's dialogue to show FinaleReady
                    var bartender = loungeScene.GetBartender();
                    if (bartender != null)
                    {
                        var finaleDialogue = GetBartenderDialogue();
                        if (finaleDialogue != null)
                        {
                            bartender.SetDialogue(finaleDialogue);
                            Console.WriteLine("[TheLoungeScreen] Bartender dialogue updated to FinaleReady");
                        }
                    }
                }
            };

            // Update fade out handler to use pending characters
            fadeTransition.OnFadeOutComplete += () =>
            {
                if (pendingInterrogationCharacters != null && pendingInterrogationCharacters.Count > 0)
                {
                    Console.WriteLine("[TheLoungeScreen] Fade out complete - starting interrogation round");

                    // If characters were dismissed, continue to next round first (shows time passage)
                    if (interrogationManager.AllCharactersDismissed)
                    {
                        Console.WriteLine("[TheLoungeScreen] Continuing from dismissed characters to next round");
                        interrogationManager.ContinueToNextRound();
                    }

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

            // Note: Security Log, Datapad, and Keycard are now EvidenceDocument objects
            // They are examined (like AutopsyReport/SuspectsFile), not collected as items
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

                    // Return any evidence document currently held to the table
                    var evidenceInventory = loungeScene.GetEvidenceInventory();
                    if (evidenceInventory != null && evidenceInventory.HasDocument)
                    {
                        evidenceInventory.DropDocument();
                        Console.WriteLine("[TheLoungeScreen] Returned evidence document to table before picking up autopsy report");
                    }

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

        private void SetupEvidenceInventory()
        {
            var evidenceInventory = loungeScene.GetEvidenceInventory();
            if (evidenceInventory != null)
            {
                // When picking up an evidence document, return autopsy report if holding it
                evidenceInventory.OnDocumentPickedUp += (document) =>
                {
                    // If holding autopsy report in old inventory, return it to table
                    if (inventory.HasItem && inventory.HasItemById("autopsy_report"))
                    {
                        var autopsyReport = loungeScene.GetAutopsyReport();
                        if (autopsyReport != null)
                        {
                            autopsyReport.ReturnToWorld();
                            Console.WriteLine("[TheLoungeScreen] Returned autopsy report to table when picking up evidence document");
                        }
                        inventory.Clear();
                    }
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

                    // Check if all rounds are complete
                    if (interrogationManager.CurrentRound >= 3 && !gameProgress.CanSelectSuspects)
                    {
                        Console.WriteLine("[TheLoungeScreen] All rounds complete - directing player to Begin Finale");
                        // Show message directing player to use Begin Finale button
                        // The player should click the "Begin Finale" button instead
                    }
                    else if (gameProgress.CanSelectSuspects)
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
                    bool isStressed = false;
                    if (interrogatedCharacters.TryGetValue(char1.Name, out var char1StateMachine))
                    {
                        isStressed = char1StateMachine.IsMaxStress;
                    }

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

                            // Set state machine on UI manager for portrait display (char1)
                            if (interrogatedCharacters.TryGetValue(char1.Name, out var char1SM))
                            {
                                loungeScene.SetActiveStressMeter(char1SM);
                                dialogueSystem.SetActiveCharacter(char1SM); // Also set for transcript recording
                            }

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
                    bool isStressed = false;
                    if (interrogatedCharacters.TryGetValue(char2.Name, out var char2StateMachine))
                    {
                        isStressed = char2StateMachine.IsMaxStress;
                    }

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

                            // Set state machine on UI manager for portrait display (char2)
                            if (interrogatedCharacters.TryGetValue(char2.Name, out var char2SM))
                            {
                                loungeScene.SetActiveStressMeter(char2SM);
                                dialogueSystem.SetActiveCharacter(char2SM); // Also set for transcript recording
                            }

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

                // Update bartender to show hint/rumor for this round
                if (bartenderStateMachine != null)
                {
                    bartenderStateMachine.SetRoundHint(interrogationManager.CurrentRound);

                    // Update bartender's dialogue to reflect new state
                    var bartender = loungeScene.GetBartender();
                    if (bartender != null)
                    {
                        var bartenderDialogue = GetBartenderDialogue();
                        if (bartenderDialogue != null)
                        {
                            bartender.SetDialogue(bartenderDialogue);
                            Console.WriteLine($"[TheLoungeScreen] Bartender dialogue updated for round {interrogationManager.CurrentRound}");
                        }
                    }
                }

                // If round 3, disable character selection and show finale button
                if (interrogationManager.CurrentRound == 3)
                {
                    Console.WriteLine("[TheLoungeScreen] Round 3 started - showing finale button (disabled until both dismissed)");
                    gameProgress.CanSelectSuspects = false;

                    // Show finale button in the character selection menu (disabled until both dismissed)
                    characterSelectionMenu.ShowFinaleButton();
                }

                // On first round, enable all evidence items and prepare for interrogation
                if (interrogationManager.CurrentRound == 1)
                {
                    // Enable all evidence documents for examination (read-only)
                    loungeScene.EnableAllEvidenceDocuments();
                    Console.WriteLine("[TheLoungeScreen] All evidence documents enabled for round 1 (read-only)");

                    // Evidence is now read-only (hover to view) - no inventory management needed
                    // Evidence presentation happens through dialogue system

                    // Autopsy report is already in transcript mode (converted when delivered to Dr. Harmon)
                    // Just make sure it's visible and interactable
                    var autopsyReport = loungeScene.GetAutopsyReport();
                    if (autopsyReport != null && autopsyReport.IsTranscriptMode)
                    {
                        autopsyReport.CanInteract = true;
                        Console.WriteLine("[TheLoungeScreen] Autopsy report transcript enabled for round 1");
                    }

                    // Clear old inventory (autopsy report was in old system)
                    inventory.Clear();
                    Console.WriteLine("[TheLoungeScreen] Inventory cleared for interrogation");
                }

                // TODO: Display time message to player
            };

            // New event: Both characters dismissed but still seated
            interrogationManager.OnBothCharactersDismissed += () =>
            {
                Console.WriteLine("[TheLoungeScreen] Both characters dismissed - staying seated until player continues");

                // Hide stress meters
                char1StressUI.Hide();
                char2StressUI.Hide();

                // Mark interrogation no longer in progress (allows suspects file interaction)
                characterSelectionMenu.SetInterrogationInProgress(false);

                // If round 3, start the finale intro sequence immediately
                if (interrogationManager.CurrentRound == 3)
                {
                    Console.WriteLine("[TheLoungeScreen] Round 3 complete - starting finale intro sequence");
                    hasTriggeredFinale = true; // Mark as triggered

                    // Start the finale intro sequence (fade, text, ship arrival)
                    // This will play while player transitions back to FPS mode
                    loungeScene.StartFinaleIntro();
                }
                else
                {
                    // Characters remain seated - player can walk around and select next round
                    Console.WriteLine("[TheLoungeScreen] Player can now select next round via suspects file");
                }
            };

            interrogationManager.OnRoundEnded += (hoursRemaining) =>
            {
                Console.WriteLine($"[TheLoungeScreen] Round ended - {hoursRemaining} hours remaining");

                // Despawn interrogation characters NOW (after player selects new round)
                loungeScene.DespawnInterrogationCharacters();

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
                Console.WriteLine("[TheLoungeScreen] All interrogation rounds complete - disabling suspect selection");
                gameProgress.CanSelectSuspects = false; // Disable character selection after round 3

                // Player must now use the "Begin Finale" button instead of selecting more suspects
                Console.WriteLine("[TheLoungeScreen] Player must now click 'Begin Finale' to proceed");
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

            // Wire up stress UI for character 1 (using state machine stress)
            if (characters.Count > 0)
            {
                // Get character config for occupation
                var char1Key = GetCharacterKey(characters[0].Name);
                var char1Config = charactersData?.GetCharacter(char1Key);
                string occupation1 = char1Config?.role ?? "Suspect";

                // Get state machine for character 1
                if (interrogatedCharacters.TryGetValue(characters[0].Name, out var char1StateMachine))
                {
                    // Pass state machine to UI (UI will read stress from it)
                    char1StressUI.Show(char1StateMachine, characters[0].Name, occupation1, characters[0].PortraitKey);

                    // Wire up max stress event to auto-dismiss
                    char1StateMachine.OnMaxStressReached += () =>
                    {
                        Console.WriteLine($"[TheLoungeScreen] {characters[0].Name} reached max stress - dismissing");
                        interrogationManager.DismissCharacter(characters[0].Name);
                    };

                    Console.WriteLine($"[TheLoungeScreen] Wired up stress UI for {characters[0].Name}");
                }
            }

            // Wire up stress UI for character 2 (using state machine stress)
            if (characters.Count > 1)
            {
                // Get character config for occupation
                var char2Key = GetCharacterKey(characters[1].Name);
                var char2Config = charactersData?.GetCharacter(char2Key);
                string occupation2 = char2Config?.role ?? "Suspect";

                // Get state machine for character 2
                if (interrogatedCharacters.TryGetValue(characters[1].Name, out var char2StateMachine))
                {
                    // Pass state machine to UI (UI will read stress from it)
                    char2StressUI.Show(char2StateMachine, characters[1].Name, occupation2, characters[1].PortraitKey);

                    // Wire up max stress event to auto-dismiss
                    char2StateMachine.OnMaxStressReached += () =>
                    {
                        Console.WriteLine($"[TheLoungeScreen] {characters[1].Name} reached max stress - dismissing");
                        interrogationManager.DismissCharacter(characters[1].Name);
                    };

                    Console.WriteLine($"[TheLoungeScreen] Wired up stress UI for {characters[1].Name}");
                }
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
            Console.WriteLine("[TheLoungeScreen] Handling Accuse action - showing evidence selection");

            // Hide the interrogation action UI
            interrogationActionUI.Hide();

            // Build list of available evidence from evidence table
            var availableEvidence = new List<EvidenceItem>
            {
                new EvidenceItem("dna_evidence", "DNA Analysis Report", "Commander Von and Dr. Thorne DNA found under Ambassador's fingernails"),
                new EvidenceItem("access_codes", "Door Access Logs", "Von's code used at 0200h (time of murder), Thorne at 2100h"),
                new EvidenceItem("medical_training", "Combat Medic Certification", "Proves Von has advanced medical training for precise injections"),
                new EvidenceItem("breturium_sample", "Breturium Sample", "Murder weapon - rare exotic material"),
                new EvidenceItem("security_log", "Security Logs", "Shows unusual access patterns that night"),
                new EvidenceItem("datapad", "Encrypted Datapad", "Contains encrypted messages"),
                new EvidenceItem("keycard", "Ambassador's Keycard", "Shows recent usage patterns")
            };

            // Show evidence selection UI
            evidenceSelectionUI.Show(availableEvidence);
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
            Console.WriteLine("[TheLoungeScreen] Proceeding with accusation with evidence: " + pendingEvidenceId);

            // Clear handlers
            confirmationDialog.OnYes -= ProceedWithAccusation;
            confirmationDialog.OnNo -= CancelAccusation;

            // Get the dialogue based on evidence presented
            var dialogue = GetDialogueForEvidence(pendingEvidenceId);
            if (dialogue != null)
            {
                dialogueSystem.StartDialogue(dialogue);
            }

            // Clear pending evidence
            pendingEvidenceId = null;
        }

        /// <summary>
        /// Handle evidence selection
        /// </summary>
        private void HandleEvidenceSelected(string evidenceId)
        {
            Console.WriteLine($"[TheLoungeScreen] Evidence selected: {evidenceId}");

            // Present evidence to get character's reaction
            PresentEvidenceToCharacter(evidenceId);
        }

        /// <summary>
        /// Present evidence to the active character and show their stress-based reaction
        /// </summary>
        private void PresentEvidenceToCharacter(string evidenceId)
        {
            if (string.IsNullOrEmpty(activeInterrogationCharacter))
            {
                Console.WriteLine("[TheLoungeScreen] ERROR: No active character for evidence presentation");
                return;
            }

            // Get the active state machine
            if (!interrogatedCharacters.TryGetValue(activeInterrogationCharacter, out var stateMachine))
            {
                Console.WriteLine($"[TheLoungeScreen] ERROR: No state machine found for {activeInterrogationCharacter}");
                return;
            }

            Console.WriteLine($"[TheLoungeScreen] Presenting evidence '{evidenceId}' to {activeInterrogationCharacter} at {stateMachine.StressPercentage:F1}% stress");

            // Notify state machine of evidence presentation
            stateMachine.OnPlayerAction("present_evidence", evidenceId);

            // Get stress-appropriate dialogue reaction from state machine
            CharacterDialogueSequence evidenceReaction = null;

            // Try character-specific GetEvidenceReaction if available
            if (stateMachine is CommanderVonStateMachine commanderVon)
            {
                evidenceReaction = commanderVon.GetEvidenceReaction(evidenceId);
            }
            else if (stateMachine is DrThorneStateMachine drThorne)
            {
                evidenceReaction = drThorne.GetEvidenceReaction(evidenceId);
            }
            else if (stateMachine is ChiefSolisStateMachine chiefSolis)
            {
                evidenceReaction = chiefSolis.GetEvidenceReaction(evidenceId);
            }
            else if (stateMachine is LtWebbStateMachine ltWebb)
            {
                evidenceReaction = ltWebb.GetEvidenceReaction(evidenceId);
            }

            if (evidenceReaction != null)
            {
                // Convert YAML dialogue to game dialogue sequence
                var gameDialogue = stateMachine.ConvertToDialogueSequence(evidenceReaction);

                if (gameDialogue != null)
                {
                    // Show the dialogue
                    dialogueSystem.StartDialogue(gameDialogue);

                    // Determine stress impact
                    float stressIncrease = 30f; // Default: correct/relevant evidence = 30% stress

                    // Use custom stress increase if specified in YAML
                    if (evidenceReaction.stress_increase > 0)
                    {
                        stressIncrease = evidenceReaction.stress_increase;
                    }

                    IncreaseCurrentCharacterStress(stressIncrease);
                    Console.WriteLine($"[TheLoungeScreen] Evidence reaction: {evidenceReaction.sequence_name}, stress +{stressIncrease}%");
                }
                else
                {
                    Console.WriteLine("[TheLoungeScreen] ERROR: Failed to convert evidence dialogue");
                }
            }
            else
            {
                // No dialogue for this evidence = wrong/irrelevant = max stress, auto-dismiss
                Console.WriteLine($"[TheLoungeScreen] WRONG EVIDENCE '{evidenceId}' - no dialogue found, maxing stress!");

                var wrongDialogue = new DialogueSequence("WrongEvidence");
                wrongDialogue.AddLine(activeInterrogationCharacter, "WHAT?! This doesn't prove ANYTHING! I'm done with this interrogation!");
                dialogueSystem.StartDialogue(wrongDialogue);

                // Max out stress (will trigger auto-dismiss)
                IncreaseCurrentCharacterStress(100f);
            }
        }

        /// <summary>
        /// Handle evidence selection cancelled
        /// </summary>
        private void HandleEvidenceSelectionCancelled()
        {
            Console.WriteLine("[TheLoungeScreen] Evidence selection cancelled");
            // Show interrogation UI again
            interrogationActionUI.Show();
        }

        /// <summary>
        /// Get display name for evidence ID
        /// </summary>
        private static string GetEvidenceName(string evidenceId)
        {
            return evidenceId switch
            {
                "dna_evidence" => "DNA Evidence",
                "access_codes" => "Access Code Logs",
                "medical_training" => "Medical Training Records",
                "breturium_sample" => "Breturium Sample",
                "security_log" => "Security Logs",
                "datapad" => "Encrypted Datapad",
                "keycard" => "Ambassador's Keycard",
                _ => "Evidence"
            };
        }

        /// <summary>
        /// Get dialogue based on evidence presented
        /// </summary>
        private DialogueSequence GetDialogueForEvidence(string evidenceId)
        {
            if (string.IsNullOrEmpty(activeInterrogationCharacter))
            {
                Console.WriteLine($"[TheLoungeScreen] ERROR: No active character for evidence presentation");
                return null;
            }

            // Get active state machine stress
            float stressLevel = 0f;
            if (interrogatedCharacters.TryGetValue(activeInterrogationCharacter, out var activeStateMachine))
            {
                stressLevel = activeStateMachine.StressPercentage;
            }

            Console.WriteLine($"[TheLoungeScreen] Presenting evidence '{evidenceId}' to {activeInterrogationCharacter}, stress level: {stressLevel}%");

            // Check if this is Commander Von with key evidence at 50%+ stress
            if (activeInterrogationCharacter.Contains("Von") || activeInterrogationCharacter.Contains("Sylara"))
            {
                return GetCommanderVonEvidenceResponse(evidenceId, stressLevel);
            }

            // Default: Wrong evidence or wrong character - max stress and dismiss
            var wrongEvidenceDialogue = new DialogueSequence("WrongEvidence");
            wrongEvidenceDialogue.AddLine(activeInterrogationCharacter, "What?! This doesn't prove anything! This interview is OVER!");

            // Max out stress (will auto-dismiss)
            IncreaseCurrentCharacterStress(100f);

            return wrongEvidenceDialogue;
        }

        /// <summary>
        /// Get Commander Von's response to evidence based on stress level
        /// </summary>
        private DialogueSequence GetCommanderVonEvidenceResponse(string evidenceId, float stressLevel)
        {
            // Check if stress is at 50% or higher (breakthrough threshold)
            bool canBreakthrough = stressLevel >= 50f;

            // DNA Evidence - smoking gun
            if (evidenceId == "dna_evidence" && canBreakthrough)
            {
                var confession = new DialogueSequence("VonDNAConfession");
                confession.AddLine(activeInterrogationCharacter, "*Long pause, staring at the evidence*");
                confession.AddLine(activeInterrogationCharacter, "...You don't understand Telirian culture, Detective.");
                confession.AddLine(activeInterrogationCharacter, "He was bound to my sister in an arranged marriage. We loved each other, but could never be together in this life.");
                confession.AddLine(activeInterrogationCharacter, "In our beliefs, only death can free a soul from such bonds. I... I freed him.");
                confession.AddLine(activeInterrogationCharacter, "The breturium, the precision - I gave him a warrior's death. Honorable. Quick.");
                confession.AddLine(activeInterrogationCharacter, "One day, in the spirit realm, we'll finally be together. No sister. No duty. Just us.");
                confession.AddLine(activeInterrogationCharacter, "I don't expect you to understand. Take me away.");

                // Mark as breakthrough - stress to max, auto-dismiss
                IncreaseCurrentCharacterStress(100f);

                Console.WriteLine("[TheLoungeScreen] BREAKTHROUGH! Commander Von confessed!");
                return confession;
            }

            // Access Codes - strong evidence
            if (evidenceId == "access_codes" && canBreakthrough)
            {
                var admission = new DialogueSequence("VonAccessCodeAdmission");
                admission.AddLine(activeInterrogationCharacter, "*Eyes narrow* My access code was used... because I was checking on him.");
                admission.AddLine(activeInterrogationCharacter, "I... I cared for him more than I should have. He was my sister's husband.");
                admission.AddLine(activeInterrogationCharacter, "The gym alibi? A lie. I was with him that night. But I didn't... I couldn't...");
                admission.AddLine(activeInterrogationCharacter, "*Voice breaking* This was supposed to free us both!");

                // Increase stress to max
                IncreaseCurrentCharacterStress(100f);

                Console.WriteLine("[TheLoungeScreen] BREAKTHROUGH! Commander Von admitted to being there!");
                return admission;
            }

            // Medical Training - circumstantial but damning
            if (evidenceId == "medical_training" && canBreakthrough)
            {
                var reaction = new DialogueSequence("VonMedicalTrainingReaction");
                reaction.AddLine(activeInterrogationCharacter, "Yes, I have advanced combat medic training. So what?");
                reaction.AddLine(activeInterrogationCharacter, "That's standard for security personnel! You're grasping at straws!");
                reaction.AddLine(activeInterrogationCharacter, "*Pause* ...Unless you have more evidence than this, Detective.");

                // Moderate stress increase
                IncreaseCurrentCharacterStress(30f);

                return reaction;
            }

            // Not enough stress or wrong evidence - she gets angry and dismisses
            var angryResponse = new DialogueSequence("VonAngryDismissal");
            angryResponse.AddLine(activeInterrogationCharacter, "You DARE accuse me with this... THIS?!");
            angryResponse.AddLine(activeInterrogationCharacter, "I have given EVERYTHING to protect the Ambassador!");
            angryResponse.AddLine(activeInterrogationCharacter, "This interview is OVER! Come back when you have real evidence!");

            // Max stress - auto-dismiss
            IncreaseCurrentCharacterStress(100f);

            return angryResponse;
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

            // Find dialogue sequence matching this action and current stress level
            var currentStress = stateMachine.StressPercentage;
            var actionDialogues = config.dialogue.Where(d => d.action == actionType).ToList();

            CharacterDialogueSequence dialogueSequence = null;

            // Find stress-appropriate dialogue
            foreach (var dialogue in actionDialogues)
            {
                float minStress = dialogue.requires_stress_above;
                float maxStress = dialogue.requires_stress_below > 0 ? dialogue.requires_stress_below : 100f;

                if (currentStress >= minStress && currentStress < maxStress)
                {
                    dialogueSequence = dialogue;
                    Console.WriteLine($"[TheLoungeScreen] Selected {actionType} dialogue '{dialogue.sequence_name}' for stress {currentStress:F1}% (range {minStress}-{maxStress})");
                    break;
                }
            }

            // Fallback to first matching action if no stress-specific match
            if (dialogueSequence == null && actionDialogues.Count > 0)
            {
                dialogueSequence = actionDialogues[0];
                Console.WriteLine($"[TheLoungeScreen] Using first {actionType} dialogue '{dialogueSequence.sequence_name}' (no stress match)");
            }

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
                float stressIncrease = 15f; // Default stress for doubt
                if (dialogueSequence.stress_increase > 0)
                {
                    stressIncrease = dialogueSequence.stress_increase;
                }
                Console.WriteLine($"[TheLoungeScreen] Doubt action - increasing stress by {stressIncrease}%");
                IncreaseCurrentCharacterStress(stressIncrease);
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

            // Increase stress directly on state machine
            if (interrogatedCharacters.TryGetValue(activeInterrogationCharacter, out var stateMachine))
            {
                Console.WriteLine($"[TheLoungeScreen] Increasing {activeInterrogationCharacter} stress by {amount}");
                stateMachine.IncreaseStress(amount);
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

            // Check if all rounds are complete - if so, override with FinaleReady dialogue
            if (interrogationManager.CurrentRound >= 3 && !gameProgress.CanSelectSuspects)
            {
                Console.WriteLine("[TheLoungeScreen] All rounds complete - checking for FinaleReady dialogue");

                // Try to get FinaleReady dialogue from loaded character data
                var finaleDialogue = charactersData?.bartender?.dialogue?.FirstOrDefault(d => d.sequence_name == "FinaleReady");
                if (finaleDialogue != null)
                {
                    Console.WriteLine("[TheLoungeScreen] Found FinaleReady dialogue, using it");
                    yamlDialogue = finaleDialogue;
                }
                else
                {
                    Console.WriteLine("[TheLoungeScreen] ERROR: Could not find FinaleReady dialogue, using current state dialogue");
                }
            }
            // Check if we're in an active interrogation round - if so, provide round-based hint
            else if (interrogationManager != null && interrogationManager.CurrentRound > 0 && interrogationManager.CurrentRound <= 3)
            {
                // Check if both characters have been dismissed (meaning player is between interrogations)
                if (interrogationManager.AllCharactersDismissed && !characterSelectionMenu.IsInterrogationInProgress)
                {
                    Console.WriteLine($"[TheLoungeScreen] Round {interrogationManager.CurrentRound} active - providing round hint");

                    // Get the appropriate round hint
                    string hintSequenceName = $"BartenderRound{interrogationManager.CurrentRound}Hint";
                    var roundHint = charactersData?.bartender?.dialogue?.FirstOrDefault(d => d.sequence_name == hintSequenceName);

                    if (roundHint != null)
                    {
                        Console.WriteLine($"[TheLoungeScreen] Found round hint: {hintSequenceName}");
                        yamlDialogue = roundHint;
                    }
                    else
                    {
                        Console.WriteLine($"[TheLoungeScreen] WARNING: Could not find {hintSequenceName}, using default dialogue");
                    }
                }
            }

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

            // Special handling for FinaleReady dialogue - show dialogue choices
            if (yamlDialogue.sequence_name == "FinaleReady")
            {
                sequence.OnSequenceComplete = () =>
                {
                    Console.WriteLine($"[TheLoungeScreen] FinaleReady dialogue complete - showing choices");
                    bartenderStateMachine.OnDialogueComplete(yamlDialogue.sequence_name);

                    // Show dialogue choices if they exist in the YAML
                    if (yamlDialogue.choices != null && yamlDialogue.choices.Count > 0)
                    {
                        var dialogueOptions = new List<DialogueOption>();

                        foreach (var choice in yamlDialogue.choices)
                        {
                            var option = new DialogueOption(choice.text, () =>
                            {
                                Console.WriteLine($"[TheLoungeScreen] Player chose: {choice.text} -> {choice.next_sequence}");

                                // Handle "FinaleQuestions" - show Zix's intro dialogue first
                                if (choice.next_sequence == "FinaleQuestions")
                                {
                                    Console.WriteLine("[TheLoungeScreen] Starting FinaleQuestions dialogue sequence");
                                    // Find and trigger FinaleQuestions dialogue
                                    var finaleQuestionsDialogue = charactersData?.bartender?.dialogue?.FirstOrDefault(d => d.sequence_name == "FinaleQuestions");
                                    if (finaleQuestionsDialogue != null)
                                    {
                                        var finaleSequence = new DialogueSequence(finaleQuestionsDialogue.sequence_name);
                                        foreach (var line in finaleQuestionsDialogue.lines)
                                        {
                                            finaleSequence.AddLine(line.speaker, line.text);
                                        }

                                        // When this dialogue completes, THEN start the finale questions
                                        finaleSequence.OnSequenceComplete = () =>
                                        {
                                            Console.WriteLine("[TheLoungeScreen] FinaleQuestions dialogue complete - starting finale UI");
                                            loungeScene.StartFinaleQuestions();
                                        };

                                        dialogueSystem.StartDialogue(finaleSequence);
                                    }
                                }
                                // Handle "FinaleNotReady" - player wants to review more evidence
                                else if (choice.next_sequence == "FinaleNotReady")
                                {
                                    Console.WriteLine("[TheLoungeScreen] Player wants to review more evidence");
                                    // Find and trigger FinaleNotReady dialogue
                                    var notReadyDialogue = charactersData?.bartender?.dialogue?.FirstOrDefault(d => d.sequence_name == "FinaleNotReady");
                                    if (notReadyDialogue != null)
                                    {
                                        var notReadySequence = new DialogueSequence(notReadyDialogue.sequence_name);
                                        foreach (var line in notReadyDialogue.lines)
                                        {
                                            notReadySequence.AddLine(line.speaker, line.text);
                                        }
                                        dialogueSystem.StartDialogue(notReadySequence);
                                    }
                                }
                            });
                            dialogueOptions.Add(option);
                        }

                        dialogueChoiceSystem.ShowChoices("", dialogueOptions, mouseOnly: true);
                    }
                };
                return sequence;
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

                        // Convert autopsy report to transcript mode and return to table
                        var autopsyReport = loungeScene.GetAutopsyReport();
                        if (autopsyReport != null)
                        {
                            autopsyReport.ConvertToTranscriptMode();
                            autopsyReport.ReturnToWorld();
                            Console.WriteLine("[TheLoungeScreen] Autopsy report converted to transcript mode and returned to table");
                        }

                        // Clear the old inventory (remove autopsy report)
                        inventory.Clear();
                        Console.WriteLine("[TheLoungeScreen] Inventory cleared after delivering autopsy report");
                    }
                };
            }

            return sequence;
        }

        private void OnBartenderDialogueTriggered(DialogueSequence sequence)
        {
            Console.WriteLine($"Bartender dialogue triggered");

            // CRITICAL: Do not start bartender interaction if dialogue is already active
            if (dialogueSystem.IsActive)
            {
                Console.WriteLine("[TheLoungeScreen] ERROR: Cannot start bartender interaction - dialogue is already active. Aborting.");
                return;
            }

            // Get current dialogue from state machine
            var currentDialogue = GetBartenderDialogue();
            if (currentDialogue == null)
            {
                Console.WriteLine("[TheLoungeScreen] No dialogue available from bartender state machine");
                return;
            }

            Console.WriteLine($"[TheLoungeScreen] Retrieved dialogue: {currentDialogue.SequenceName} from state: {bartenderStateMachine.CurrentState}");

            // SIMPLIFIED: Set portrait immediately when E is pressed
            loungeScene.SetActiveDialogueCharacter("NPC_Bartender");

            // Transition camera to bartender
            var bartender = loungeScene.GetBartender();
            if (bartender != null)
            {
                // Update the character's dialogue to the current state machine dialogue
                bartender.SetDialogue(currentDialogue);

                // Disable interactions during camera transition

                cameraTransitionSystem.TransitionToInteraction(
                    bartender.CameraInteractionPosition,
                    bartender.CameraInteractionLookAt,
                    1.0f);

                // Set state machine for transcript recording
                dialogueSystem.SetActiveCharacter(bartenderStateMachine);

                // Start dialogue immediately - don't wait for camera
                dialogueSystem.StartDialogue(currentDialogue);
            }
        }

        private void OnPathologistDialogueTriggered(DialogueSequence sequence)
        {
            Console.WriteLine($"Pathologist dialogue triggered");

            // CRITICAL: Do not start pathologist interaction if dialogue is already active
            if (dialogueSystem.IsActive)
            {
                Console.WriteLine("[TheLoungeScreen] ERROR: Cannot start pathologist interaction - dialogue is already active. Aborting.");
                return;
            }

            // Get current dialogue from state machine
            var currentDialogue = GetPathologistDialogue();
            if (currentDialogue == null)
            {
                Console.WriteLine("[TheLoungeScreen] No dialogue available from pathologist state machine");
                return;
            }

            Console.WriteLine($"[TheLoungeScreen] Retrieved dialogue: {currentDialogue.SequenceName} from state: {pathologistStateMachine.CurrentState}");

            // SIMPLIFIED: Set portrait immediately when E is pressed
            loungeScene.SetActiveDialogueCharacter("DrHarmon");

            // Transition camera to pathologist
            var pathologist = loungeScene.GetPathologist();
            if (pathologist != null)
            {
                // Update the character's dialogue to the current state machine dialogue
                pathologist.SetDialogue(currentDialogue);

                // Disable interactions during camera transition

                cameraTransitionSystem.TransitionToInteraction(
                    pathologist.CameraInteractionPosition,
                    pathologist.CameraInteractionLookAt,
                    1.0f);

                // Set state machine for transcript recording
                dialogueSystem.SetActiveCharacter(pathologistStateMachine);

                // Start dialogue immediately - don't wait for camera
                dialogueSystem.StartDialogue(currentDialogue);
            }
        }

        // NOTE: Removed StartBartenderDialogueAfterTransition and StartPathologistDialogueAfterTransition
        // These callbacks were causing portrait override bugs because they subscribed to the same global event
        // Now we set portrait and start dialogue immediately when E is pressed (no waiting for camera)

        public override void Update(GameTime gameTime)
        {
            // Start ship rumbling ambient sound (will only play once, then loop)
            audioManager?.PlayShipRumbling();

            // Fade-in now starts when intro text completes (via OnIntroTextComplete event)
            // No timer-based fade start needed anymore

            // Check if finale should be triggered (when time reaches 0)
            if (interrogationManager != null && interrogationManager.HoursRemaining <= 0 && !hasTriggeredFinale)
            {
                TriggerFinale();
            }

            // Update evidence selection UI (highest priority)
            if (evidenceSelectionUI.IsVisible)
            {
                evidenceSelectionUI.Update(gameTime);
                return; // Don't update other systems while selecting evidence
            }

            // Update confirmation dialog (high priority)
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

            // Update dialogue system (but not while camera is transitioning to prevent race conditions)
            if (dialogueSystem.IsActive && !cameraTransitionSystem.IsTransitioning)
            {
                dialogueSystem.Update(gameTime);
            }

            // Update scene with camera for character movement (pass dialogue active state to disable interactions)
            // Include interrogation action UI as "dialogue active" to prevent movement but allow animations
            bool isDialogueActive = dialogueSystem.IsActive || dialogueChoiceSystem.IsActive || transcriptReviewUI.IsActive || interrogationActionUI.IsActive;
            loungeScene.UpdateWithCamera(gameTime, fpsCamera, isDialogueActive, cameraTransitionSystem.IsTransitioning);

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

        /// <summary>
        /// Trigger the finale availability when investigation time reaches 0
        /// Player must click the finale button in the character selection menu to actually start
        /// </summary>
        private void TriggerFinale()
        {
            hasTriggeredFinale = true;
            Console.WriteLine("[TheLoungeScreen] Investigation time expired - finale button is ready!");

            // The finale button is already enabled at the end of round 3
            // Player must open the character selection menu and click the finale button
            // That will trigger the intro sequence and then the finale questions
        }

        public override void UpdateInput(GameTime gameTime)
        {
            if (!Globals.screenManager.IsActive)
                return;

            // Show mouse cursor when character selection menu, transcript review, interrogation actions, evidence selection, confirmation dialog, dialogue choices, finale UI, or finale ending screen are active
            if (characterSelectionMenu.IsActive || transcriptReviewUI.IsActive || interrogationActionUI.IsActive || confirmationDialog.IsActive || evidenceSelectionUI.IsVisible || dialogueChoiceSystem.IsActive || loungeScene.IsFinaleUIActive || loungeScene.IsFinaleEndingScreenActive)
            {
                Globals.screenManager.IsMouseVisible = true;
            }
            else
            {
                Globals.screenManager.IsMouseVisible = false;
            }

            // Only update FPS camera when not showing intro, not in dialogue, not in menus, not in finale UI/ending screen, and not transitioning
            if (!loungeScene.IsShowingIntroText() && !dialogueSystem.IsActive &&
                !cameraTransitionSystem.IsInInteractionMode && !cameraTransitionSystem.IsTransitioning &&
                !characterSelectionMenu.IsActive && !transcriptReviewUI.IsActive && !interrogationActionUI.IsActive &&
                !loungeScene.IsFinaleUIActive && !loungeScene.IsFinaleEndingScreenActive)
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
            var viewportBounds = Globals.screenManager.GraphicsDevice.Viewport;

            // Draw fade transition FIRST (under everything)
            fadeTransition.Draw(spriteBatch, new Rectangle(0, 0, viewportBounds.Width, viewportBounds.Height));

            // Draw UI (pass dialogue active state to hide interaction prompts during dialogue AND interrogation actions AND camera transitions)
            bool isInDialogueMode = dialogueSystem.IsActive || dialogueChoiceSystem.IsActive || interrogationActionUI.IsActive || cameraTransitionSystem.IsTransitioning;
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

            // Draw inventory at center-left of screen (old inventory system - autopsy report)
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

                // Draw background box with better styling
                Rectangle bgRect = new Rectangle(
                    (int)position.X - 5,
                    (int)position.Y - 2,
                    (int)textSize.X + 10,
                    (int)textSize.Y + 4
                );

                // Draw semi-transparent background
                var backgroundTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                backgroundTexture.SetData(new[] { Color.Black });
                spriteBatch.Draw(backgroundTexture, bgRect, Color.Black * 0.7f);

                // Draw text with shadow for better readability
                spriteBatch.DrawString(font, inventoryText, position + Vector2.One, Color.Black); // Shadow
                spriteBatch.DrawString(font, inventoryText, position, Color.Yellow);
            }

            // Evidence items are now read-only (no inventory display needed)
            // Evidence is presented through dialogue system

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

            // NOTE: Stress meters are drawn by LoungeUIManager.DrawStressBar (integrated with portrait)
            // No separate stress meter drawing needed here

            // Draw interrogation action UI
            if (interrogationActionUI.IsActive && font != null)
            {
                interrogationActionUI.Draw(spriteBatch, font);
            }

            // Draw evidence selection UI (on top of most things)
            if (evidenceSelectionUI.IsVisible && font != null)
            {
                evidenceSelectionUI.Draw(spriteBatch, font);
            }

            // Draw confirmation dialog (on top of everything)
            if (confirmationDialog.IsActive && font != null)
            {
                confirmationDialog.Draw(spriteBatch, font);
            }
        }

        public override void Draw3D(GameTime gameTime)
        {
            // Skip 3D rendering if we're still in black screen mode
            if (fadeTransition != null && !fadeTransition.IsBlack)
            {
                // Draw the lounge scene
                loungeScene.Draw(gameTime, fpsCamera);
            }
        }

        public override Color? GetBackgroundColor()
        {
            // Keep background black until fade transition is ready
            if (fadeTransition == null || fadeTransition.IsBlack)
            {
                return Color.Black;
            }

            // Return the lounge scene's background color
            return loungeScene.BackgroundColor;
        }

        /// <summary>
        /// Disable color quantization and dithering for The Lounge scene
        /// Set to false to disable PSX-style post-processing effects
        /// </summary>
        public bool EnableColorQuantizationDithering { get; set; } = true;

        public override bool? OverridePostProcessing()
        {
            // If dithering is disabled, turn off all post-processing
            // (since dithering is the main PSX-style effect)
            if (!EnableColorQuantizationDithering)
            {
                return false;
            }
            return null; // Use global config default
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
