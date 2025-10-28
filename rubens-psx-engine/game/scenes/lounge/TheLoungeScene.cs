using anakinsoft.entities;
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
    /// TODO: Move debug wireframe drawing (DrawDebugBox, DrawDebugSphere, DrawEvidenceTableGrid) to mesh loader
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

        // Direct BepuPhysics meshes for models
        List<Mesh> loungeBepuMeshes;

        // Collision mesh wireframe data
        List<List<Vector3>> meshTriangleVertices; // Store triangle vertices for wireframe
        List<(Vector3 position, Quaternion rotation)> staticMeshTransforms; // Store mesh transforms

        // Input handling
        KeyboardState previousKeyboard;

        // Alien character with skeletal animation support
        SkinnedRenderingEntity alienCharacter;

        // Point light
        PointLight centerLight;

        // Interaction system
        InteractionSystem interactionSystem;

        // Bartender character
        InteractableCharacter bartenderCharacterInteraction;
        SkinnedRenderingEntity bartenderModel;

        // Bartender collider dimensions (shared between physics and debug visualization)
        private float bartenderColliderWidth;
        private float bartenderColliderHeight;
        private float bartenderColliderDepth;
        private Vector3 bartenderColliderCenter;

        // Pathologist character (Dr. Harmon Kerrigan)
        InteractableCharacter pathologistCharacterInteraction;
        SkinnedRenderingEntity pathologistModel;
        private float pathologistColliderWidth;
        private float pathologistColliderHeight;
        private float pathologistColliderDepth;
        private Vector3 pathologistColliderCenter;
        private bool pathologistSpawned = false;

        // Evidence vial item
        InteractableItem evidenceVial;

        // Evidence table system
        EvidenceTable evidenceTable;

        // Crime scene file
        CrimeSceneFile crimeSceneFile;
        RenderingEntity crimeSceneFileVisual; // Cube placeholder for the file

        // Intro text state
        private bool showIntroText = true;
        private float introTextTimer = 0f;
        private const float IntroTextDuration = 4.0f; // Show for 8 seconds
        private const string IntroText = "Welcome to the Lounge. You are a detective on board the UEFS Marron. The Telirian ambassador is dead. Question the suspects, determine motive, means, and opportunity. Determine who is guilty before the Telirians arrive. Failure to do so will mean all out war.";

        // Intro text teletype effect
        private float introTeletypeTimer = 0f;
        private int introVisibleCharacters = 0;
        private const float IntroCharactersPerSecond = 30f;
        private bool introTeletypeComplete = false;
        private KeyboardState previousKeyboardState;

        // Character profiles (portraits)
        private Dictionary<string, Texture2D> characterPortraits;
        private string hoveredCharacter = null;
        private string activeDialogueCharacter = null;
        private Texture2D portraitFrame;

        // Starfield
        private struct Star
        {
            public Vector3 Position;
            public Color Color;
            public float Depth; // 0 = farthest, 1 = closest
            public float Speed; // Movement speed based on depth
        }
        private List<Star> stars;
        private const int StarCount = 1000;
        private const float StarfieldZStart = 8000f; // Stars spawn here (front)
        private const float StarfieldZEnd = -200f; // Stars despawn here (far back)
        private const float StarfieldRadius = 2000f; // Large radius to extend beyond lounge geometry
        private const float StarfieldMinRadius = 100f; // Exclude center area to avoid lounge geometry
        private const float StarLineLength = 500.0f; // 3 meter streak length (3x longer)
        private const float StarBaseSpeed = 2000f; // Base movement speed in units/second

        public TheLoungeScene() : base()
        {
            // Initialize character system and physics
            characters = null; // Will be initialized in physics system
            physicsSystem = new PhysicsSystem(ref characters);

            loungeBepuMeshes = new List<Mesh>();
            meshTriangleVertices = new List<List<Vector3>>();
            staticMeshTransforms = new List<(Vector3 position, Quaternion rotation)>();

            // Initialize interaction system
            interactionSystem = new InteractionSystem(physicsSystem);

            // Initialize starfield
            stars = new List<Star>();
            InitializeStarfield();

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

            // Initialize character portraits
            InitializeCharacterPortraits();

            // Create first person character at starting position
            CreateCharacter(new Vector3(0, 5f, 0), Quaternion.Identity);

            float jitter = 3;
            var affine = 0;

            // Create unlit materials for each lounge texture
            var ceilingMat = new UnlitMaterial("textures/Lounge/Cieling");
            ceilingMat.VertexJitterAmount = jitter;
            ceilingMat.Brightness = 1.2f;
            ceilingMat.AffineAmount = affine;

            var doorMat = new UnlitMaterial("textures/Lounge/door");
            doorMat.VertexJitterAmount = jitter;
            doorMat.Brightness = 1.2f;
            doorMat.AffineAmount = affine;

            var floorMat = new UnlitMaterial("textures/Lounge/floor_1");
            floorMat.VertexJitterAmount = jitter;
            floorMat.AffineAmount = affine;
            floorMat.Brightness = 1.2f;

            var wall1Mat = new UnlitMaterial("textures/Lounge/wall_1");
            wall1Mat.VertexJitterAmount = jitter;
            wall1Mat.Brightness = 1.2f;
            wall1Mat.AffineAmount = affine;

            var wall2Mat = new UnlitMaterial("textures/Lounge/wall_2");
            wall2Mat.VertexJitterAmount = jitter;
            wall2Mat.Brightness = 1.2f;
            wall2Mat.AffineAmount = affine;
            var windowMat = new UnlitMaterial("textures/Lounge/window");
            wall2Mat.VertexJitterAmount = jitter;
            wall2Mat.Brightness = 1.2f;
            wall2Mat.AffineAmount = affine;

            // Load all models from models/lounge with colliders
            // Position them in the scene - you can adjust these positions as needed
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { ceilingMat }, "models/lounge/Ceiling2");
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { doorMat }, "models/lounge/Door");
            CreateStaticMesh(new Vector3(0, 0, 0), QuaternionExtensions.CreateFromYawPitchRollDegrees(180,0,0), new[] { doorMat }, "models/lounge/Door");
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { floorMat }, "models/lounge/Lounge_floor");
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { wall1Mat }, "models/lounge/Lounge_wall");
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { wall2Mat }, "models/lounge/Wall_L");
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { wall2Mat }, "models/lounge/Wall_R");
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { windowMat }, "models/lounge/Window");

            // Create furniture materials
            var barMat = new UnlitMaterial("textures/Lounge/Bar");
            barMat.VertexJitterAmount = jitter;
            barMat.Brightness = 1.2f;
            barMat.AffineAmount = affine;

            var chairMat = new UnlitMaterial("textures/Lounge/chair");
            chairMat.VertexJitterAmount = jitter;
            chairMat.Brightness = 1.2f;
            chairMat.AffineAmount = affine;

            var tableMat = new UnlitMaterial("textures/Lounge/table");
            tableMat.VertexJitterAmount = jitter;
            tableMat.Brightness = 1.2f;
            tableMat.AffineAmount = affine;

            var boothMat = new UnlitMaterial("textures/Lounge/booth");
            boothMat.VertexJitterAmount = jitter;
            boothMat.Brightness = 1.2f;
            boothMat.AffineAmount = affine;

            // Add lounge furniture
            var posScale = 10f;
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { barMat }, "models/lounge/furnitures/lounge_bar");
            CreateStaticMesh(new Vector3(0, 0, 0), Quaternion.Identity, new[] { barMat }, "models/lounge/furnitures/lounge_bar_2");
            CreateStaticMesh(new Vector3(-1.70852f, 0, -3.29662f) * posScale,
                Quaternion.Identity, new[] { chairMat }, "models/lounge/furnitures/lounge_high_chair");
            CreateStaticMesh(new Vector3(-1.70852f, 0, -2) * posScale,
                Quaternion.Identity, new[] { chairMat }, "models/lounge/furnitures/lounge_high_chair");

            CreateStaticMesh(new Vector3(-2.91486f, 0, 2.17103f) * posScale,
                Quaternion.Identity, new[] { chairMat }, "models/lounge/furnitures/lounge_chair");
            CreateStaticMesh(new Vector3(-2.91486f, 0, 3.41485f) * posScale,
                Quaternion.Identity, new[] { chairMat }, "models/lounge/furnitures/lounge_chair");
            CreateStaticMesh(new Vector3(-1.28593f, 0, 3.11644f) * posScale,
                Quaternion.Identity, new[] { tableMat }, "models/lounge/furnitures/lounge_table");
            CreateStaticMesh(new Vector3(2.05432f, 0, 3.11644f) * posScale,
                Quaternion.Identity, new[] { tableMat }, "models/lounge/furnitures/lounge_table");

            CreateStaticMesh(new Vector3(0.137007f, 0, 2.8772f) * posScale,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(180, 0, 0), new[] { boothMat }, "models/lounge/furnitures/lounge_booth");
            CreateStaticMesh(new Vector3(0.137007f, 0, 2.8772f) * posScale,
                Quaternion.Identity, new[] { boothMat }, "models/lounge/furnitures/lounge_booth");
            CreateStaticMesh(new Vector3(3.18364f, 0, 2.8772f) * posScale,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(180, 0, 0), new[] { boothMat }, "models/lounge/furnitures/lounge_booth");

            CreateStaticMesh(new Vector3(-3.82676f, 0, 2.89935f ) * posScale,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(90,0,0), new[] { barMat }, "models/lounge/furnitures/lounge_shelf");
            CreateStaticMesh(new Vector3(3.68024f, 0, 2.89935f) * posScale,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0), new[] { barMat }, "models/lounge/furnitures/lounge_shelf");

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
            bartenderCharacterInteraction = new InteractableCharacter("Bartender", bartenderPosition,
                cameraInteractionPosition, cameraLookAt);

            // Register with interaction system
            interactionSystem.RegisterInteractable(bartenderCharacterInteraction);

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

            bartenderModel = new SkinnedRenderingEntity("models/characters/alien", bartenderMaterial);
            bartenderModel.Position = bartenderPosition;
            bartenderModel.Scale = Vector3.One * 0.25f * LevelScale;
            bartenderModel.Rotation = Quaternion.Identity; // Face forward (was already facing the right way)
            bartenderModel.IsVisible = true;

            // Play idle animation
            if (bartenderModel is SkinnedRenderingEntity skinnedBartender)
            {
                var skinData = skinnedBartender.GetSkinningData();
                if (skinData != null && skinData.AnimationClips.Count > 0)
                {
                    var firstClipName = skinData.AnimationClips.Keys.First();
                    skinnedBartender.PlayAnimation(firstClipName, loop: true);
                }
            }

            AddRenderingEntity(bartenderModel);

            Console.WriteLine("========================================\n");
        }

        public void SpawnPathologist()
        {
            if (pathologistSpawned)
            {
                Console.WriteLine("Pathologist already spawned, skipping...");
                return;
            }

            pathologistSpawned = true;
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
            Vector3 cameraInteractionPosition = pathologistPosition + new Vector3(15, 20, 0);
            Vector3 cameraLookAt = pathologistPosition + new Vector3(0, 10, 0); // Look at head level

            // Create pathologist interactable
            pathologistCharacterInteraction = new InteractableCharacter("Dr. Harmon Kerrigan", pathologistPosition,
                cameraInteractionPosition, cameraLookAt);

            // Register with interaction system
            interactionSystem.RegisterInteractable(pathologistCharacterInteraction);

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

            pathologistModel = new SkinnedRenderingEntity("models/characters/alien-2", pathologistMaterial);
            pathologistModel.Position = pathologistPosition;
            pathologistModel.Scale = Vector3.One * 0.25f * LevelScale; // Slightly larger for sitting pose
            pathologistModel.Rotation = QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0); // Face towards camera
            pathologistModel.IsVisible = true;

            // Play idle animation
            if (pathologistModel is SkinnedRenderingEntity skinnedPathologist)
            {
                var skinData = skinnedPathologist.GetSkinningData();
                if (skinData != null && skinData.AnimationClips.Count > 0)
                {
                    var firstClipName = skinData.AnimationClips.Keys.First();
                    skinnedPathologist.PlayAnimation(firstClipName, loop: true);
                }
            }

            AddRenderingEntity(pathologistModel);

            Console.WriteLine("========================================\n");
        }

        private void CreatePathologistCollider(Vector3 position)
        {
            // Create a box collider for the pathologist (sitting, so shorter)
            pathologistColliderWidth = 15f * LevelScale;
            pathologistColliderHeight = 30f * LevelScale; // Sitting height
            pathologistColliderDepth = 15f * LevelScale;

            var boxShape = new Box(pathologistColliderWidth, pathologistColliderHeight, pathologistColliderDepth);
            var shapeIndex = physicsSystem.Simulation.Shapes.Add(boxShape);

            // Move the collider up so the bottom touches the floor
            pathologistColliderCenter = position + new Vector3(0, pathologistColliderHeight / 2f, 0);

            // Create static body at adjusted position
            var staticHandle = physicsSystem.Simulation.Statics.Add(new StaticDescription(
                pathologistColliderCenter.ToVector3N(),
                Quaternion.Identity.ToQuaternionN(),
                shapeIndex));

            // Store the static handle in the pathologist for interaction detection
            pathologistCharacterInteraction.SetStaticHandle(staticHandle);

            Console.WriteLine($"Created pathologist physics collider at {pathologistColliderCenter} (size: {pathologistColliderWidth}x{pathologistColliderHeight}x{pathologistColliderDepth})");
        }

        private void CreateBartenderCollider(Vector3 position)
        {
            // Create a box collider for the bartender (4x taller, bottom at floor level)
            bartenderColliderWidth = 10f * LevelScale;
            bartenderColliderHeight = 48f * LevelScale; // 60% of original 80f height
            bartenderColliderDepth = 10f * LevelScale;

            var boxShape = new Box(bartenderColliderWidth, bartenderColliderHeight, bartenderColliderDepth);
            var shapeIndex = physicsSystem.Simulation.Shapes.Add(boxShape);

            // Move the collider up so the bottom touches the floor (half of height up from position)
            bartenderColliderCenter = position + new Vector3(0, bartenderColliderHeight / 2f, 0);

            // Create static body at adjusted position
            var staticHandle = physicsSystem.Simulation.Statics.Add(new StaticDescription(
                bartenderColliderCenter.ToVector3N(),
                Quaternion.Identity.ToQuaternionN(),
                shapeIndex));

            // Store the static handle in the bartender for interaction detection
            bartenderCharacterInteraction.SetStaticHandle(staticHandle);

            Console.WriteLine($"Created bartender physics collider at {bartenderColliderCenter} (size: {bartenderColliderWidth}x{bartenderColliderHeight}x{bartenderColliderDepth})");
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

        private void InitializeCharacterPortraits()
        {
            characterPortraits = new Dictionary<string, Texture2D>();

            // Load character portraits from chars folder
            characterPortraits["NPC_Bartender"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/(NPC) bartender zix");
            characterPortraits["NPC_Ambassador"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/(NPC) Ambassador Tesh");
            characterPortraits["NPC_DrThorne"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/(NPC) Dr thorne - xenopathologist");
            characterPortraits["DrHarmon"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Dr Harmon - CMO");
            characterPortraits["CommanderSylar"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Commander Sylar Von - Body guard");
            characterPortraits["LtWebb"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Lt. Marcus Webb");
            characterPortraits["EnsignTork"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Ensign Tork - Junior Eng");
            characterPortraits["ChiefSolis"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Chief Kala Solis - Sec Cheif");
            characterPortraits["MavenKilroth"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Maven Kilroth - Smuggler");
            characterPortraits["Tehvora"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Tehvora - Diplomatic Atache (Kullan)");
            characterPortraits["LuckyChen"] = Globals.screenManager.Content.Load<Texture2D>("textures/chars/Lucky Chen - Quartermaster");

            // Create portrait frame (simple colored rectangle)
            var whiteTexture = Globals.screenManager.Content.Load<Texture2D>("textures/white");
            portraitFrame = whiteTexture;

            Console.WriteLine($"Initialized {characterPortraits.Count} character portraits");
        }

        private void CreateStaticMesh(Vector3 offset, Quaternion rotation, Material[] mats, string mesh)
        {
            // Create corridor entity with material channels
            var loadedMats = new Dictionary<int, Material>();
            for(int i = 0; i < mats.Length; i++)
            {
                loadedMats.Add(i, mats[i]);
            }
            var entity = new MultiMaterialRenderingEntity(mesh, loadedMats);

            entity.Position = Vector3.Zero + offset;
            entity.Scale = Vector3.One * 0.2f * LevelScale;
            entity.Rotation = rotation;
            entity.IsVisible = true;

            // Add to rendering entities
            AddRenderingEntity(entity);

            // Create physics mesh for the model
            CreatePhysicsMesh(mesh, offset, rotation,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, -90, 0), Vector3.Zero);
        }

        // Star color table
        private static readonly Color[] StarColors = new Color[]
        {
            ColorExtensions.FromHex("#492d38"), // Farthest - dark purple
            ColorExtensions.FromHex("#ab5236"), // Medium-far - tan
            ColorExtensions.FromHex("#ffccaa"), // Medium-close - peach
            ColorExtensions.FromHex("#fff1e8")  // Closest - light peach
        };

        // Calculate max distance based on farthest possible spawn point
        private static readonly float MaxStarDistance = (float)Math.Sqrt(StarfieldRadius * StarfieldRadius + StarfieldZEnd * StarfieldZEnd);

        private Color GetStarColorFromPosition(Vector3 position)
        {
            // Calculate depth based on distance from origin (0,0,0)
            float distanceFromOrigin = position.Length();
            float depth = 1f - Math.Min(distanceFromOrigin / StarfieldZStart, 1f);

            // Hard transition between 4 color bands based on depth
            if (depth < 0.33f)
            {
                return StarColors[0]; // Farthest
            }
            else if (depth < 0.46f)
            {
                return StarColors[1]; // Medium-far
            }
            else if (depth < 0.6f)
            {
                return StarColors[2]; // Medium-close
            }
            else
            {
                return StarColors[3]; // Closest
            }
        }

        private void InitializeStarfield()
        {
            Random random = new Random();

            // Create evenly distributed stars along the Z axis
            for (int i = 0; i < StarCount; i++)
            {
                // Random position in XY plane within radius, excluding center area (donut shape)
                float angle = (float)(random.NextDouble() * Math.PI * 2);

                // Map random value to range between MinRadius and MaxRadius
                float normalizedDistance = (float)Math.Sqrt(random.NextDouble());
                float distance = StarfieldMinRadius + normalizedDistance * (StarfieldRadius - StarfieldMinRadius);

                float x = distance * (float)Math.Cos(angle);
                float y = distance * (float)Math.Sin(angle);

                // Evenly distribute along Z axis from start to end (prewarm the starfield)
                // This ensures stars are visible immediately when the game loads
                float z = StarfieldZStart + (float)random.NextDouble() * (StarfieldZEnd - StarfieldZStart);

                Vector3 position = new Vector3(x, y, z);

                // Get color based on position
                Color starColor = GetStarColorFromPosition(position);

                // Calculate depth for speed variation
                float distanceFromOrigin = position.Length();
                float depth = 1f - Math.Min(distanceFromOrigin / MaxStarDistance, 1f);
                float speed = StarBaseSpeed * (0.5f + depth * 0.5f);

                stars.Add(new Star
                {
                    Position = position,
                    Color = starColor,
                    Depth = depth,
                    Speed = speed
                });
            }
        }

        private void CreatePhysicsMesh(string mesh, Vector3 offset, Quaternion rotation,
            Quaternion rotationOffset, Vector3 physicsMeshOffset)
        {
            try
            {
                // Load the same model used for rendering
                var model = Globals.screenManager.Content.Load<Model>(mesh);

                // Use consistent scaling approach: visual scale * physics scale factor
                var visualScale = Vector3.One * 0.2f * LevelScale; // Same scale as rendering entity
                var physicsScale = visualScale * 100; // Apply our learned scaling factor

                // Extract triangles and wireframe vertices for rendering
                var (triangles, wireframeVertices) = BepuMeshExtractor.ExtractTrianglesFromModel(model, physicsScale, physicsSystem);
                var bepuMesh = new Mesh(triangles, Vector3.One.ToVector3N(), physicsSystem.BufferPool);

                // Add the mesh shape to the simulation's shape collection
                var shapeIndex = physicsSystem.Simulation.Shapes.Add(bepuMesh);
                // Rotate the rotation by this
                var rotation2 = rotation * rotationOffset;

                // Create static body with the mesh shape
                var staticHandle = physicsSystem.Simulation.Statics.Add(new StaticDescription(
                    offset.ToVector3N(),
                    rotation2.ToQuaternionN(),
                    shapeIndex));

                // Keep references for cleanup and wireframe rendering
                loungeBepuMeshes.Add(bepuMesh);
                meshTriangleVertices.Add(wireframeVertices);
                staticMeshTransforms.Add((offset, rotation2));

                Console.WriteLine($"Created lounge physics mesh at position: {offset} with physics scale: {physicsScale}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to create lounge physics mesh: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                // Continue without physics mesh for this model
            }
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

            // Update intro text timer and teletype effect
            if (showIntroText)
            {
                var keyboard = Keyboard.GetState();

                // Update teletype effect
                if (!introTeletypeComplete)
                {
                    introTeletypeTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    introVisibleCharacters = (int)(introTeletypeTimer * IntroCharactersPerSecond);

                    if (introVisibleCharacters >= IntroText.Length)
                    {
                        introVisibleCharacters = IntroText.Length;
                        introTeletypeComplete = true;
                    }
                }

                // Handle E key press
                if (keyboard.IsKeyDown(Keys.E) && !previousKeyboardState.IsKeyDown(Keys.E))
                {
                    if (!introTeletypeComplete)
                    {
                        // Complete teletype immediately
                        introVisibleCharacters = IntroText.Length;
                        introTeletypeComplete = true;
                    }
                    else
                    {
                        // Skip intro text entirely
                        showIntroText = false;
                    }
                }

                previousKeyboardState = keyboard;

                // Auto-advance after duration if teletype is complete
                if (introTeletypeComplete)
                {
                    introTextTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (introTextTimer >= IntroTextDuration)
                    {
                        showIntroText = false;
                    }
                }
            }

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
                        if (hoveredChar == bartenderCharacterInteraction)
                            hoveredCharacter = "NPC_Bartender";
                        else if (pathologistSpawned && hoveredChar == pathologistCharacterInteraction)
                            hoveredCharacter = "DrHarmon";
                        else
                            hoveredCharacter = null;
                    }
                    else
                    {
                        hoveredCharacter = null;
                    }
                }
                else
                {
                    // During dialogue, keep showing the active character's portrait
                    hoveredCharacter = activeDialogueCharacter;
                }

                // Only allow character movement after intro and after load delay
                if (!showIntroText && characterActive && character.HasValue && timeSinceLoad > characterLoadDelay)
                {
                    character.Value.UpdateCharacterGoals(Keyboard.GetState(), camera, (float)gameTime.ElapsedGameTime.TotalSeconds);
                }
            }

            // Update starfield
            UpdateStarfield(gameTime);
        }

        private void UpdateStarfield(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = 0; i < stars.Count; i++)
            {
                var star = stars[i];

                // Move star backward along -Z axis (away from camera)
                star.Position.Z -= star.Speed * deltaTime;

                // If star passed the end point, respawn at start
                if (star.Position.Z < StarfieldZEnd)
                {
                    // Reset Z to start position (close to camera)
                    star.Position.Z = StarfieldZStart;
                }

                // Update color based on current position (distance from origin)
                star.Color = GetStarColorFromPosition(star.Position);

                // Update depth for speed variation
                float distanceFromOrigin = star.Position.Length();
                star.Depth = 1f - Math.Min(distanceFromOrigin / MaxStarDistance, 1f);

                stars[i] = star;
            }
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
            DrawStarfield(camera);

            // Draw all entities using the base scene drawing
            base.Draw(gameTime, camera);

            // Draw wireframe visualization of static mesh collision geometry if debug mode enabled
            if (ShowPhysicsWireframe)
            {
                DrawStaticMeshWireframes(gameTime, camera);

                // Also draw alien character wireframe and bounding box in purple
                if (alienCharacter != null && alienCharacter.IsVisible)
                {
                    DrawAlienWireframe(gameTime, camera);
                }

                // Draw bartender character wireframe
                if (bartenderModel != null && bartenderModel.IsVisible)
                {
                    DrawAlienWireframe(gameTime, camera); // Reuse the same method
                }

                // Draw pathologist character wireframe (only if spawned)
                if (pathologistSpawned && pathologistModel != null && pathologistModel.IsVisible)
                {
                    DrawAlienWireframe(gameTime, camera); // Reuse the same method
                }

                // Draw bartender collider box
                DrawBartenderCollider(camera);

                // Draw pathologist collider box (only if spawned)
                if (pathologistSpawned)
                {
                    DrawPathologistCollider(camera);
                }

                // Draw interaction camera positions
                DrawInteractionDebugVisualization(camera);
            }
        }

        /// <summary>
        /// Draws the UI elements
        /// </summary>
        public void DrawUI(GameTime gameTime, Camera camera, SpriteBatch spriteBatch, bool isDialogueActive = false)
        {
            var font = Globals.fontNTR;
            if (font != null)
            {
                // Show loading indicator during character load delay
                if (timeSinceLoad <= characterLoadDelay)
                {
                    string loadingText = "Loading The Lounge...";
                    var textSize = font.MeasureString(loadingText);
                    var screenCenter = new Vector2(
                        Globals.screenManager.GraphicsDevice.Viewport.Width / 2f,
                        Globals.screenManager.GraphicsDevice.Viewport.Height / 2f
                    );

                    // Draw loading text with fade effect
                    float fadeAlpha = 1.0f - (timeSinceLoad / characterLoadDelay);
                    spriteBatch.DrawString(font, loadingText,
                        screenCenter - textSize / 2f,
                        Color.White * fadeAlpha);
                }
                // Show intro text on black background
                else if (showIntroText)
                {
                    DrawIntroText(spriteBatch, font);
                }

                // Draw interaction UI (only when not showing intro and not in dialogue)
                if (!showIntroText && !isDialogueActive)
                {
                    interactionSystem?.DrawUI(spriteBatch, font);
                }

                // Draw character portrait if hovering or in dialogue
                if (!showIntroText && hoveredCharacter != null)
                {
                    DrawCharacterPortrait(spriteBatch, hoveredCharacter);
                }
            }
        }

        private void DrawCharacterPortrait(SpriteBatch spriteBatch, string characterKey)
        {
            if (!characterPortraits.ContainsKey(characterKey))
                return;

            var viewport = Globals.screenManager.GraphicsDevice.Viewport;
            var portrait = characterPortraits[characterKey];

            // Portrait dimensions (matching 64x96 ratio = 2:3 aspect ratio)
            int portraitWidth = 128;  // 2x scale of 64
            int portraitHeight = 192; // 2x scale of 96
            int frameThickness = 4;
            int margin = 20;

            // Position in top-right corner
            Rectangle portraitRect = new Rectangle(
                viewport.Width - portraitWidth - margin - frameThickness,
                margin,
                portraitWidth,
                portraitHeight
            );

            // Frame rectangle (slightly larger)
            Rectangle frameRect = new Rectangle(
                portraitRect.X - frameThickness,
                portraitRect.Y - frameThickness,
                portraitRect.Width + frameThickness * 2,
                portraitRect.Height + frameThickness * 2
            );

            // Draw frame
            spriteBatch.Draw(portraitFrame, frameRect, Color.Gold);

            // Draw portrait
            spriteBatch.Draw(portrait, portraitRect, Color.White);
        }

        /// <summary>
        /// Draws the intro text on black background
        /// </summary>
        private void DrawIntroText(SpriteBatch spriteBatch, SpriteFont font)
        {
            var viewport = Globals.screenManager.GraphicsDevice.Viewport;

            // Draw black background
            var blackTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            blackTexture.SetData(new[] { Color.Black });
            spriteBatch.Draw(blackTexture, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black);

            // Get visible text based on teletype progress
            string visibleText = IntroText.Substring(0, Math.Min(introVisibleCharacters, IntroText.Length));

            // Wrap and measure the intro text
            string wrappedText = WrapText(visibleText, font, viewport.Width - 100); // 50px padding on each side
            var textSize = font.MeasureString(wrappedText);

            // Center the text on screen
            var textPosition = new Vector2(
                (viewport.Width - textSize.X) / 2f,
                (viewport.Height - textSize.Y) / 2f
            );

            // Draw the text with a slight shadow for readability
            spriteBatch.DrawString(font, wrappedText, textPosition + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(font, wrappedText, textPosition, Color.White);

            // Draw prompt
            string promptText = introTeletypeComplete ? "Press [E] to continue..." : "Press [E] to skip...";
            var promptSize = font.MeasureString(promptText);
            var promptPosition = new Vector2(
                (viewport.Width - promptSize.X) / 2f,
                viewport.Height - 100
            );
            spriteBatch.DrawString(font, promptText, promptPosition, Color.Gray);
        }

        /// <summary>
        /// Wraps text to fit within a specified width
        /// </summary>
        private string WrapText(string text, SpriteFont font, float maxWidth)
        {
            string[] words = text.Split(' ');
            string wrappedText = "";
            string line = "";

            foreach (string word in words)
            {
                string testLine = line + word + " ";
                Vector2 testSize = font.MeasureString(testLine);

                if (testSize.X > maxWidth && line.Length > 0)
                {
                    wrappedText += line.TrimEnd() + "\n";
                    line = word + " ";
                }
                else
                {
                    line = testLine;
                }
            }

            wrappedText += line.TrimEnd();
            return wrappedText;
        }

        private void DrawStarfield(Camera camera)
        {
            var graphicsDevice = Globals.screenManager.GraphicsDevice;

            // Create a basic effect for rendering star streaks
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.View = camera.View;
            basicEffect.Projection = camera.Projection;
            basicEffect.World = Matrix.Identity;

            // Create star streak lines pointing along the Z axis (direction of movement)
            var starVertices = new List<VertexPositionColor>();

            foreach (var star in stars)
            {
                // Create a line streak pointing forward along +Z axis (trail effect showing motion away from camera)
                Vector3 startPoint = star.Position;
                Vector3 endPoint = star.Position + new Vector3(0, 0, StarLineLength);

                // Add the line (2 vertices per star)
                starVertices.Add(new VertexPositionColor(startPoint, star.Color));
                starVertices.Add(new VertexPositionColor(endPoint, star.Color));
            }

            // Draw the star streaks as line list
            if (starVertices.Count > 0)
            {
                foreach (var pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.LineList,
                        starVertices.ToArray(),
                        0,
                        starVertices.Count / 2
                    );
                }
            }
        }

        private void DrawStaticMeshWireframes(GameTime gameTime, Camera camera)
        {
            var graphicsDevice = Globals.screenManager.GraphicsDevice;

            // Create a basic effect for wireframe rendering
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.View = camera.View;
            basicEffect.Projection = camera.Projection;
            basicEffect.World = Matrix.Identity;

            // Draw wireframes for each static mesh
            for (int meshIndex = 0; meshIndex < loungeBepuMeshes.Count; meshIndex++)
            {
                if (meshIndex < meshTriangleVertices.Count && meshIndex < staticMeshTransforms.Count)
                {
                    var triangleVertices = meshTriangleVertices[meshIndex];
                    var (position, rotation) = staticMeshTransforms[meshIndex];

                    if (triangleVertices != null && triangleVertices.Count > 0)
                    {
                        // Create transform matrix for this static mesh
                        var worldMatrix = Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(position);

                        // Create wireframe edges from triangles
                        var wireframeVertices = new List<VertexPositionColor>();

                        for (int i = 0; i < triangleVertices.Count; i += 3)
                        {
                            if (i + 2 < triangleVertices.Count)
                            {
                                // Apply world transform to vertices
                                var v1 = Vector3.Transform(triangleVertices[i], worldMatrix);
                                var v2 = Vector3.Transform(triangleVertices[i + 1], worldMatrix);
                                var v3 = Vector3.Transform(triangleVertices[i + 2], worldMatrix);

                                // Create the three edges of the triangle
                                // Edge 1: v1 to v2
                                wireframeVertices.Add(new VertexPositionColor(v1, Color.Yellow));
                                wireframeVertices.Add(new VertexPositionColor(v2, Color.Yellow));

                                // Edge 2: v2 to v3
                                wireframeVertices.Add(new VertexPositionColor(v2, Color.Yellow));
                                wireframeVertices.Add(new VertexPositionColor(v3, Color.Yellow));

                                // Edge 3: v3 to v1
                                wireframeVertices.Add(new VertexPositionColor(v3, Color.Yellow));
                                wireframeVertices.Add(new VertexPositionColor(v1, Color.Yellow));
                            }
                        }

                        if (wireframeVertices.Count > 0)
                        {
                            try
                            {
                                // Apply the effect and draw the wireframe lines
                                basicEffect.CurrentTechnique.Passes[0].Apply();
                                graphicsDevice.DrawUserPrimitives(
                                    PrimitiveType.LineList,
                                    wireframeVertices.ToArray(),
                                    0,
                                    wireframeVertices.Count / 2);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error drawing wireframe: {ex.Message}");
                            }
                        }
                    }
                }
            }

            basicEffect.Dispose();
        }

        private void DrawAlienWireframe(GameTime gameTime, Camera camera)
        {
            if (alienCharacter?.Model == null) return;

            var graphicsDevice = Globals.screenManager.GraphicsDevice;
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.View = camera.View;
            basicEffect.Projection = camera.Projection;
            basicEffect.World = Matrix.Identity;

            var purpleColor = Color.Purple;
            var worldMatrix = alienCharacter.GetWorldMatrix();

            // Draw bounding box
            var boundingBox = GetModelBoundingBox(alienCharacter.Model, worldMatrix);
            DrawBoundingBox(boundingBox, basicEffect, graphicsDevice, purpleColor);

            // Draw mesh wireframe
            DrawModelWireframe(alienCharacter.Model, worldMatrix, basicEffect, graphicsDevice, purpleColor);

            basicEffect.Dispose();
        }

        private Microsoft.Xna.Framework.BoundingBox GetModelBoundingBox(Model model, Matrix worldMatrix)
        {
            // Initialize with max/min values
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    // Get vertex data
                    var vertexBuffer = part.VertexBuffer;
                    var vertexDeclaration = vertexBuffer.VertexDeclaration;
                    var vertexSize = vertexDeclaration.VertexStride;
                    var vertexCount = part.NumVertices;

                    byte[] vertexData = new byte[vertexCount * vertexSize];
                    vertexBuffer.GetData(
                        part.VertexOffset * vertexSize,
                        vertexData,
                        0,
                        vertexCount * vertexSize);

                    // Find position element offset
                    int positionOffset = 0;
                    foreach (var element in vertexDeclaration.GetVertexElements())
                    {
                        if (element.VertexElementUsage == VertexElementUsage.Position)
                        {
                            positionOffset = element.Offset;
                            break;
                        }
                    }

                    // Extract positions and transform them
                    Matrix meshTransform = transforms[mesh.ParentBone.Index] * worldMatrix;
                    for (int i = 0; i < vertexCount; i++)
                    {
                        Vector3 position = new Vector3(
                            BitConverter.ToSingle(vertexData, i * vertexSize + positionOffset),
                            BitConverter.ToSingle(vertexData, i * vertexSize + positionOffset + 4),
                            BitConverter.ToSingle(vertexData, i * vertexSize + positionOffset + 8));

                        Vector3 transformedPosition = Vector3.Transform(position, meshTransform);

                        min = Vector3.Min(min, transformedPosition);
                        max = Vector3.Max(max, transformedPosition);
                    }
                }
            }

            return new Microsoft.Xna.Framework.BoundingBox(min, max);
        }

        private void DrawBoundingBox(Microsoft.Xna.Framework.BoundingBox box, BasicEffect effect, GraphicsDevice graphicsDevice, Color color)
        {
            Vector3[] corners = box.GetCorners();
            var vertices = new List<VertexPositionColor>();

            // Bottom face
            vertices.Add(new VertexPositionColor(corners[0], color));
            vertices.Add(new VertexPositionColor(corners[1], color));
            vertices.Add(new VertexPositionColor(corners[1], color));
            vertices.Add(new VertexPositionColor(corners[2], color));
            vertices.Add(new VertexPositionColor(corners[2], color));
            vertices.Add(new VertexPositionColor(corners[3], color));
            vertices.Add(new VertexPositionColor(corners[3], color));
            vertices.Add(new VertexPositionColor(corners[0], color));

            // Top face
            vertices.Add(new VertexPositionColor(corners[4], color));
            vertices.Add(new VertexPositionColor(corners[5], color));
            vertices.Add(new VertexPositionColor(corners[5], color));
            vertices.Add(new VertexPositionColor(corners[6], color));
            vertices.Add(new VertexPositionColor(corners[6], color));
            vertices.Add(new VertexPositionColor(corners[7], color));
            vertices.Add(new VertexPositionColor(corners[7], color));
            vertices.Add(new VertexPositionColor(corners[4], color));

            // Vertical edges
            vertices.Add(new VertexPositionColor(corners[0], color));
            vertices.Add(new VertexPositionColor(corners[4], color));
            vertices.Add(new VertexPositionColor(corners[1], color));
            vertices.Add(new VertexPositionColor(corners[5], color));
            vertices.Add(new VertexPositionColor(corners[2], color));
            vertices.Add(new VertexPositionColor(corners[6], color));
            vertices.Add(new VertexPositionColor(corners[3], color));
            vertices.Add(new VertexPositionColor(corners[7], color));

            effect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.DrawUserPrimitives(
                PrimitiveType.LineList,
                vertices.ToArray(),
                0,
                vertices.Count / 2);
        }

        private void DrawModelWireframe(Model model, Matrix worldMatrix, BasicEffect effect, GraphicsDevice graphicsDevice, Color color)
        {
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                Matrix meshWorld = transforms[mesh.ParentBone.Index] * worldMatrix;

                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    // Get vertex and index data
                    var vertexBuffer = part.VertexBuffer;
                    var indexBuffer = part.IndexBuffer;
                    var vertexDeclaration = vertexBuffer.VertexDeclaration;
                    var vertexSize = vertexDeclaration.VertexStride;

                    // Read vertex data
                    byte[] vertexData = new byte[part.NumVertices * vertexSize];
                    vertexBuffer.GetData(
                        part.VertexOffset * vertexSize,
                        vertexData,
                        0,
                        part.NumVertices * vertexSize);

                    // Find position offset
                    int positionOffset = 0;
                    foreach (var element in vertexDeclaration.GetVertexElements())
                    {
                        if (element.VertexElementUsage == VertexElementUsage.Position)
                        {
                            positionOffset = element.Offset;
                            break;
                        }
                    }

                    // Read positions
                    Vector3[] positions = new Vector3[part.NumVertices];
                    for (int i = 0; i < part.NumVertices; i++)
                    {
                        positions[i] = new Vector3(
                            BitConverter.ToSingle(vertexData, i * vertexSize + positionOffset),
                            BitConverter.ToSingle(vertexData, i * vertexSize + positionOffset + 4),
                            BitConverter.ToSingle(vertexData, i * vertexSize + positionOffset + 8));
                    }

                    // Read index data
                    var indexElementSize = indexBuffer.IndexElementSize == IndexElementSize.SixteenBits ? 2 : 4;
                    byte[] indexData = new byte[part.PrimitiveCount * 3 * indexElementSize];
                    indexBuffer.GetData(
                        part.StartIndex * indexElementSize,
                        indexData,
                        0,
                        part.PrimitiveCount * 3 * indexElementSize);

                    // Build wireframe lines
                    var wireframeVertices = new List<VertexPositionColor>();
                    for (int i = 0; i < part.PrimitiveCount * 3; i += 3)
                    {
                        int idx0, idx1, idx2;
                        if (indexElementSize == 2)
                        {
                            idx0 = BitConverter.ToUInt16(indexData, i * 2) - part.VertexOffset;
                            idx1 = BitConverter.ToUInt16(indexData, (i + 1) * 2) - part.VertexOffset;
                            idx2 = BitConverter.ToUInt16(indexData, (i + 2) * 2) - part.VertexOffset;
                        }
                        else
                        {
                            idx0 = BitConverter.ToInt32(indexData, i * 4) - part.VertexOffset;
                            idx1 = BitConverter.ToInt32(indexData, (i + 1) * 4) - part.VertexOffset;
                            idx2 = BitConverter.ToInt32(indexData, (i + 2) * 4) - part.VertexOffset;
                        }

                        if (idx0 >= 0 && idx0 < positions.Length &&
                            idx1 >= 0 && idx1 < positions.Length &&
                            idx2 >= 0 && idx2 < positions.Length)
                        {
                            Vector3 v0 = Vector3.Transform(positions[idx0], meshWorld);
                            Vector3 v1 = Vector3.Transform(positions[idx1], meshWorld);
                            Vector3 v2 = Vector3.Transform(positions[idx2], meshWorld);

                            // Three edges of the triangle
                            wireframeVertices.Add(new VertexPositionColor(v0, color));
                            wireframeVertices.Add(new VertexPositionColor(v1, color));
                            wireframeVertices.Add(new VertexPositionColor(v1, color));
                            wireframeVertices.Add(new VertexPositionColor(v2, color));
                            wireframeVertices.Add(new VertexPositionColor(v2, color));
                            wireframeVertices.Add(new VertexPositionColor(v0, color));
                        }
                    }

                    if (wireframeVertices.Count > 0)
                    {
                        try
                        {
                            effect.CurrentTechnique.Passes[0].Apply();
                            graphicsDevice.DrawUserPrimitives(
                                PrimitiveType.LineList,
                                wireframeVertices.ToArray(),
                                0,
                                wireframeVertices.Count / 2);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error drawing alien wireframe: {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draws the bartender's interaction collider box
        /// </summary>
        private void DrawBartenderCollider(Camera camera)
        {
            if (bartenderCharacterInteraction == null) return;

            var graphicsDevice = Globals.screenManager.GraphicsDevice;
            var basicEffect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                View = camera.View,
                Projection = camera.Projection,
                World = Matrix.Identity
            };

            // Check if bartender is being targeted
            bool isTargeted = interactionSystem?.CurrentTarget == bartenderCharacterInteraction;
            Color colliderColor = isTargeted ? Color.Yellow : Color.Cyan;

            // Use the exact same dimensions and position as the physics collider
            DrawDebugBox(bartenderColliderCenter, bartenderColliderWidth, bartenderColliderHeight,
                bartenderColliderDepth, colliderColor, basicEffect, graphicsDevice);

            basicEffect.Dispose();
        }

        /// <summary>
        /// Draws the pathologist's interaction collider box
        /// </summary>
        private void DrawPathologistCollider(Camera camera)
        {
            if (pathologistCharacterInteraction == null) return;

            var graphicsDevice = Globals.screenManager.GraphicsDevice;
            var basicEffect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                View = camera.View,
                Projection = camera.Projection,
                World = Matrix.Identity
            };

            // Check if pathologist is being targeted
            bool isTargeted = interactionSystem?.CurrentTarget == pathologistCharacterInteraction;
            Color colliderColor = isTargeted ? Color.Yellow : Color.Magenta;

            // Use the exact same dimensions and position as the physics collider
            DrawDebugBox(pathologistColliderCenter, pathologistColliderWidth, pathologistColliderHeight,
                pathologistColliderDepth, colliderColor, basicEffect, graphicsDevice);

            basicEffect.Dispose();
        }

        /// <summary>
        /// Draws a debug box wireframe
        /// </summary>
        private void DrawDebugBox(Vector3 center, float width, float height, float depth, Color color, BasicEffect effect, GraphicsDevice graphicsDevice)
        {
            var vertices = new List<VertexPositionColor>();

            // Calculate half extents
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;
            float halfDepth = depth / 2f;

            // Define 8 corners of the box
            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-halfWidth, -halfHeight, -halfDepth);
            corners[1] = center + new Vector3(halfWidth, -halfHeight, -halfDepth);
            corners[2] = center + new Vector3(halfWidth, -halfHeight, halfDepth);
            corners[3] = center + new Vector3(-halfWidth, -halfHeight, halfDepth);
            corners[4] = center + new Vector3(-halfWidth, halfHeight, -halfDepth);
            corners[5] = center + new Vector3(halfWidth, halfHeight, -halfDepth);
            corners[6] = center + new Vector3(halfWidth, halfHeight, halfDepth);
            corners[7] = center + new Vector3(-halfWidth, halfHeight, halfDepth);

            // Bottom face edges
            vertices.Add(new VertexPositionColor(corners[0], color));
            vertices.Add(new VertexPositionColor(corners[1], color));
            vertices.Add(new VertexPositionColor(corners[1], color));
            vertices.Add(new VertexPositionColor(corners[2], color));
            vertices.Add(new VertexPositionColor(corners[2], color));
            vertices.Add(new VertexPositionColor(corners[3], color));
            vertices.Add(new VertexPositionColor(corners[3], color));
            vertices.Add(new VertexPositionColor(corners[0], color));

            // Top face edges
            vertices.Add(new VertexPositionColor(corners[4], color));
            vertices.Add(new VertexPositionColor(corners[5], color));
            vertices.Add(new VertexPositionColor(corners[5], color));
            vertices.Add(new VertexPositionColor(corners[6], color));
            vertices.Add(new VertexPositionColor(corners[6], color));
            vertices.Add(new VertexPositionColor(corners[7], color));
            vertices.Add(new VertexPositionColor(corners[7], color));
            vertices.Add(new VertexPositionColor(corners[4], color));

            // Vertical edges
            vertices.Add(new VertexPositionColor(corners[0], color));
            vertices.Add(new VertexPositionColor(corners[4], color));
            vertices.Add(new VertexPositionColor(corners[1], color));
            vertices.Add(new VertexPositionColor(corners[5], color));
            vertices.Add(new VertexPositionColor(corners[2], color));
            vertices.Add(new VertexPositionColor(corners[6], color));
            vertices.Add(new VertexPositionColor(corners[3], color));
            vertices.Add(new VertexPositionColor(corners[7], color));

            if (vertices.Count > 0)
            {
                try
                {
                    effect.CurrentTechnique.Passes[0].Apply();
                    graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.LineList,
                        vertices.ToArray(),
                        0,
                        vertices.Count / 2);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error drawing bartender collider: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Draws debug visualization for interaction camera positions
        /// </summary>
        private void DrawInteractionDebugVisualization(Camera camera)
        {
            if (bartenderCharacterInteraction == null) return;

            var graphicsDevice = Globals.screenManager.GraphicsDevice;
            var basicEffect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                View = camera.View,
                Projection = camera.Projection,
                World = Matrix.Identity
            };

            var vertices = new List<VertexPositionColor>();

            // Draw sphere at bartender position (Green)
            DrawDebugSphere(bartenderCharacterInteraction.Position, 5f, Color.Green, vertices);

            // Draw sphere at interaction camera position (Cyan)
            DrawDebugSphere(bartenderCharacterInteraction.CameraInteractionPosition, 5f, Color.Cyan, vertices);

            // Draw line from camera position to look-at position (Yellow)
            vertices.Add(new VertexPositionColor(bartenderCharacterInteraction.CameraInteractionPosition, Color.Yellow));
            vertices.Add(new VertexPositionColor(bartenderCharacterInteraction.CameraInteractionLookAt, Color.Yellow));

            // Draw sphere at look-at position (Magenta)
            DrawDebugSphere(bartenderCharacterInteraction.CameraInteractionLookAt, 3f, Color.Magenta, vertices);

            // Draw evidence table grid
            DrawEvidenceTableGrid(vertices);

            if (vertices.Count > 0)
            {
                try
                {
                    basicEffect.CurrentTechnique.Passes[0].Apply();
                    graphicsDevice.DrawUserPrimitives(
                        PrimitiveType.LineList,
                        vertices.ToArray(),
                        0,
                        vertices.Count / 2);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error drawing interaction debug: {ex.Message}");
                }
            }

            basicEffect.Dispose();
        }

        /// <summary>
        /// Helper to draw a debug sphere using lines
        /// </summary>
        private void DrawDebugSphere(Vector3 center, float radius, Color color, List<VertexPositionColor> vertices)
        {
            const int segments = 16;
            float angleStep = Microsoft.Xna.Framework.MathHelper.TwoPi / segments;

            // Draw three circles (XY, XZ, YZ planes)
            // XY plane
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;

                Vector3 p1 = center + new Vector3(
                    (float)Math.Cos(angle1) * radius,
                    (float)Math.Sin(angle1) * radius,
                    0);
                Vector3 p2 = center + new Vector3(
                    (float)Math.Cos(angle2) * radius,
                    (float)Math.Sin(angle2) * radius,
                    0);

                vertices.Add(new VertexPositionColor(p1, color));
                vertices.Add(new VertexPositionColor(p2, color));
            }

            // XZ plane
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;

                Vector3 p1 = center + new Vector3(
                    (float)Math.Cos(angle1) * radius,
                    0,
                    (float)Math.Sin(angle1) * radius);
                Vector3 p2 = center + new Vector3(
                    (float)Math.Cos(angle2) * radius,
                    0,
                    (float)Math.Sin(angle2) * radius);

                vertices.Add(new VertexPositionColor(p1, color));
                vertices.Add(new VertexPositionColor(p2, color));
            }

            // YZ plane
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;

                Vector3 p1 = center + new Vector3(
                    0,
                    (float)Math.Cos(angle1) * radius,
                    (float)Math.Sin(angle1) * radius);
                Vector3 p2 = center + new Vector3(
                    0,
                    (float)Math.Cos(angle2) * radius,
                    (float)Math.Sin(angle2) * radius);

                vertices.Add(new VertexPositionColor(p1, color));
                vertices.Add(new VertexPositionColor(p2, color));
            }
        }

        /// <summary>
        /// Draw evidence table grid for debugging
        /// </summary>
        private void DrawEvidenceTableGrid(List<VertexPositionColor> vertices)
        {
            if (evidenceTable == null)
                return;

            var slots = evidenceTable.GetAllSlots();
            int rows = evidenceTable.GridRows;
            int cols = evidenceTable.GridColumns;

            // Draw grid lines and slot markers
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var slot = slots[row, col];
                    Vector3 pos = slot.Position;
                    Vector3 size = slot.Size;

                    // Determine color based on occupancy
                    Color slotColor = slot.IsOccupied ? Color.Red : Color.Green;

                    // Draw slot boundary as a box outline
                    float halfWidth = size.X / 2f;
                    float halfDepth = size.Z / 2f;
                    float y = pos.Y;

                    // Bottom rectangle (at table surface)
                    Vector3 bl = new Vector3(pos.X - halfWidth, y, pos.Z - halfDepth); // Bottom-left
                    Vector3 br = new Vector3(pos.X + halfWidth, y, pos.Z - halfDepth); // Bottom-right
                    Vector3 tl = new Vector3(pos.X - halfWidth, y, pos.Z + halfDepth); // Top-left
                    Vector3 tr = new Vector3(pos.X + halfWidth, y, pos.Z + halfDepth); // Top-right

                    // Draw rectangle edges
                    vertices.Add(new VertexPositionColor(bl, slotColor));
                    vertices.Add(new VertexPositionColor(br, slotColor));

                    vertices.Add(new VertexPositionColor(br, slotColor));
                    vertices.Add(new VertexPositionColor(tr, slotColor));

                    vertices.Add(new VertexPositionColor(tr, slotColor));
                    vertices.Add(new VertexPositionColor(tl, slotColor));

                    vertices.Add(new VertexPositionColor(tl, slotColor));
                    vertices.Add(new VertexPositionColor(bl, slotColor));

                    // Draw center marker (small cross)
                    float markerSize = 2f;
                    vertices.Add(new VertexPositionColor(pos + new Vector3(-markerSize, 0, 0), Color.Yellow));
                    vertices.Add(new VertexPositionColor(pos + new Vector3(markerSize, 0, 0), Color.Yellow));

                    vertices.Add(new VertexPositionColor(pos + new Vector3(0, 0, -markerSize), Color.Yellow));
                    vertices.Add(new VertexPositionColor(pos + new Vector3(0, 0, markerSize), Color.Yellow));
                }
            }

            // Draw table boundary
            var tableBounds = evidenceTable.GetTableBounds();
            DrawDebugBox(evidenceTable.TableCenter, evidenceTable.TableSize.X, evidenceTable.TableSize.Y,
                evidenceTable.TableSize.Z, Color.Cyan, null, null, vertices);
        }

        /// <summary>
        /// Overload DrawDebugBox to work with vertex list for grid drawing
        /// </summary>
        private void DrawDebugBox(Vector3 center, float width, float height, float depth, Color color,
            BasicEffect effect, GraphicsDevice graphicsDevice, List<VertexPositionColor> vertices)
        {
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;
            float halfDepth = depth / 2f;

            // Define 8 corners of the box
            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-halfWidth, -halfHeight, -halfDepth);
            corners[1] = center + new Vector3(halfWidth, -halfHeight, -halfDepth);
            corners[2] = center + new Vector3(halfWidth, halfHeight, -halfDepth);
            corners[3] = center + new Vector3(-halfWidth, halfHeight, -halfDepth);
            corners[4] = center + new Vector3(-halfWidth, -halfHeight, halfDepth);
            corners[5] = center + new Vector3(halfWidth, -halfHeight, halfDepth);
            corners[6] = center + new Vector3(halfWidth, halfHeight, halfDepth);
            corners[7] = center + new Vector3(-halfWidth, halfHeight, halfDepth);

            // Define the 12 edges of the box
            int[][] edges = new int[][]
            {
                new int[] {0, 1}, new int[] {1, 2}, new int[] {2, 3}, new int[] {3, 0}, // Bottom face
                new int[] {4, 5}, new int[] {5, 6}, new int[] {6, 7}, new int[] {7, 4}, // Top face
                new int[] {0, 4}, new int[] {1, 5}, new int[] {2, 6}, new int[] {3, 7}  // Vertical edges
            };

            // Add edges to vertex list
            foreach (var edge in edges)
            {
                vertices.Add(new VertexPositionColor(corners[edge[0]], color));
                vertices.Add(new VertexPositionColor(corners[edge[1]], color));
            }
        }

        // Utility methods for external access
        public bool IsCharacterActive() => characterActive;
        public CharacterInput? GetCharacter() => character;
        public Quaternion GetCharacterInitialRotation() => characterInitialRotation;
        public InteractableCharacter GetBartender() => bartenderCharacterInteraction;
        public InteractableCharacter GetPathologist() => pathologistCharacterInteraction;
        public InteractableItem GetEvidenceVial() => evidenceVial;
        public CrimeSceneFile GetCrimeSceneFile() => crimeSceneFile;
        public EvidenceTable GetEvidenceTable() => evidenceTable;
        public InteractionSystem GetInteractionSystem() => interactionSystem;
        public bool IsShowingIntroText() => showIntroText;
        public Dictionary<string, Texture2D> GetCharacterPortraits() => characterPortraits;

        public void SetActiveDialogueCharacter(string characterKey)
        {
            activeDialogueCharacter = characterKey;
        }

        public void ClearActiveDialogueCharacter()
        {
            activeDialogueCharacter = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // BepuPhysics meshes are cleaned up by the physics system
                loungeBepuMeshes.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
