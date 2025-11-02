using anakinsoft.entities;
using anakinsoft.game.scenes.lounge;
using anakinsoft.game.scenes.lounge.characters;
using anakinsoft.game.scenes.lounge.evidence;
using anakinsoft.game.scenes.lounge.finale;
using anakinsoft.game.scenes.lounge.ui;
using anakinsoft.system;
using anakinsoft.system.character;
using anakinsoft.system.physics;
using anakinsoft.utilities;
using BepuPhysics;
using BepuPhysics.Collidables;
using Demos.Demos.Characters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.entities;
using rubens_psx_engine.Extensions;
using rubens_psx_engine.system.lighting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Matrix = Microsoft.Xna.Framework.Matrix;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// The Lounge scene with models from models/lounge and first person controller
    ///
    /// TODO: Refactor mesh loading code into LoungeSceneMeshLoader
    /// TODO: Move CreateStaticMesh() and related physics mesh code to mesh loader
    /// TODO: Move physics collision mesh data (meshTriangleVertices, staticMeshTransforms) to dedicated physics class
    /// TODO: Consider breaking scene into smaller focused components:
    ///       - LoungeVisualSetup (rendering/meshes)
    ///       - LoungePhysicsSetup (collision)
    ///       - LoungeCharacterSetup (NPCs, already partially done with LoungeCharacterManager)
    ///       - LoungeInteractionSetup (items, evidence table)
    /// </summary>
    public class TheLoungeScene : Scene
    {
        // Debug settings
        public bool ShowPhysicsWireframe = false; // Toggle to show/hide physics collision wireframes

        // Starfield shader parameters (exposed for tweaking)
        public float StarDensity = 150.0f;           // Higher = more stars
        public float StarBrightness = 3.0f;         // Star brightness multiplier
        public float StarSize = 0.1f;             // Star size threshold (smaller = smaller stars)
        public float NebulaBrightness = 0.15f;      // Background nebula glow

        // Level scaling
        private const float LevelScale = 0.5f; // Scale factor for the entire level

        // Character system
        CharacterControllers characters;
        CharacterInput? character;
        bool characterActive;
        float characterLoadDelay = 1.0f; // Delay in seconds before character physics activate
        float timeSinceLoad = 0f;
        Quaternion characterInitialRotation = Quaternion.Identity; // Initial camera rotation

        // Input handling
        KeyboardState previousKeyboard;

        // Point light
        PointLight centerLight;

        // Interaction system
        InteractionSystem interactionSystem;

        // Characters
        private LoungeCharacterData bartender;
        private LoungeCharacterData pathologist;

        // Debug visualizer
        private LoungeDebugVisualizer debugVisualizer;

        // UI Manager
        private LoungeUIManager uiManager;

        // Starfield
        private LoungeStarfield starfield;
        private LoungeStarfieldSphere starfieldSphere;

        // Finale system
        private FinaleIntroSequence finaleIntro;
        private FinaleManager finaleManager;
        private FinaleUI finaleUI;
        private FinaleEndingScreen finaleEndingScreen;
        private RenderingEntity odysseusShip;

        // Mesh loader
        private LoungeSceneMeshLoader meshLoader;

        // Evidence vial item
        InteractableItem evidenceVial;

        // Additional evidence documents (7 new items)
        EvidenceDocument securityLog;
        EvidenceDocument datapad;
        EvidenceDocument keycard;
        EvidenceDocument dnaEvidence;
        EvidenceDocument accessLog;
        EvidenceDocument medicalRecord;
        EvidenceDocument breturiumSample;

        // List for easier iteration
        List<EvidenceDocument> evidenceDocuments;

        // Evidence table system
        EvidenceTable evidenceTable;

        // Evidence inventory (holds 1 item at a time)
        EvidenceInventory evidenceInventory;

        // Suspects file
        SuspectsFile suspectsFile;
        RenderingEntity suspectsFileVisual; // Cube placeholder for the file

        // Autopsy report
        AutopsyReport autopsyReport;
        RenderingEntity autopsyReportVisual; // Cube placeholder for the report

        // Interrogation characters (spawned during rounds)
        LoungeCharacterData interrogationCharacter1;
        LoungeCharacterData interrogationCharacter2;

        // Character Profile Manager
        private CharacterProfileManager profileManager;

        public TheLoungeScene() : base()
        {
            // Initialize character system and physics
            characters = null; // Will be initialized in physics system
            physicsSystem = new PhysicsSystem(ref characters);

            // Initialize interaction system
            interactionSystem = new InteractionSystem(physicsSystem);

            // Initialize managers
            bartender = new LoungeCharacterData("Bartender Zix");
            pathologist = new LoungeCharacterData("Dr. Harmon Kerrigan");
            debugVisualizer = new LoungeDebugVisualizer();
            uiManager = new LoungeUIManager();
            starfield = new LoungeStarfield();
            starfieldSphere = new LoungeStarfieldSphere();

            // Initialize finale system
            finaleIntro = new FinaleIntroSequence();
            finaleManager = new FinaleManager();
            finaleUI = new FinaleUI();
            finaleEndingScreen = new FinaleEndingScreen();

            // Initialize mesh loader
            meshLoader = new LoungeSceneMeshLoader(LevelScale, physicsSystem, entity => AddRenderingEntity(entity));

            // Set black background for lounge scene
            BackgroundColor = Color.Black;
            Globals.screenManager.IsMouseVisible = false;
            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();

            // Initialize debug rendering (off by default)
            if (boundingBoxRenderer != null)
            {
                boundingBoxRenderer.ShowBoundingBoxes = false;
            }

            // Initialize Character Profile Manager
            profileManager = new CharacterProfileManager();

            // Initialize UI Manager (loads portraits)
            uiManager.Initialize();

            // Initialize finale system
            finaleIntro.Initialize();
            finaleUI.Initialize();
            finaleEndingScreen.Initialize();

            // Wire up finale event handlers
            finaleIntro.OnSequenceComplete += OnFinaleIntroComplete;
            finaleUI.OnAnswerSelected += OnFinaleAnswerSelected;
            finaleEndingScreen.OnRestartRequested += OnFinaleRestartRequested;
            finaleEndingScreen.OnQuitRequested += OnFinaleQuitRequested;

            // Create first person character at starting position
            CreateCharacter(new Vector3(0, 5f, 0), Quaternion.Identity);

            // Load all lounge geometry and furniture via mesh loader
            meshLoader.LoadAllLoungeGeometry();
            meshLoader.LoadFurniture();

            // Initialize Odysseus ship for finale intro sequence
            InitializeOdysseusShip();

            // Create point light at center of scene, 20 units from ground
            centerLight = new PointLight("CenterLight")
            {
                Position = new Vector3(0, 20, 0),
                Color = Color.White,
                Range = 50.0f,
                Intensity = 1.5f,
                IsEnabled = true
            };

       

            Console.WriteLine("========================================\n");

            // Create bartender character
            InitializeBartender();

            // Initialize evidence table system
            InitializeEvidenceTable();

            // Create evidence vial item
            InitializeEvidenceVial();

            // Create suspects file on table
            InitializeSuspectsFile();

            // Create autopsy report on table
            InitializeAutopsyReport();

            // Create additional evidence items
            InitializeAdditionalEvidenceItems();

            // Pathologist will be spawned after bartender dialogue
            // Don't initialize pathologist here - will be called from TheLoungeScreen after bartender dialogue
        }

        /// <summary>
        /// Load character profiles from YAML data
        /// Called from TheLoungeScreen after character data is loaded
        /// </summary>
        public void LoadCharacterProfiles(anakinsoft.game.scenes.lounge.characters.LoungeCharactersData yamlData)
        {
            if (profileManager == null)
            {
                Console.WriteLine("[TheLoungeScene] ERROR: ProfileManager not initialized");
                return;
            }

            try
            {
                profileManager.LoadFromYaml(yamlData);
                profileManager.LoadPortraits();
                Console.WriteLine($"[TheLoungeScene] Loaded {profileManager.ProfileCount} character profiles");

                // Pass profile manager to UI manager for portrait integration
                uiManager.SetProfileManager(profileManager);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TheLoungeScene] ERROR loading character profiles: {ex.Message}");
            }
        }

        private void InitializeBartender()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("LOADING BARTENDER CHARACTER");
            Console.WriteLine("========================================");

            // Position bartender where the alien was (at the bar)
            Vector3 bartenderPosition = new Vector3(25, 0, -25);

            // Camera interaction position - pulled back and looking directly at bartender
            Vector3 cameraInteractionPosition = new Vector3(25, 20, -5); // Same Z, pulled back on X
            Vector3 cameraLookAt =  new Vector3(0, 0, -10); // Look at bartender's head height

            // Create bartender interactable
            bartender.Interaction = new InteractableCharacter("Bartender", bartenderPosition,
                cameraInteractionPosition, cameraLookAt);

            // Register with interaction system
            interactionSystem.RegisterInteractable(bartender.Interaction);

            // Create physics collider for bartender (invisible box for interaction raycasting)
            CreateBartenderCollider(bartenderPosition);

            Console.WriteLine($"Bartender created at position: {bartenderPosition}");
            Console.WriteLine($"Interaction camera position: {cameraInteractionPosition}");

            // TODO: Add bartender model rendering (using alien model for now as placeholder)
            var bartenderMaterial = new UnlitSkinnedMaterial("textures/prototype/grass", "shaders/surface/SkinnedVertexLit", useDefault: false);
            bartenderMaterial.AmbientColor = new Vector3(0.1f, 0.1f, 0.2f);
            bartenderMaterial.EmissiveColor = new Vector3(0.1f, 0.1f, 0.2f);
            bartenderMaterial.LightDirection = Vector3.Normalize(new Vector3(0.3f, -1, -0.5f));
            bartenderMaterial.LightColor = new Vector3(1.0f, 0.95f, 0.9f);
            bartenderMaterial.LightIntensity = 0.9f;

            bartender.Model = new SkinnedRenderingEntity("models/characters/alien", bartenderMaterial);
            bartender.Model.Position = bartenderPosition;
            bartender.Model.Scale = Vector3.One * 0.25f * LevelScale;
            bartender.Model.Rotation = Quaternion.Identity; // Face forward (was already facing the right way)
            bartender.Model.IsVisible = true;

            // Play idle animation
            if (bartender.Model is SkinnedRenderingEntity skinnedBartender)
            {
                var skinData = skinnedBartender.GetSkinningData();
                if (skinData != null && skinData.AnimationClips.Count > 0)
                {
                    var firstClipName = skinData.AnimationClips.Keys.First();
                    skinnedBartender.PlayAnimation(firstClipName, loop: true);
                }
            }

            AddRenderingEntity(bartender.Model);

            Console.WriteLine("========================================\n");
        }

        public void SpawnPathologist()
        {
            if (pathologist.IsSpawned)
            {
                Console.WriteLine("Pathologist already spawned, skipping...");
                return;
            }

            InitializePathologist();
        }

        private void InitializePathologist()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("LOADING PATHOLOGIST CHARACTER (DR. HARMON KERRIGAN)");
            Console.WriteLine("========================================");

            // Position pathologist at table (left side of lounge)
            Vector3 pathologistPosition = new Vector3(-9f, 0, 28f);
            Vector3 cameraInteractionPosition = pathologistPosition + new Vector3(-16, 15, 0);
            Vector3 cameraLookAt = new Vector3(10, 0, 0);

            var config = new CharacterSpawnConfig
            {
                Name = "Dr. Harmon Kerrigan",
                Position = pathologistPosition,
                CameraPosition = cameraInteractionPosition,
                CameraLookAt = cameraLookAt,
                ModelPath = "models/characters/alien-2",
                TexturePath = "textures/prototype/grass",
                Scale = 0.25f,
                RotationYaw = -90f,
                ColliderWidth = 15f * LevelScale,
                ColliderHeight = 30f * LevelScale,
                ColliderDepth = 15f * LevelScale
            };

            pathologist = SpawnCharacter(config);

            Console.WriteLine("========================================\n");
        }

        private void CreateBartenderCollider(Vector3 position)
        {
            // Create a box collider for the bartender (4x taller, bottom at floor level)
            bartender.ColliderWidth = 10f * LevelScale;
            bartender.ColliderHeight = 48f * LevelScale; // 60% of original 80f height
            bartender.ColliderDepth = 10f * LevelScale;

            var boxShape = new Box(bartender.ColliderWidth, bartender.ColliderHeight, bartender.ColliderDepth);
            var shapeIndex = physicsSystem.Simulation.Shapes.Add(boxShape);

            // Move the collider up so the bottom touches the floor (half of height up from position)
            bartender.ColliderCenter = position + new Vector3(0, bartender.ColliderHeight / 2f, 0);

            // Create static body at adjusted position
            var staticHandle = physicsSystem.Simulation.Statics.Add(new StaticDescription(
                bartender.ColliderCenter.ToVector3N(),
                Quaternion.Identity.ToQuaternionN(),
                shapeIndex));

            // Store the static handle in the bartender for interaction detection
            bartender.Interaction.SetStaticHandle(staticHandle);

            Console.WriteLine($"Created bartender physics collider at {bartender.ColliderCenter} (size: {bartender.ColliderWidth}x{bartender.ColliderHeight}x{bartender.ColliderDepth})");
        }

        /// <summary>
        /// Generic character spawning method - creates interaction, collider, and visual model
        /// </summary>
        private LoungeCharacterData SpawnCharacter(CharacterSpawnConfig config)
        {
            var character = new LoungeCharacterData(config.Name);

            // Create interaction
            character.Interaction = new InteractableCharacter(
                config.Name,
                config.Position,
                config.CameraPosition,
                config.CameraLookAt
            );

            // Register with interaction system
            interactionSystem.RegisterInteractable(character.Interaction);

            // Create physics collider
            character.ColliderWidth = config.ColliderWidth;
            character.ColliderHeight = config.ColliderHeight;
            character.ColliderDepth = config.ColliderDepth;

            var boxShape = new Box(character.ColliderWidth, character.ColliderHeight, character.ColliderDepth);
            var shapeIndex = physicsSystem.Simulation.Shapes.Add(boxShape);

            // Move the collider up so the bottom touches the floor
            character.ColliderCenter = config.Position + new Vector3(5, character.ColliderHeight / 2f, 0);

            var staticHandle = physicsSystem.Simulation.Statics.Add(new StaticDescription(
                character.ColliderCenter.ToVector3N(),
                Quaternion.Identity.ToQuaternionN(),
                shapeIndex));

            character.Interaction.SetStaticHandle(staticHandle);

            // Create visual model
            Console.WriteLine($"[SpawnCharacter] Creating model: {config.ModelPath}, texture: {config.TexturePath}, scale: {config.Scale}");

            var material = new UnlitSkinnedMaterial(config.TexturePath, "shaders/surface/SkinnedVertexLit", useDefault: false);
            material.AmbientColor = config.AmbientColor;
            material.EmissiveColor = config.EmissiveColor;
            material.LightDirection = Vector3.Normalize(config.LightDirection);
            material.LightColor = config.LightColor;
            material.LightIntensity = config.LightIntensity;

            character.Model = new SkinnedRenderingEntity(config.ModelPath, material);
            character.Model.Position = config.Position;
            character.Model.Scale = Vector3.One * config.Scale * LevelScale;
            character.Model.Rotation = QuaternionExtensions.CreateFromYawPitchRollDegrees(
                config.RotationYaw,
                config.RotationPitch,
                config.RotationRoll
            );
            character.Model.IsVisible = true;

            Console.WriteLine($"[SpawnCharacter] Model created at {character.Model.Position}, scale: {character.Model.Scale}, visible: {character.Model.IsVisible}");

            // Play idle animation
            if (character.Model is SkinnedRenderingEntity skinnedChar)
            {
                var skinData = skinnedChar.GetSkinningData();
                if (skinData != null && skinData.AnimationClips.Count > 0)
                {
                    var firstClipName = skinData.AnimationClips.Keys.First();
                    skinnedChar.PlayAnimation(firstClipName, loop: true);
                }
            }

            AddRenderingEntity(character.Model);

            Console.WriteLine($"[SpawnCharacter] SUCCESS: {config.Name} spawned");
            Console.WriteLine($"  Position: {character.Model.Position}");
            Console.WriteLine($"  Scale: {character.Model.Scale}");
            Console.WriteLine($"  Visible: {character.Model.IsVisible}");
            Console.WriteLine($"  Collider: {character.ColliderWidth}x{character.ColliderHeight}x{character.ColliderDepth}");

            return character;
        }

        /// <summary>
        /// Despawn a character - removes interaction, physics, and visual model
        /// </summary>
        private void DespawnCharacter(LoungeCharacterData character)
        {
            if (character == null) return;

            if (character.Interaction != null)
            {
                interactionSystem.UnregisterInteractable(character.Interaction);

                var handle = character.Interaction.GetStaticHandle();
                if (handle.HasValue)
                {
                    physicsSystem.Simulation.Statics.Remove(handle.Value);
                }
            }

            if (character.Model != null)
            {
                RemoveRenderingEntity(character.Model);
                character.Model = null;
            }

            Console.WriteLine($"[DespawnCharacter] Despawned {character.Name}");
        }

        private void InitializeEvidenceTable()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("INITIALIZING EVIDENCE TABLE");
            Console.WriteLine("========================================");

            // Define table area near pathologist's location
            // Table center positioned on the left side of the lounge
            Vector3 tableCenter = new Vector3(-4f, 12.8f, -25f); // Same general area as bar table 2
            Vector3 tableSize = new Vector3(12f, 2f, 20f); // 20 units wide, 15 units deep

            // Create 3x3 grid (9 slots for evidence items)
            // This gives us organized positions for:
            // - Suspects file (center)
            // - Evidence items (surrounding slots)
            // - Future collectibles
            evidenceTable = new EvidenceTable(tableCenter, tableSize, 3, 3);

            // Initialize evidence inventory
            evidenceInventory = new EvidenceInventory();

            Console.WriteLine($"Evidence table created at {tableCenter}");
            Console.WriteLine($"Table size: {tableSize.X}x{tableSize.Z}, Grid: 3x3 (9 slots)");
            Console.WriteLine("========================================\n");
        }

        private void InitializeEvidenceVial()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("CREATING EVIDENCE VIAL ITEM");
            Console.WriteLine("========================================");

            // Position vial on the bar counter near the bartender
            Vector3 vialPosition = new Vector3(20, 12, -25); // On bar counter

            // Camera interaction position - look at the vial
            Vector3 cameraInteractionPosition = new Vector3(20, 15, -10);
            Vector3 cameraLookAt = vialPosition;

            // Create inventory item data
            var vialItem = new InventoryItem(
                id: "evidence_vial",
                name: "Evidence Vial",
                description: "A small vial containing trace amounts of breturium - the substance used to kill the ambassador."
            );

            // Create interactable item
            evidenceVial = new InteractableItem(
                "Evidence Vial",
                vialPosition,
                cameraInteractionPosition,
                cameraLookAt,
                vialItem
            );

            // Register with interaction system
            interactionSystem.RegisterInteractable(evidenceVial);

            Console.WriteLine($"Evidence vial created at position: {vialPosition}");
            Console.WriteLine("========================================\n");
        }

        private void InitializeSuspectsFile()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("CREATING SUSPECTS FILE");
            Console.WriteLine("========================================");

            // Place suspects file in center slot of evidence table (slot [1,1])
            // Grid layout (3x3):
            // [0,0] [0,1] [0,2]
            // [1,0] [1,1] [1,2]  <- Center slot [1,1] for suspects file
            // [2,0] [2,1] [2,2]
            Vector3 filePosition = evidenceTable.GetSlotPosition(1, 1); // Center of 3x3 grid

            // Create suspects file interactable
            Vector3 fileSize = new Vector3(8f, 1f, 8f) * LevelScale; // Match visual size
            suspectsFile = new SuspectsFile(
                "Suspects File",
                filePosition,
                fileSize
            );

            // Disable file initially - will be enabled after talking to pathologist
            suspectsFile.CanInteract = false;

            // Register file with evidence table
            evidenceTable.PlaceItem("suspects_file", 1, 1, suspectsFile);

            // Initialize with all suspect entries (empty transcripts until interviewed)
            suspectsFile.AddTranscript("Bartender Zix", "Not yet interviewed.", false);
            suspectsFile.AddTranscript("Dr. Harmon Kerrigan", "Chief Medical Officer - performed the autopsy.", false);
            suspectsFile.AddTranscript("Commander Sylar Von", "Not yet interviewed.", false);
            suspectsFile.AddTranscript("Lt. Marcus Webb", "Not yet interviewed.", false);
            suspectsFile.AddTranscript("Ensign Tork", "Not yet interviewed.", false);
            suspectsFile.AddTranscript("Chief Kala Solis", "Not yet interviewed.", false);
            suspectsFile.AddTranscript("Maven Kilroth", "Not yet interviewed.", false);
            suspectsFile.AddTranscript("Tehvora", "Not yet interviewed.", false);
            suspectsFile.AddTranscript("Lucky Chen", "Not yet interviewed.", false);

            // Register with interaction system
            interactionSystem.RegisterInteractable(suspectsFile);

            // Create physics collider for the file (so it can be detected by raycasting)
            var fileColliderSize = fileSize * 1.2f; // Slightly larger for easier interaction
            var fileShape = new Box(
                fileColliderSize.X,
                fileColliderSize.Y,
                fileColliderSize.Z
            );
            var fileStaticHandle = physicsSystem.Simulation.Statics.Add(
                new StaticDescription(
                    filePosition.ToVector3N(),
                    QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0).ToQuaternionN(),
                    physicsSystem.Simulation.Shapes.Add(fileShape)
                )
            );

            // Store the static handle in the suspects file for interaction detection
            suspectsFile.SetStaticHandle(fileStaticHandle);

            // Create visual placeholder (cube) - 90% smaller for a small file/tablet appearance
            var fileMaterial = new UnlitMaterial("textures/prototype/concrete");
            suspectsFileVisual = new RenderingEntity("models/cube", "textures/prototype/concrete");
            suspectsFileVisual.Position = filePosition;
            suspectsFileVisual.Scale = new Vector3(0.3f, 0.05f, 0.2f) * LevelScale; // 90% smaller - small tablet/file size
            suspectsFileVisual.Rotation = QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0);
            suspectsFileVisual.IsVisible = true;

            AddRenderingEntity(suspectsFileVisual);

            Console.WriteLine($"Suspects file created at position: {filePosition}");
            Console.WriteLine($"Suspects file physics collider created with handle: {fileStaticHandle.Value}");
            Console.WriteLine("========================================\n");
        }

        private void InitializeAutopsyReport()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("CREATING AUTOPSY REPORT");
            Console.WriteLine("========================================");

            // Place autopsy report in slot [1,0] (left of center)
            // Grid layout (3x3):
            // [0,0] [0,1] [0,2]
            // [1,0] [1,1] [1,2]  <- Slot [1,0] for autopsy report, [1,1] for suspects file
            // [2,0] [2,1] [2,2]
            Vector3 reportPosition = evidenceTable.GetSlotPosition(1, 0);

            // Create autopsy report interactable
            Vector3 reportSize = new Vector3(10f, 1f, 10f) * LevelScale;
            autopsyReport = new AutopsyReport(
                "Autopsy Report",
                reportPosition,
                reportSize
            );

            // Disable report initially - will be enabled after talking to pathologist
            autopsyReport.CanInteract = false;

            // Register report with evidence table
            evidenceTable.PlaceItem("autopsy_report", 1, 0, autopsyReport);

            // Set initial content
            autopsyReport.ReportContent = "Dr. Harmon Kerrigan's preliminary autopsy findings...";

            // Register with interaction system
            interactionSystem.RegisterInteractable(autopsyReport);

            // Create physics collider for the report (so it can be detected by raycasting)
            var reportColliderSize = reportSize * 1.2f;
            var reportShape = new Box(
                reportColliderSize.X,
                reportColliderSize.Y,
                reportColliderSize.Z
            );
            var reportStaticHandle = physicsSystem.Simulation.Statics.Add(
                new StaticDescription(
                    reportPosition.ToVector3N(),
                    QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0).ToQuaternionN(),
                    physicsSystem.Simulation.Shapes.Add(reportShape)
                )
            );

            // Store the static handle in the autopsy report for interaction detection
            autopsyReport.SetStaticHandle(reportStaticHandle);

            // Create visual placeholder (cube)
            autopsyReportVisual = new RenderingEntity("models/cube", "textures/prototype/concrete");
            autopsyReportVisual.Position = reportPosition;
            autopsyReportVisual.Scale = new Vector3(0.3f, 0.05f, 0.2f) * LevelScale;
            autopsyReportVisual.Rotation = QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0);
            autopsyReportVisual.IsVisible = true;

            AddRenderingEntity(autopsyReportVisual);

            // Link visual to autopsy report for showing/hiding
            autopsyReport.SetVisual(autopsyReportVisual);

            Console.WriteLine($"Autopsy report created at position: {reportPosition}");
            Console.WriteLine($"Autopsy report physics collider created with handle: {reportStaticHandle.Value}");
            Console.WriteLine("========================================\n");
        }

        private void InitializeAdditionalEvidenceItems()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("CREATING ADDITIONAL EVIDENCE ITEMS");
            Console.WriteLine("========================================");

            // Load evidence data from YAML
            string yamlPath = Path.Combine("Content", "Data", "Lounge", "evidence.yml");
            var evidenceData = EvidenceDataLoader.LoadEvidence(yamlPath);

            if (evidenceData == null || evidenceData.evidence == null || evidenceData.evidence.Count == 0)
            {
                Console.WriteLine("[TheLoungeScene] ERROR: Failed to load evidence data from YAML");
                return;
            }

            // Create factory for evidence documents
            var factory = new EvidenceDocumentFactory(physicsSystem, interactionSystem, evidenceTable, LevelScale);

            // Grid layout (3x3):
            // [0,0] [0,1] [0,2]
            // [1,0] [1,1] [1,2]  <- [1,0] autopsy report, [1,1] suspects file
            // [2,0] [2,1] [2,2]

            // Initialize evidence documents list
            evidenceDocuments = new List<EvidenceDocument>();

            // Create all evidence documents from YAML data
            foreach (var evidenceConfig in evidenceData.evidence)
            {
                var (doc, vis) = factory.CreateEvidenceDocument(
                    evidenceConfig.name,
                    evidenceConfig.description,
                    evidenceConfig.id,
                    evidenceConfig.table_row,
                    evidenceConfig.table_column,
                    evidenceConfig.visual_scale.ToVector3(),
                    evidenceConfig.texture
                );

                // Add to list for iteration
                evidenceDocuments.Add(doc);
                AddRenderingEntity(vis);

                // Store specific references for backwards compatibility
                switch (evidenceConfig.id)
                {
                    case "security_log":
                        securityLog = doc;
                        break;
                    case "datapad":
                        datapad = doc;
                        break;
                    case "keycard":
                        keycard = doc;
                        break;
                    case "dna_evidence":
                        dnaEvidence = doc;
                        break;
                    case "access_codes":
                        accessLog = doc;
                        break;
                    case "medical_training":
                        medicalRecord = doc;
                        break;
                    case "breturium_sample":
                        breturiumSample = doc;
                        break;
                }

                Console.WriteLine($"[TheLoungeScene] Created evidence: {evidenceConfig.name} ({evidenceConfig.id})");
            }

            // Hook up event handlers for all evidence documents
            foreach (var doc in evidenceDocuments)
            {
                doc.OnDocumentExamined += OnEvidenceDocumentExamined;
            }

            Console.WriteLine($"[TheLoungeScene] Loaded {evidenceDocuments.Count} evidence items from YAML");
            Console.WriteLine("========================================\n");
        }

        private void OnEvidenceDocumentExamined(EvidenceDocument document)
        {
            // Check if this document is already in inventory (player wants to drop it)
            if (evidenceInventory.HasDocument && evidenceInventory.CurrentDocument == document)
            {
                // Drop the document
                evidenceInventory.DropDocument();
                Console.WriteLine($"[TheLoungeScene] Evidence document dropped: {document.Name}");
            }
            else
            {
                // Pick up document (automatically swaps out current item if holding one)
                evidenceInventory.PickUpDocument(document);
                Console.WriteLine($"[TheLoungeScene] Evidence document examined and added to inventory: {document.Name}");
            }
        }

        /// <summary>
        /// Initialize Odysseus ship model for finale intro sequence
        /// Ship starts far away and moves to window during finale
        /// </summary>
        private void InitializeOdysseusShip()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("CREATING ODYSSEUS SHIP");
            Console.WriteLine("========================================");

            // Create ship entity with emissive shader for glowing windows
            odysseusShip = new RenderingEntity(
                "models/odysseus_ship",
                "textures/odysseus/odysseus_ship",
                "shaders/surface/EmissiveLit" // Use emissive shader for glowing windows
            );
            odysseusShip.Scale = Vector3.One * 2.0f; // Scale up the ship
            odysseusShip.IsVisible = false; // Hidden until finale intro starts

            // Load and apply emissive texture for glowing windows
            var emissiveTexture = Globals.screenManager.Content.Load<Texture2D>("textures/odysseus/odysseus_ship-expor_emt");

            // Apply emissive map to the ship's model
            foreach (var mesh in odysseusShip.Model.Meshes)
            {
                foreach (var part in mesh.MeshParts)
                {
                    if (part.Effect != null && part.Effect.Parameters["EmissiveMap"] != null)
                    {
                        part.Effect.Parameters["EmissiveMap"].SetValue(emissiveTexture);
                        part.Effect.Parameters["UseEmissiveMap"]?.SetValue(true);
                    }
                }
            }

            // Start at far position (will be moved during intro sequence)
            odysseusShip.Position = finaleIntro.ShipPosition;

            AddRenderingEntity(odysseusShip);

            Console.WriteLine("[TheLoungeScene] Odysseus ship created with emissive glow");
            Console.WriteLine("========================================\n");
        }

        void CreateCharacter(Vector3 position, Quaternion rotation)
        {
            characterActive = true;
            character = new CharacterInput(characters, position.ToVector3N(),
                new Capsule(0.5f * 10 * LevelScale, 1 * 10 * LevelScale),
                minimumSpeculativeMargin: 1f * LevelScale,
                mass: 0.1f,
                maximumHorizontalForce: 100 * LevelScale,
                maximumVerticalGlueForce: 1500 * LevelScale,
                jumpVelocity: 0,
                speed: 80 * LevelScale,
                maximumSlope: 40f.ToRadians());

            // Store the initial rotation for the camera
            characterInitialRotation = rotation;
        }

        /// <summary>
        /// Reset player character to initial starting position and rotation
        /// </summary>
        public void ResetPlayerPosition()
        {
            if (character == null) return;

            var initialPosition = new Vector3(0, 5f, 0);
            var bodyReference = characters.Simulation.Bodies.GetBodyReference(character.Value.BodyHandle);
            bodyReference.Pose.Position = initialPosition.ToVector3N();
            bodyReference.Pose.Orientation = BepuUtilities.QuaternionEx.Identity;
            bodyReference.Velocity.Linear = System.Numerics.Vector3.Zero;
            bodyReference.Velocity.Angular = System.Numerics.Vector3.Zero;

            Console.WriteLine($"[TheLoungeScene] Reset player to position: {initialPosition}");
        }

        public override void Update(GameTime gameTime)
        {
            // Update load timer first
            timeSinceLoad += (float)gameTime.ElapsedGameTime.TotalSeconds;
            uiManager.UpdateLoadTimer(gameTime);

            // During load delay, don't update base (which updates physics)
            // This prevents character from falling through floor while level loads
            if (timeSinceLoad <= characterLoadDelay)
            {
                // Skip physics update during load delay
                // Just update rendering entities
                foreach (var entity in renderingEntities)
                {
                    entity.Update(gameTime);
                }
            }
            else
            {
                // After load delay, update normally (includes physics)
                base.Update(gameTime);
            }

            // Apply global mouse visibility state
            Globals.screenManager.IsMouseVisible = Globals.shouldShowMouse;

            // Only handle input and movement if not in menu
            if (!Globals.IsInMenuState())
            {
                // Handle input
                HandleInput();
            }
        }

        public void UpdateWithCamera(GameTime gameTime, Camera camera, bool isDialogueActive = false, bool isCameraTransitionActive = false)
        {
            // Update the scene normally first
            Update(gameTime);

            // Update intro text via UI Manager
            uiManager.UpdateIntroText(gameTime);

            // Only update character movement and interactions if not in menu
            if (!Globals.IsInMenuState())
            {
                // Update interaction system with camera for raycasting (skip during dialogue OR camera transition)
                if (!isDialogueActive && !isCameraTransitionActive)
                {
                    interactionSystem?.Update(gameTime, camera);

                    // Track hovered character for portrait display
                    if (interactionSystem?.CurrentTarget is InteractableCharacter hoveredChar)
                    {
                        // Map the character to their portrait key
                        if (hoveredChar == bartender.Interaction)
                            uiManager.SetHoveredCharacter("NPC_Bartender");
                        else if (pathologist.IsSpawned && hoveredChar == pathologist.Interaction)
                            uiManager.SetHoveredCharacter("DrHarmon");
                        else if (interrogationCharacter1 != null && interrogationCharacter1.IsSpawned && hoveredChar == interrogationCharacter1.Interaction)
                            uiManager.SetHoveredCharacter(interrogationCharacter1.PortraitKey);
                        else if (interrogationCharacter2 != null && interrogationCharacter2.IsSpawned && hoveredChar == interrogationCharacter2.Interaction)
                            uiManager.SetHoveredCharacter(interrogationCharacter2.PortraitKey);
                        else
                            uiManager.SetHoveredCharacter(null);
                    }
                    else
                    {
                        uiManager.SetHoveredCharacter(null);
                    }
                }

                // Only allow character movement after intro and after load delay
                if (!uiManager.ShowIntroText && characterActive && character.HasValue && timeSinceLoad > characterLoadDelay)
                {
                    character.Value.UpdateCharacterGoals(Keyboard.GetState(), camera, (float)gameTime.ElapsedGameTime.TotalSeconds);
                }
            }

            // Update finale intro sequence
            if (finaleIntro.IsActive)
            {
                finaleIntro.Update(gameTime);

                // Update ship position and visibility during finale intro
                if (odysseusShip != null)
                {
                    odysseusShip.Position = finaleIntro.ShipPosition;
                    odysseusShip.IsVisible = finaleIntro.IsShipVisible;
                }
            }

            // Update finale UI (question buttons)
            if (finaleUI.IsActive)
            {
                finaleUI.Update(gameTime);
            }

            // Update finale ending screen (restart/quit buttons)
            if (finaleEndingScreen.IsActive)
            {
                finaleEndingScreen.Update(gameTime);
            }

            // Update starfield with speed multiplier from finale intro
            float starfieldSpeed = finaleIntro.IsActive ? finaleIntro.StarfieldSpeedMultiplier : 1.0f;
            starfield.Update(gameTime, starfieldSpeed);
            starfieldSphere.Update(gameTime);
        }

        private void HandleInput()
        {
            var keyboard = Keyboard.GetState();

            // Toggle physics wireframe rendering with L key
            if (keyboard.IsKeyDown(Keys.L) && !previousKeyboard.IsKeyDown(Keys.L))
            {
                ShowPhysicsWireframe = !ShowPhysicsWireframe;
                Console.WriteLine($"Physics wireframe debug rendering: {(ShowPhysicsWireframe ? "ON" : "OFF")}");
            }

            previousKeyboard = keyboard;
        }

        public override void Draw(GameTime gameTime, Camera camera)
        {
            // Apply starfield shader parameters from scene settings
            starfieldSphere.StarDensity = StarDensity;
            starfieldSphere.StarBrightness = StarBrightness;
            starfieldSphere.StarSize = StarSize;
            starfieldSphere.NebulaBrightness = NebulaBrightness;

            // Draw shader-based starfield sphere first (far background)
            starfieldSphere.Draw(camera);

            // Draw particle starfield on top (closer to camera)
            starfield.Draw(camera);

            // Draw all entities using the base scene drawing
            base.Draw(gameTime, camera);

            // Draw suspects file bounding box when targeted (always visible for gameplay)
            if (suspectsFile != null)
            {
                debugVisualizer.DrawSuspectsFileBox(suspectsFile, camera);
            }

            // Draw autopsy report bounding box when targeted (always visible for gameplay, unless collected)
            if (autopsyReport != null && !autopsyReport.IsCollected)
            {
                debugVisualizer.DrawAutopsyReportBox(autopsyReport, camera);
            }

            // Draw evidence documents bounding boxes (always visible for gameplay)
            if (evidenceDocuments != null)
            {
                foreach (var document in evidenceDocuments)
                {
                    if (document != null)
                    {
                        debugVisualizer.DrawEvidenceDocumentBox(document, camera);
                    }
                }
            }

            // Draw wireframe visualization of static mesh collision geometry if debug mode enabled
            if (ShowPhysicsWireframe)
            {
                // Draw static mesh wireframes
                var (_, meshVerts, meshTransforms) = meshLoader.GetPhysicsMeshData();
                debugVisualizer.DrawStaticMeshWireframes(meshVerts, meshTransforms, camera);

                // Draw bartender collider box
                debugVisualizer.DrawCharacterCollider(bartender, camera, interactionSystem);

                // Draw pathologist collider box (only if spawned)
                if (pathologist.IsSpawned)
                {
                    debugVisualizer.DrawCharacterCollider(pathologist, camera, interactionSystem);
                }

                // Draw interrogation character 1 collider box (if spawned)
                if (interrogationCharacter1 != null && interrogationCharacter1.IsSpawned)
                {
                    debugVisualizer.DrawCharacterCollider(interrogationCharacter1, camera, interactionSystem);
                }

                // Draw interrogation character 2 collider box (if spawned)
                if (interrogationCharacter2 != null && interrogationCharacter2.IsSpawned)
                {
                    debugVisualizer.DrawCharacterCollider(interrogationCharacter2, camera, interactionSystem);
                }

                // Draw interaction camera positions
                if (bartender.Interaction != null)
                {
                    debugVisualizer.DrawInteractionDebugVisualization(bartender.Interaction, evidenceTable, camera);
                }

                // Draw pathologist interaction camera positions (only if spawned)
                if (pathologist.IsSpawned && pathologist.Interaction != null)
                {
                    debugVisualizer.DrawInteractionDebugVisualization(pathologist.Interaction, evidenceTable, camera);
                }

                // Draw interrogation characters interaction camera positions (if spawned)
                if (interrogationCharacter1 != null && interrogationCharacter1.IsSpawned && interrogationCharacter1.Interaction != null)
                {
                    debugVisualizer.DrawInteractionDebugVisualization(interrogationCharacter1.Interaction, evidenceTable, camera);
                }

                if (interrogationCharacter2 != null && interrogationCharacter2.IsSpawned && interrogationCharacter2.Interaction != null)
                {
                    debugVisualizer.DrawInteractionDebugVisualization(interrogationCharacter2.Interaction, evidenceTable, camera);
                }
            }
        }

        /// <summary>
        /// Draws the UI elements
        /// </summary>
        public void DrawUI(GameTime gameTime, Camera camera, SpriteBatch spriteBatch, bool isDialogueActive = false)
        {
            var font = Globals.fontNTR;
            uiManager.DrawUI(gameTime, spriteBatch, font, interactionSystem, isDialogueActive);

            // Draw finale intro overlay (fade, text) on top of everything
            if (finaleIntro.IsActive)
            {
                finaleIntro.DrawUI(spriteBatch);
            }

            // Draw finale question UI
            if (finaleUI.IsActive)
            {
                finaleUI.Draw(spriteBatch, finaleManager.CurrentQuestionNumber, finaleManager.TotalQuestions);
            }

            // Draw finale ending screen
            if (finaleEndingScreen.IsActive)
            {
                finaleEndingScreen.Draw(spriteBatch);
            }
        }




        // Utility methods for external access
        public bool IsCharacterActive() => characterActive;
        public CharacterInput? GetCharacter() => character;
        public Quaternion GetCharacterInitialRotation() => characterInitialRotation;
        public InteractableCharacter GetBartender() => bartender.Interaction;
        public InteractableCharacter GetPathologist() => pathologist.Interaction;
        public InteractableItem GetEvidenceVial() => evidenceVial;
        public EvidenceDocument GetSecurityLog() => securityLog;
        public EvidenceDocument GetDatapad() => datapad;
        public EvidenceDocument GetKeycard() => keycard;
        public EvidenceDocument GetDNAEvidence() => dnaEvidence;
        public EvidenceDocument GetAccessLog() => accessLog;
        public EvidenceDocument GetMedicalRecord() => medicalRecord;
        public EvidenceDocument GetBreturiumSample() => breturiumSample;
        public SuspectsFile GetSuspectsFile() => suspectsFile;
        public AutopsyReport GetAutopsyReport() => autopsyReport;
        public EvidenceTable GetEvidenceTable() => evidenceTable;
        public InteractionSystem GetInteractionSystem() => interactionSystem;
        public EvidenceInventory GetEvidenceInventory() => evidenceInventory;
        public bool IsShowingIntroText() => uiManager.ShowIntroText;
        public Dictionary<string, Texture2D> GetCharacterPortraits() => uiManager.CharacterPortraits;
        public CharacterProfileManager GetProfileManager() => profileManager;

        public void SetActiveDialogueCharacter(string characterKey)
        {
            uiManager.SetActiveDialogueCharacter(characterKey);
        }

        /// <summary>
        /// Start the finale intro sequence (fade, text, ship arrival, starfield slowdown)
        /// Call this when investigation time reaches 0 hours
        /// </summary>
        public void StartFinaleIntro()
        {
            finaleIntro.Start();
            Console.WriteLine("[TheLoungeScene] Finale intro sequence started");
        }

        // Finale event handlers
        private void OnFinaleIntroComplete()
        {
            Console.WriteLine("[TheLoungeScene] Finale intro complete, starting questions");
            finaleManager.StartFinale();
            finaleUI.Show(finaleManager.CurrentQuestion);
        }

        private void OnFinaleAnswerSelected(int answerIndex)
        {
            Console.WriteLine("[TheLoungeScene] Answer selected: {0}", answerIndex);
            bool continueQuestions = finaleManager.SubmitAnswer(answerIndex);

            if (continueQuestions && !finaleManager.IsFinaleCompleted)
            {
                // Show next question
                finaleUI.Show(finaleManager.CurrentQuestion);
            }
            else
            {
                // Show ending screen (success or failure)
                finaleUI.Hide();
                finaleEndingScreen.Show(finaleManager.Results);
            }
        }

        private void OnFinaleRestartRequested()
        {
            Console.WriteLine("[TheLoungeScene] Finale restart requested");
            // Reset finale state
            finaleEndingScreen.Hide();
            finaleManager.Reset();
            finaleIntro.Reset();
            // Could reload scene or return to investigation
        }

        private void OnFinaleQuitRequested()
        {
            Console.WriteLine("[TheLoungeScene] Finale quit requested");
            // Return to main menu or quit game
            finaleEndingScreen.Hide();
        }

        public void HideAutopsyReportVisual()
        {
            if (autopsyReportVisual != null)
            {
                autopsyReportVisual.IsVisible = false;
                Console.WriteLine("[TheLoungeScene] Autopsy report visual hidden");
            }
        }

        public void ClearActiveDialogueCharacter()
        {
            uiManager.ClearActiveDialogueCharacter();
        }

        /// <summary>
        /// Enable all evidence documents for interaction (called when round 1 starts)
        /// </summary>
        public void EnableAllEvidenceDocuments()
        {
            if (evidenceDocuments != null)
            {
                foreach (var doc in evidenceDocuments)
                {
                    doc.CanInteract = true;
                }
                Console.WriteLine("[TheLoungeScene] All evidence documents enabled for interaction");
            }
        }

        public void SetActiveStressMeter(anakinsoft.game.scenes.lounge.characters.CharacterStateMachine stateMachine)
        {
            uiManager.SetActiveStressMeter(stateMachine);
        }

        public void ClearActiveStressMeter()
        {
            uiManager.ClearActiveStressMeter();
        }

        public void ShowTimePassageMessage(int hoursPassed, int hoursRemaining)
        {
            uiManager.ShowTimePassageMessage(hoursPassed, hoursRemaining);
        }

        public void UpdateTimePassageMessage(GameTime gameTime)
        {
            uiManager.UpdateTimePassageMessage(gameTime);
        }

        /// <summary>
        /// Spawn two characters for interrogation at designated positions
        /// </summary>
        public void SpawnInterrogationCharacters(List<SelectableCharacter> characters, int roundNumber)
        {
            if (characters == null || characters.Count < 2)
            {
                Console.WriteLine("[TheLoungeScene] ERROR: Need 2 characters for interrogation");
                return;
            }

            // Get pathologist position as reference before potentially despawning
            Vector3 pathologistPos = pathologist.Interaction?.Position ?? Vector3.Zero;

            // On first round, completely despawn pathologist (no longer needed)
            if (roundNumber == 1 && pathologist.IsSpawned)
            {
                Console.WriteLine($"[TheLoungeScene] First interrogation round - despawning pathologist at {pathologistPos}");
                DespawnCharacter(pathologist);
                // Keep position reference for interrogation spawn locations
                pathologist = new LoungeCharacterData("Dr. Harmon Kerrigan");
                pathologist.Interaction = new InteractableCharacter("Dr. Harmon Kerrigan", pathologistPos, Vector3.Zero, Vector3.Zero);
            }

            // Position 1: Near where pathologist was (offset slightly)
            Vector3 interrogationPos1 = pathologistPos + new Vector3(2f, 0, 0);
            Vector3 interrogationPos2 = pathologistPos + new Vector3(34f, 0, 0);

            Console.WriteLine($"[SpawnInterrogationCharacters] Pathologist was at: {pathologistPos}");
            Console.WriteLine($"[SpawnInterrogationCharacters] Char1 at: {interrogationPos1}, Char2 at: {interrogationPos2}");

            // Camera positions
            // Vector3 cameraInteractionPosition = pathologistPosition + new Vector3(-16, 15, 0);
            // Vector3 cameraLookAt = new Vector3(10, 0, 0);
            Vector3 cameraPos1 = interrogationPos1 + new Vector3(-16, 15, 0);
            Vector3 cameraPos2 = interrogationPos2 + new Vector3(-16, 15, 0);
            Vector3 cameraLookAt = new Vector3(10, 0, 0);

            // Spawn character 1 using generic spawner
            var config1 = new CharacterSpawnConfig
            {
                Name = characters[0].Name,
                Position = interrogationPos1,
                CameraPosition = cameraPos1,
                CameraLookAt = cameraLookAt,
                ColliderWidth = 15f * LevelScale,
                ColliderHeight = 30f * LevelScale,
                ColliderDepth = 15f * LevelScale
            };
            interrogationCharacter1 = SpawnCharacter(config1);
            interrogationCharacter1.PortraitKey = characters[0].PortraitKey; // Store portrait key for hover display

            // Spawn character 2 using generic spawner
            var config2 = new CharacterSpawnConfig
            {
                Name = characters[1].Name,
                Position = interrogationPos2,
                CameraPosition = cameraPos2,
                CameraLookAt = cameraLookAt,
                ColliderWidth = 15f * LevelScale,
                ColliderHeight = 30f * LevelScale,
                ColliderDepth = 15f * LevelScale
            };
            interrogationCharacter2 = SpawnCharacter(config2);
            interrogationCharacter2.PortraitKey = characters[1].PortraitKey; // Store portrait key for hover display

            Console.WriteLine($"[TheLoungeScene] Spawned {characters[0].Name} and {characters[1].Name} for interrogation");
        }

        /// <summary>
        /// Despawn interrogation characters
        /// </summary>
        public void DespawnInterrogationCharacters()
        {
            // Despawn using generic despawner
            DespawnCharacter(interrogationCharacter1);
            interrogationCharacter1 = null;

            DespawnCharacter(interrogationCharacter2);
            interrogationCharacter2 = null;

            Console.WriteLine("[TheLoungeScene] Despawned interrogation characters");
        }

        public LoungeCharacterData GetInterrogationCharacter1() => interrogationCharacter1;
        public LoungeCharacterData GetInterrogationCharacter2() => interrogationCharacter2;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Mesh loader handles cleanup
                meshLoader.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
