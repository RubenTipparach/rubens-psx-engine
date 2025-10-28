using anakinsoft.entities;
using anakinsoft.game.scenes.lounge.characters;
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
        // TODO: Move to LoungeSceneMeshLoader or debug helper class
        public bool ShowPhysicsWireframe = true; // Toggle to show/hide physics collision wireframes

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

        // Mesh loader
        private LoungeSceneMeshLoader meshLoader;

        // Evidence vial item
        InteractableItem evidenceVial;

        // Evidence table system
        EvidenceTable evidenceTable;

        // Crime scene file
        CrimeSceneFile crimeSceneFile;
        RenderingEntity crimeSceneFileVisual; // Cube placeholder for the file

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

            // Initialize UI Manager (loads portraits)
            uiManager.Initialize();

            // Create first person character at starting position
            CreateCharacter(new Vector3(0, 5f, 0), Quaternion.Identity);

            // Load all lounge geometry and furniture via mesh loader
            meshLoader.LoadAllLoungeGeometry();
            meshLoader.LoadFurniture();

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

            // Create crime scene file on table
            InitializeCrimeSceneFile();

            // Pathologist will be spawned after bartender dialogue
            // Don't initialize pathologist here - will be called from TheLoungeScreen after bartender dialogue
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
            // Using the booth table position from the scene
            Vector3 pathologistPosition = new Vector3(-9.5f, 0, 28f); 

            // Camera interaction position - looking at the pathologist from across the table
            Vector3 cameraInteractionPosition = pathologistPosition + new Vector3(-10, 15, 0);
            Vector3 cameraLookAt = new Vector3(2, 0, 0); // Look at head level

            // Create pathologist interactable
            pathologist.Interaction = new InteractableCharacter("Dr. Harmon Kerrigan", pathologistPosition,
                cameraInteractionPosition, cameraLookAt);

            // Register with interaction system
            interactionSystem.RegisterInteractable(pathologist.Interaction);

            // Create physics collider for pathologist (sitting, so shorter)
            CreatePathologistCollider(pathologistPosition);

            Console.WriteLine($"Pathologist created at position: {pathologistPosition}");
            Console.WriteLine($"Interaction camera position: {cameraInteractionPosition}");

            // Create pathologist model using alien-2 (sitting character)
            var pathologistMaterial = new UnlitSkinnedMaterial("textures/prototype/grass", "shaders/surface/SkinnedVertexLit", useDefault: false);
            pathologistMaterial.AmbientColor = new Vector3(0.1f, 0.1f, 0.2f);
            pathologistMaterial.EmissiveColor = new Vector3(0.1f, 0.1f, 0.2f);
            pathologistMaterial.LightDirection = Vector3.Normalize(new Vector3(0.3f, -1, -0.5f));
            pathologistMaterial.LightColor = new Vector3(1.0f, 0.95f, 0.9f);
            pathologistMaterial.LightIntensity = 0.9f;

            pathologist.Model = new SkinnedRenderingEntity("models/characters/alien-2", pathologistMaterial);
            pathologist.Model.Position = pathologistPosition;
            pathologist.Model.Scale = Vector3.One * 0.25f * LevelScale; // Slightly larger for sitting pose
            pathologist.Model.Rotation = QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0); // Face towards camera
            pathologist.Model.IsVisible = true;

            // Play idle animation
            if (pathologist.Model is SkinnedRenderingEntity skinnedPathologist)
            {
                var skinData = skinnedPathologist.GetSkinningData();
                if (skinData != null && skinData.AnimationClips.Count > 0)
                {
                    var firstClipName = skinData.AnimationClips.Keys.First();
                    skinnedPathologist.PlayAnimation(firstClipName, loop: true);
                }
            }

            AddRenderingEntity(pathologist.Model);

            Console.WriteLine("========================================\n");
        }

        private void CreatePathologistCollider(Vector3 position)
        {
            // Create a box collider for the pathologist (sitting, so shorter)
            pathologist.ColliderWidth = 15f * LevelScale;
            pathologist.ColliderHeight = 30f * LevelScale; // Sitting height
            pathologist.ColliderDepth = 15f * LevelScale;

            var boxShape = new Box(pathologist.ColliderWidth, pathologist.ColliderHeight, pathologist.ColliderDepth);
            var shapeIndex = physicsSystem.Simulation.Shapes.Add(boxShape);

            // Move the collider up so the bottom touches the floor
            pathologist.ColliderCenter = position + new Vector3(0, pathologist.ColliderHeight / 2f, 0);

            // Create static body at adjusted position
            var staticHandle = physicsSystem.Simulation.Statics.Add(new StaticDescription(
                pathologist.ColliderCenter.ToVector3N(),
                Quaternion.Identity.ToQuaternionN(),
                shapeIndex));

            // Store the static handle in the pathologist for interaction detection
            pathologist.Interaction.SetStaticHandle(staticHandle);

            Console.WriteLine($"Created pathologist physics collider at {pathologist.ColliderCenter} (size: {pathologist.ColliderWidth}x{pathologist.ColliderHeight}x{pathologist.ColliderDepth})");
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

        private void InitializeEvidenceTable()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("INITIALIZING EVIDENCE TABLE");
            Console.WriteLine("========================================");

            // Define table area near pathologist's location
            // Table center positioned on the left side of the lounge
            Vector3 tableCenter = new Vector3(-9.5f, 12f, 28f); // Same general area as pathologist
            Vector3 tableSize = new Vector3(20f, 2f, 15f); // 20 units wide, 15 units deep

            // Create 3x3 grid (9 slots for evidence items)
            // This gives us organized positions for:
            // - Crime scene file (center)
            // - Evidence items (surrounding slots)
            // - Future collectibles
            evidenceTable = new EvidenceTable(tableCenter, tableSize, 3, 3);

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

        private void InitializeCrimeSceneFile()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("CREATING CRIME SCENE FILE");
            Console.WriteLine("========================================");

            // Place crime scene file in center slot of evidence table (slot [1,1])
            // Grid layout (3x3):
            // [0,0] [0,1] [0,2]
            // [1,0] [1,1] [1,2]  <- Center slot [1,1] for crime scene file
            // [2,0] [2,1] [2,2]
            Vector3 filePosition = evidenceTable.GetSlotPosition(1, 1); // Center of 3x3 grid

            // Create crime scene file interactable
            crimeSceneFile = new CrimeSceneFile(
                "Suspects File",
                filePosition
            );

            // Register file with evidence table
            evidenceTable.PlaceItem("crime_scene_file", 1, 1, crimeSceneFile);

            // Initialize with all suspect entries (empty transcripts until interviewed)
            crimeSceneFile.AddTranscript("Bartender Zix", "Not yet interviewed.", false);
            crimeSceneFile.AddTranscript("Dr. Harmon Kerrigan", "Chief Medical Officer - performed the autopsy.", false);
            crimeSceneFile.AddTranscript("Commander Sylar Von", "Not yet interviewed.", false);
            crimeSceneFile.AddTranscript("Lt. Marcus Webb", "Not yet interviewed.", false);
            crimeSceneFile.AddTranscript("Ensign Tork", "Not yet interviewed.", false);
            crimeSceneFile.AddTranscript("Chief Kala Solis", "Not yet interviewed.", false);
            crimeSceneFile.AddTranscript("Maven Kilroth", "Not yet interviewed.", false);
            crimeSceneFile.AddTranscript("Tehvora", "Not yet interviewed.", false);
            crimeSceneFile.AddTranscript("Lucky Chen", "Not yet interviewed.", false);

            // Register with interaction system
            interactionSystem.RegisterInteractable(crimeSceneFile);

            // Create visual placeholder (cube) - 90% smaller for a small file/tablet appearance
            var fileMaterial = new UnlitMaterial("textures/prototype/concrete");
            crimeSceneFileVisual = new RenderingEntity("models/cube", "textures/prototype/concrete");
            crimeSceneFileVisual.Position = filePosition;
            crimeSceneFileVisual.Scale = new Vector3(0.3f, 0.05f, 0.2f) * LevelScale; // 90% smaller - small tablet/file size
            crimeSceneFileVisual.Rotation = QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0);
            crimeSceneFileVisual.IsVisible = true;

            AddRenderingEntity(crimeSceneFileVisual);

            Console.WriteLine($"Crime scene file created at position: {filePosition}");
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

        public void UpdateWithCamera(GameTime gameTime, Camera camera, bool isDialogueActive = false)
        {
            // Update the scene normally first
            Update(gameTime);

            // Update intro text via UI Manager
            uiManager.UpdateIntroText(gameTime);

            // Only update character movement and interactions if not in menu
            if (!Globals.IsInMenuState())
            {
                // Update interaction system with camera for raycasting (skip during dialogue)
                if (!isDialogueActive)
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

            // Update starfield
            starfield.Update(gameTime);
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
            // Draw starfield first (background)
            starfield.Draw(camera);

            // Draw all entities using the base scene drawing
            base.Draw(gameTime, camera);

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
            }
        }

        /// <summary>
        /// Draws the UI elements
        /// </summary>
        public void DrawUI(GameTime gameTime, Camera camera, SpriteBatch spriteBatch, bool isDialogueActive = false)
        {
            var font = Globals.fontNTR;
            uiManager.DrawUI(gameTime, spriteBatch, font, interactionSystem, isDialogueActive);
        }




        // Utility methods for external access
        public bool IsCharacterActive() => characterActive;
        public CharacterInput? GetCharacter() => character;
        public Quaternion GetCharacterInitialRotation() => characterInitialRotation;
        public InteractableCharacter GetBartender() => bartender.Interaction;
        public InteractableCharacter GetPathologist() => pathologist.Interaction;
        public InteractableItem GetEvidenceVial() => evidenceVial;
        public CrimeSceneFile GetCrimeSceneFile() => crimeSceneFile;
        public EvidenceTable GetEvidenceTable() => evidenceTable;
        public InteractionSystem GetInteractionSystem() => interactionSystem;
        public bool IsShowingIntroText() => uiManager.ShowIntroText;
        public Dictionary<string, Texture2D> GetCharacterPortraits() => uiManager.CharacterPortraits;

        public void SetActiveDialogueCharacter(string characterKey)
        {
            uiManager.SetActiveDialogueCharacter(characterKey);
        }

        public void ClearActiveDialogueCharacter()
        {
            uiManager.ClearActiveDialogueCharacter();
        }

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
