using anakinsoft.entities;
using anakinsoft.system;
using anakinsoft.system.character;
using anakinsoft.system.physics;
using anakinsoft.utilities;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;
using Demos.Demos.Characters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NUnit.Framework.Constraints;
using rubens_psx_engine;
using rubens_psx_engine.chain;
using rubens_psx_engine.entities;
using rubens_psx_engine.Extensions;
using rubens_psx_engine.system.config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using static rubens_psx_engine.chain.ChainUtilities;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Vector3N = System.Numerics.Vector3;

namespace anakinsoft.game.scenes
{
    /// <summary>
    /// Corridor scene with multi-material corridor_single model and FPS character controller
    /// </summary>
    public class CorridorScene : Scene
    {
        // Character system
        CharacterControllers characters;
        CharacterInput? character;
        bool characterActive;
        float characterLoadDelay = 1.0f; // Delay in seconds before character physics activate
        float timeSinceLoad = 0f;
        Quaternion characterInitialRotation = Quaternion.Identity; // Initial camera rotation

        // Multi-material corridor entity
        //MultiMaterialRenderingEntity corridorEntity;
        
        // Direct BepuPhysics meshes for corridors
        List<Mesh> corridorBepuMeshes;
        
        // Entity collections
        List<PhysicsEntity> bullets;
        PhysicsEntity ground;
        List<DoorEntity> doors;
        List<InteractableDoorEntity> interactiveDoors;

        // Interaction system
        InteractionSystem interactionSystem;

        // Input handling
        bool mouseClick = false;
        KeyboardState previousKeyboard;
        
        // Collision mesh wireframe data
        List<List<Vector3>> meshTriangleVertices; // Store triangle vertices for wireframe
        List<(Vector3 position, Quaternion rotation)> staticMeshTransforms; // Store mesh transforms

        public CorridorScene() : base()
        {
            // Initialize character system and physics
            characters = null; // Will be initialized in physics system
            physicsSystem = new PhysicsSystem(ref characters);
            
            bullets = new List<PhysicsEntity>();
            doors = new List<DoorEntity>();
            interactiveDoors = new List<InteractableDoorEntity>();
            corridorBepuMeshes = new List<Mesh>();
            meshTriangleVertices = new List<List<Vector3>>();
            staticMeshTransforms = new List<(Vector3 position, Quaternion rotation)>();

            // Initialize interaction system
            interactionSystem = new InteractionSystem(physicsSystem);
            
            // Set black background for corridor scene
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
            float intervals = 20;



            // Load CHAIN stuff

            List<DoorMapping> doorMappings = new List<DoorMapping>()
            {
                new(doorId: "AC1",
                    startLocaion: new Vector3(0, -3.5f, 36) * intervals,
                    startRotation: Quaternion.Identity),
                new(doorId: "AC2",
                    startLocaion: new Vector3(6.2873f, 3f, -140.26f) * intervals,
                    startRotation: Quaternion.CreateFromYawPitchRoll(86,0,0)),
                new(doorId: "AA1",
                    startLocaion: new Vector3(25.825f, 3f, -139.56f) * intervals,
                    startRotation: Quaternion.CreateFromYawPitchRoll(-86,0,0)),
            };

            var entryDoor = ChainUtilities.GetSceneFromDoorFile(doorMappings, "AC1");

            // Create character with initial orientation based on the starting door
            CreateCharacter(entryDoor.startlocation , entryDoor.startRotation); // Start at back of corridor with orientation

            float jitter = 3;
            var affine = 0;
            var wall = new UnlitMaterial("textures/corridor_wall");
            wall.VertexJitterAmount = jitter;
            wall.Brightness = 1.2f; // Slightly darker
            wall.AffineAmount = affine;

            var floor = new UnlitMaterial("textures/floor_1");
            floor.VertexJitterAmount = jitter;
            floor.AffineAmount = affine;
            floor.Brightness = 1.2f; // Brighter
            //material2.LightDirection = Vector3.Normalize(new Vector3(0.5f, -1, 0.3f));

            var placeholder = new VertexLitMaterial("textures/prototype/wood");
            placeholder.VertexJitterAmount = jitter;
            placeholder.AffineAmount = affine;
            placeholder.AmbientColor = new Vector3(.7f, .7f, .7f);
            //placeholder.Brightness = 1.0f; // Brighter
            var placeholder2 = new VertexLitMaterial("textures/prototype/concrete");
            placeholder.VertexJitterAmount = jitter;
            placeholder.AffineAmount = affine;
            placeholder.AmbientColor = new Vector3(.7f, .7f, .7f);

            // Create door materials
            var doorMat = new UnlitMaterial("textures/door");
            doorMat.Brightness = 1.0f;
            doorMat.AffineAmount = 0;
            doorMat.VertexJitterAmount = 3;

            var frameMat = new UnlitMaterial("textures/door_frame");
            frameMat.Brightness = 1.0f;
            frameMat.AffineAmount = 0;
            frameMat.VertexJitterAmount = 3;

            var doorMat_2 = new UnlitMaterial("textures/prototype/prototype_512x512_orange");
            doorMat.Brightness = 1.0f;
            doorMat.AffineAmount = 0;
            doorMat.VertexJitterAmount = 3;
            var doorMat_2A = new UnlitMaterial("textures/prototype/prototype_512x512_cyan");
            doorMat.Brightness = 1.0f;
            doorMat.AffineAmount = 0;
            doorMat.VertexJitterAmount = 3;

            var doorMat_2B = new UnlitMaterial("textures/prototype/prototype_512x512_green1");
            doorMat.Brightness = 1.0f;
            doorMat.AffineAmount = 0;
            doorMat.VertexJitterAmount = 3;

            var frameMat_2 = new VertexLitMaterial("textures/prototype/concrete");
            frameMat.Brightness = .9f;
            frameMat.AffineAmount = 0;
            frameMat.VertexJitterAmount = 3;

            var rotateLevel = QuaternionExtensions.CreateFromYawPitchRollDegrees(180, 0, 0);

            // Lower Corridors
            CreateCorridorWithMaterialsAndPhysics(new Vector3(0, -4, 16) * intervals,
                Quaternion.Identity, wall, floor);

            CreateCorridorWithMaterialsAndPhysics(new Vector3(0, -4, 8) * intervals,
                Quaternion.Identity, wall, floor);

            CreateCorridorWithMaterialsAndPhysics(new Vector3(0, -4, 0) * intervals,
                Quaternion.Identity, wall, floor);

            CreateCorridorSlope(new Vector3(0, -4, -8) * intervals,
                rotateLevel, wall, floor);

            CreateCorridorSlope(new Vector3(0, -2, -16) * intervals,
                rotateLevel, wall, floor);

            CreateCorridorWithMaterialsAndPhysics(new Vector3(0, 0, -24) * intervals,
                Quaternion.Identity, wall, floor);

            // Upper corridors
            CreateStaticMesh(new Vector3(0, 0, -32) * intervals,
                rotateLevel, [wall, floor], "models/level/corridor_T_shape");
            CreateStaticMesh(new Vector3(-8, 0, -32) * intervals,
                Quaternion.Identity, [wall, floor], "models/level/corridor_T_shape");
            CreateStaticMesh(new Vector3(8, 0, -32) * intervals,
                Quaternion.Identity, [wall, floor], "models/level/corridor_T_shape");

            CreateStaticMesh(new Vector3(-16, 0, -32) * intervals,
                Quaternion.Identity, [wall, floor], "models/level/corridor_X_shape");


            // Main Engines
            CreateStaticMesh(new Vector3(0, -4, 28) * intervals,
                rotateLevel, [placeholder], "models/level/EngineRoom_1");

            CreatePhysicsMesh("models/level/EngineRoom_Railing", new Vector3(0, -4, 28) * intervals, rotateLevel,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, -90, 0), Vector3.Zero);

            CreatePhysicsMesh("models/level/EngineRoom_Railing_2", new Vector3(0, -4, 28) * intervals, rotateLevel,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, -90, 0), Vector3.Zero);

            CreateStaticMesh(new Vector3(0, -4, 28) * intervals,
                rotateLevel, [placeholder], "models/level/EngineRoom_2");

            // Main Facilities
            CreateStaticMesh(new Vector3(16, 0, -28) * intervals,
                rotateLevel, [placeholder], "models/level/medical_room");
            CreateStaticMesh(new Vector3(8, 0, -43) * intervals,
                rotateLevel, [placeholder], "models/level/crew_quarters");
            CreateStaticMesh(new Vector3(-9, 0, -54) * intervals,
                rotateLevel, [placeholder], "models/level/bathroom");
            CreateStaticMesh(new Vector3(-20, 0, -28) * intervals,
                rotateLevel, [placeholder], "models/level/lab");
            CreateStaticMesh(new Vector3(-16, -2, -48) * intervals,
                rotateLevel, [placeholder], "models/level/arborium");

            //bridge corridor
            CreateStaticMesh(new Vector3(-16, 0, -78) * intervals,
                Quaternion.Identity, [wall, floor], "models/level/corridor_X_shape");

            CreateStaticMesh(new Vector3(-16, -2, -48) * intervals,
                rotateLevel, [placeholder], "models/level/arborium");

            CreatePhysicsMesh("models/level/arborium_railing", new Vector3(-16, -2, -48) * intervals, rotateLevel,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, -90, 0), Vector3.Zero);

            //CreateStaticRoom(new Vector3(-16, -2, -48) * intervals,
            //    rotateLevel, [placeholder], "models/level/arborium");

            CreateStaticMesh(new Vector3(-24, 0, -78) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0),
                [wall, floor, wall], "models/level/corridor");
            CreateStaticMesh(new Vector3(-32, 0, -78) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0),
                [wall, floor, wall], "models/level/corridor");
            CreateStaticMesh(new Vector3(-8, 0, -78) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0),
                [wall, floor, wall],  "models/level/corridor");

            //CreateCorridorWithMaterialsAndPhysics(new Vector3(-24, 0, -78) * intervals,
            //    Quaternion.CreateFromYawPitchRoll(0, 0, 0), wall, floor);
            //CreateCorridorWithMaterialsAndPhysics(new Vector3(-32, 0, -78) * intervals,
            //    Quaternion.CreateFromYawPitchRoll(0, 0, 0), wall, floor);
            //CreateCorridorWithMaterialsAndPhysics(new Vector3(-8, 0, -78) * intervals,
            //    Quaternion.CreateFromYawPitchRoll(0, 0, 0), wall, floor);

            CreateStaticMesh(new Vector3(-4, 0, -78) * intervals,
                rotateLevel, [placeholder], "models/level/curved_hall");
            CreateCorridorWithMaterialsAndPhysics(new Vector3(16, 0, -102) * intervals,
                Quaternion.Identity, wall, floor);
            CreateCorridorSlope(new Vector3(16, 0, -110) * intervals,
                rotateLevel, wall, floor);
            CreateCorridorSlope(new Vector3(16, 2, -118) * intervals,
                rotateLevel, wall, floor);

            // bridge
            CreateStaticMesh(new Vector3(16, 4, -139) * intervals,
                rotateLevel, [placeholder], "models/level/bridge_prep_rooms");
            CreateStaticMesh(new Vector3(16, 4, -139) * intervals,
                rotateLevel, [placeholder], "models/level/bridge");
            CreateStaticMesh(new Vector3(16, 4, -126) * intervals,
                Quaternion.Identity, [wall, floor], "models/level/corridor_X_shape");
            // Create physics ground for collision (visible for testing)
            //CreatePhysicsGround();

            // Create test cube in the middle of the scene
            //CreateTestCube();
            MakeDoors(intervals, doorMat, frameMat);

            MakeInteractiveDoor(intervals, doorMat_2, frameMat_2, doorMat_2A, doorMat_2B);

            // medical furnishings
            CreateStaticMesh(new Vector3(22, 0, -33) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(180,0,0), [placeholder2], "models/props/medical_bed");
            CreateStaticMesh(new Vector3(22, 0, -28) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(180, 0, 0), [placeholder2], "models/props/medical_bed");
            CreateStaticMesh(new Vector3(22, 0, -23) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(180, 0, 0), [placeholder2], "models/props/medical_bed");
            CreateStaticMesh(new Vector3(22, 0, -18) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(180, 0, 0), [placeholder2], "models/props/medical_bed");

            CreateStaticMesh(new Vector3(24, 0, -35) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/medical_structure");
            CreateStaticMesh(new Vector3(24, 0, -30) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/medical_structure");
            CreateStaticMesh(new Vector3(24, 0, -25) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/medical_structure");
            CreateStaticMesh(new Vector3(24, 0, -20) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/medical_structure");
            CreateStaticMesh(new Vector3(24, 0, -15) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/medical_structure");

            CreateStaticMesh(new Vector3(13, 0, -27) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(180, 0, 0), [placeholder2], "models/props/medical_desk");

            // crew quarters beds
            CreateStaticMesh(new Vector3(15, 0, -39) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(15, 0, -43) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(15, 0, -47) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(15, 0, -51) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(15, 0, -55) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(15, 0, -59) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(15, 0, -63) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(15, 0, -67) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/quarter_beds");

            CreateStaticMesh(new Vector3(10, 0, -65) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(10, 0, -64) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(10, 0, -61) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(6, 0, -65) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(6, 0, -64) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(6, 0, -61) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/quarter_beds");

            CreateStaticMesh(new Vector3(10, 0, -50) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(10, 0, -46) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(6, 0, -50) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(6, 0, -46) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/quarter_beds");

            CreateStaticMesh(new Vector3(1, 0, -51) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(1, 0, -55) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(1, 0, -59) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(1, 0, -63) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0), [placeholder2], "models/props/quarter_beds");
            CreateStaticMesh(new Vector3(1, 0, -67) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0), [placeholder2], "models/props/quarter_beds");

            // bathroom
            CreateStaticMesh(new Vector3(-5, -0.1f, -45) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/bathroom_sink");
            CreateStaticMesh(new Vector3(-5, -0.1f, -44) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/bathroom_sink");
            CreateStaticMesh(new Vector3(-5, -0.1f, -43) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/bathroom_sink");
            CreateStaticMesh(new Vector3(-5, -0.1f, -42) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/bathroom_sink");
            CreateStaticMesh(new Vector3(-5, -0.1f, -41) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/bathroom_sink");

            CreateStaticMesh(new Vector3(-5.5f, -0.1f, -45) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0), [placeholder2], "models/props/bathroom_stall");
            CreateStaticMesh(new Vector3(-5.5f, -0.1f, -43) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0), [placeholder2], "models/props/bathroom_stall");
            CreateStaticMesh(new Vector3(-5.5f, -0.1f, -41) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0), [placeholder2], "models/props/bathroom_stall");

            CreateStaticMesh(new Vector3(-11f, -0.1f, -39) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/bathroom_shower");
            CreateStaticMesh(new Vector3(-11f, -0.1f, -41) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/bathroom_shower");
            CreateStaticMesh(new Vector3(-11f, -0.1f, -43) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/bathroom_shower");
            CreateStaticMesh(new Vector3(-11f, -0.1f, -45) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/bathroom_shower");
            CreateStaticMesh(new Vector3(-11f, -0.1f, -47) * intervals,
                 QuaternionExtensions.CreateFromYawPitchRollDegrees(-90, 0, 0), [placeholder2], "models/props/bathroom_shower");

            // laboratory

            CreateStaticMesh(new Vector3(-26, 0, -34) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0), [placeholder2], "models/props/lab_container");
            CreateStaticMesh(new Vector3(-26, 0, -28) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0), [placeholder2], "models/props/lab_container");
            CreateStaticMesh(new Vector3(-26, 0, -22) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0), [placeholder2], "models/props/lab_container");
        }

        private void MakeInteractiveDoor(float intervals,
            Material doorMat_2, Material frameMat_2, Material doorMat_2A, Material doorMat_2B)
        {
            CreateInteractiveDoor(new Vector3(0, -4, 49) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(180, 0, 0),
                "Door teleport to AC1", doorMat_2, frameMat_2,
                (door) =>
                {
                    ChainUtilities.ExitGame("AC1");
                }); //red

            CreateInteractiveDoor(new Vector3(4.7042f, 2.3f, -140.06f) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(180 - 97.559f, 0, 0),
                "Door teleport to AC2", doorMat_2A, frameMat_2,
                (door) =>
                {
                    ChainUtilities.ExitGame("AC2");
                }); //cyan

            CreateInteractiveDoor(new Vector3(27.347f, 2.3f, -139.97f) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(180 - 264.17f, 0, 0),
                "Door teleport to AA1", doorMat_2B, frameMat_2, (door) =>
                {
                    ChainUtilities.ExitGame("AA1");

                });//green
        }

        private void MakeDoors(float intervals, Material doorMat, Material frameMat)
        {

            // Door between first and second corridor sections
            CreateDoor(new Vector3(0, -4, 20) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0),
                doorMat, frameMat);
            CreateDoor(new Vector3(0, -4, 4) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0),
                doorMat, frameMat);
            CreateDoor(new Vector3(0, 0, -28) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0),
                doorMat, frameMat);

            // main area doors
            CreateDoor(new Vector3(8, 0, -36) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0),
                doorMat, frameMat);
            CreateDoor(new Vector3(-8, 0, -36) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0),
                doorMat, frameMat);

            // Create a locked door as an example
            var lockedDoor = CreateDoor(new Vector3(-16, 0, -36) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0),
                doorMat, frameMat); // well the AI says I should add a puzzle here, maybe I will lol
            //lockedDoor.Lock(); // This door starts locked

            CreateDoor(new Vector3(-16, 0, -28) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0),
                doorMat, frameMat);
            CreateDoor(new Vector3(-20, 0, -32) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0),
                doorMat, frameMat);
            CreateDoor(new Vector3(12, 0, -32) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0),
                doorMat, frameMat);

            // airlock area
            CreateDoor(new Vector3(-16, 0, -74) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0),
                doorMat, frameMat);

            var b2 = CreateDoor(new Vector3(-16, 0, -82) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0),
                doorMat, frameMat);
            b2.Lock();

            var b3 = CreateDoor(new Vector3(-36, 0, -78) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0),
                doorMat, frameMat);
            b3.Lock();

            CreateDoor(new Vector3(-4, 0, -78) * intervals,
               QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0),
               doorMat, frameMat);
            CreateDoor(new Vector3(16, 0, -98) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0),
                doorMat, frameMat);

            // bridge doors
            CreateDoor(new Vector3(16, 4, -122) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0),
                doorMat, frameMat);

            CreateDoor(new Vector3(16, 4, -129) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0),
                doorMat, frameMat);

            CreateDoor(new Vector3(20, 4, -126) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0),
                doorMat, frameMat);

            CreateDoor(new Vector3(12, 4, -126) * intervals,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0),
                doorMat, frameMat);


        }

        private DoorEntity CreateDoor(Vector3 position, Quaternion rotation, Material doorMat, Material frameMat)
        {
            return CreateDoorWithParameters(
                position, rotation,
                openSpeed: 1f,
                openHeight: 250f,
                triggerDistance: 60f,
                closeDelay: 2f,
                scale: new Vector3(0.2f),
                doorMaterial: doorMat,
                frameMaterial: frameMat
            );
        }

        private void CreateStaticMesh(Vector3 offset, Quaternion rotation, Material[] mats, string mesh, bool  flipPhys = false)
        {
            // Create three different materials for the corridor channels using actual texture files

            // Create corridor entity with three material channels
            var loadedMats = new Dictionary<int, Material>();
            for(int i = 0; i < mats.Length; i++)
            {
                loadedMats.Add(i, mats[i]);
            }
            var entity = new MultiMaterialRenderingEntity(mesh, loadedMats);

            entity.Position = Vector3.Zero + offset;
            entity.Scale = Vector3.One * .2f;
            entity.Rotation = rotation;
            entity.IsVisible = true;

            // Add to rendering entities
            AddRenderingEntity(entity);

            // Create physics mesh for the corridor
            CreatePhysicsMesh(mesh, offset, rotation, 
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, -90, 0), Vector3.Zero);
        }


        private void CreateCorridorSlope(Vector3 offset, Quaternion rotation, Material wall, Material floor)
        {
            var mesh = "models/level/corridor_slope";
            // Create three different materials for the corridor channels using actual texture files
    

            // Create corridor entity with three material channels
             var corridorEntity = new MultiMaterialRenderingEntity(mesh,
                new Dictionary<int, Material>
                {
                    { 0, wall }, // Floor/walls
                    { 1, floor }, // Architectural details
                    { 2, wall }  // Decorative elements
                });

            corridorEntity.Position = Vector3.Zero + offset;
            corridorEntity.Scale = Vector3.One * .2f;
            corridorEntity.Rotation = rotation;
            corridorEntity.IsVisible = true;

            // Add to rendering entities
            AddRenderingEntity(corridorEntity);

            // Create physics mesh for the corridor
            CreatePhysicsMesh(mesh, offset, rotation, QuaternionExtensions.CreateFromYawPitchRollDegrees(0, -90, 0), Vector3.Zero);
        }


        private void CreateCorridorWithMaterialsAndPhysics(Vector3 offset, Quaternion rotation, Material wall, Material floor)
        {
            var mesh = "models/corridor_single";

            // Create corridor entity with three material channels
            var corridorEntity = new MultiMaterialRenderingEntity(mesh, 
                new Dictionary<int, Material>
                {
                    { 0, floor }, // Floor/walls
                    { 1, wall }, // Architectural details
                    { 2, wall }  // Decorative elements
                });

            corridorEntity.Position = Vector3.Zero + offset; 
            corridorEntity.Scale = Vector3.One * .2f;
            corridorEntity.IsVisible = true;
            
            // Add to rendering entities
            AddRenderingEntity(corridorEntity);

            // Create physics mesh for the corridor
            CreatePhysicsMesh(mesh, offset, rotation, QuaternionExtensions.CreateFromYawPitchRollDegrees(0, -90, 0), Vector3.Zero);
        }


        private void CreatePhysicsMesh(string mesh, Vector3 offset, Quaternion rotation,
            Quaternion rotationOffset, Vector3 physicsMeshOffset)
        {
            try
            {
                // Load the same model used for rendering
                var corridorModel = Globals.screenManager.Content.Load<Model>(mesh);
                
                // Use consistent scaling approach: visual scale * physics scale factor
                // Corridor uses visual scale of 0.1f, so physics scale = 0.1f * 10 = 1.0f
                var visualScale = Vector3.One * 0.2f; // Same scale as rendering entity
                var physicsScale = visualScale * 100; // Apply our learned scaling factor

                // Extract triangles and wireframe vertices for rendering
                var (triangles, wireframeVertices) = BepuMeshExtractor.ExtractTrianglesFromModel(corridorModel, physicsScale, physicsSystem);
                var bepuMesh = new Mesh(triangles, Vector3.One.ToVector3N(), physicsSystem.BufferPool);
                
                // Add the mesh shape to the simulation's shape collection
                var shapeIndex = physicsSystem.Simulation.Shapes.Add(bepuMesh);
                //rotate the rotation by this.
                var rotation2 = rotation * rotationOffset;
                // Create static body with the mesh shape
                var staticHandle = physicsSystem.Simulation.Statics.Add(new StaticDescription(
                    offset.ToVector3N(),
                    rotation2.ToQuaternionN(), 
                    shapeIndex));
                
                // Keep references for cleanup and wireframe rendering
                corridorBepuMeshes.Add(bepuMesh);
                meshTriangleVertices.Add(wireframeVertices);
                staticMeshTransforms.Add((offset, rotation2));


                Console.WriteLine($"Created corridor physics mesh at position: {offset} with physics scale: {physicsScale}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to create corridor physics mesh: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                // Continue without physics mesh for this corridor
            }
        }

        private void CreatePhysicsGround()
        {
            // Create visible ground plane for character physics and visual reference
            ground = CreateGround(new Vector3(0, -50, 0), new Vector3(8000, 2, 8000), 
                "models/cube", "textures/prototype/concrete");
            ground.IsVisible = false; // Make it visible for testing
            ground.Scale = new Vector3(10f, 0.1f, 20f);
            ground.Color = new Vector3(0.5f, 0.5f, 0.5f); // Gray color
        }

        private void CreateTestCube()
        {
            // Create a simple test cube with unlit material in the center
            var testMaterial = new UnlitMaterial("textures/prototype/brick");
            testMaterial.VertexJitterAmount = 2.0f;
            testMaterial.AffineAmount = 1.0f;
            
            var testCube = CreateBoxWithMaterial(new Vector3(0, 10, 0), testMaterial, new Vector3(3f));
            testCube.Color = new Vector3(1.0f, 0.5f, 0.2f); // Orange color
            testCube.IsVisible = true;
        }

        void CreateCharacter(Vector3 position, Quaternion rotation)
        {
            characterActive = true;
            character = new CharacterInput(characters, position.ToVector3N(),
                new Capsule(0.5f * 10, 1 * 10),
                minimumSpeculativeMargin: 1f,
                mass: 0.1f,
                maximumHorizontalForce: 100,
                maximumVerticalGlueForce: 1500,
                jumpVelocity: 0,
                speed: 80,
                maximumSlope: 40f.ToRadians());

            // Store the initial rotation for the camera
            characterInitialRotation = rotation;
        }

                    //      "models/level/door", "models/level/door_frame",
                  //"textures/door", "textures/door_frame",
        /// <summary>
        /// Creates an automatic sliding door at the specified position
        /// </summary>
        /// <param name="position">World position for the door</param>
        /// <param name="rotation">Rotation of the door</param>
        /// <param name="scale">Scale of the door (default is 0.2f to match corridor scale)</param>
        /// <param name="doorModel">Path to door model (optional)</param>
        /// <param name="frameModel">Path to doorframe model (optional)</param>
        /// <param name="doorMaterial">Material for the door (optional)</param>
        /// <param name="frameMaterial">Material for the door frame (optional)</param>
        /// <returns>The created door entity</returns>
        public DoorEntity CreateDoor(Vector3 position, Quaternion rotation, Vector3? scale = null,
            string doorModel = "models/level/door", string frameModel = "models/level/door_frame",
            Material doorMaterial = null, Material frameMaterial = null)
        {
            // Use default scale to match corridor if not specified
            var doorScale = scale ?? new Vector3(0.2f);

            // Create the door entity with materials
            var door = new DoorEntity(position, rotation, doorScale,
                doorModel, frameModel, doorMaterial, frameMaterial, physicsSystem);

            // Add door's rendering entities to the scene
            AddRenderingEntity(door.DoorModel);
            AddRenderingEntity(door.DoorFrameModel);

            // Add to doors list for updating
            doors.Add(door);

            CreatePhysicsMesh(frameModel, position, rotation,
                QuaternionExtensions.CreateFromYawPitchRollDegrees(0, -90, 0), Vector3.Zero);


            Console.WriteLine($"Created door at position: {position}");
            return door;
        }

        /// <summary>
        /// Creates an automatic sliding door with multi-material support
        /// </summary>
        public DoorEntity CreateDoorWithMaterials(Vector3 position, Quaternion rotation,
            Dictionary<int, Material> doorMaterials, Dictionary<int, Material> frameMaterials,
            Vector3? scale = null,
            string doorModel = "models/level/door", string frameModel = "models/level/door_frame")
        {
            // Use default scale to match corridor if not specified
            var doorScale = scale ?? new Vector3(0.2f);

            // Create the door entity with material dictionaries
            var door = new DoorEntity(position, rotation, doorScale,
                doorModel, frameModel, doorMaterials, frameMaterials, physicsSystem);

            // Add door's rendering entities to the scene
            AddRenderingEntity(door.DoorModel);
            AddRenderingEntity(door.DoorFrameModel);

            // Add to doors list for updating
            doors.Add(door);

            Console.WriteLine($"Created door with materials at position: {position}");
            return door;
        }

        /// <summary>
        /// Creates a door with custom opening parameters
        /// </summary>
        public DoorEntity CreateDoorWithParameters(Vector3 position, Quaternion rotation,
            float openSpeed = 3f, float openHeight = 30f, float triggerDistance = 50f,
            float closeDelay = 2f, Vector3? scale = null,
            Material doorMaterial = null, Material frameMaterial = null)
        {
            var door = CreateDoor(position, rotation, scale,
                "models/level/door", "models/level/door_frame",
                doorMaterial, frameMaterial);

            //physicsEntities.Add(door.doo
            door.SetOpeningParameters(openSpeed, openHeight, triggerDistance, closeDelay);
            return door;
        }

        /// <summary>
        /// Creates an interactive door that doesn't open automatically but can trigger custom actions
        /// </summary>
        public InteractableDoorEntity CreateInteractiveDoor(Vector3 position, Quaternion rotation,
            string destinationName, Material doorMaterial, Material frameMaterial,
            Action<InteractableDoorEntity> doorAction, Vector3? scale = null)
        {
            var doorScale = scale ?? new Vector3(0.2f);

            // Create interactive door entity
            var interactiveDoor = new InteractableDoorEntity(position, rotation, doorScale,
                destinationName, "models/level/door_2", "models/level/door_frame_2",
                doorMaterial, frameMaterial, physicsSystem);

            // Add door's rendering entities to the scene
            AddRenderingEntity(interactiveDoor.DoorModel);
            AddRenderingEntity(interactiveDoor.DoorFrameModel);

            // Register with interaction system
            interactionSystem.RegisterInteractable(interactiveDoor);

            // Add to interactive doors list
            interactiveDoors.Add(interactiveDoor);

            // Set up custom action for demonstration
            interactiveDoor.SetCustomAction((door) =>
            {
                doorAction(door);
                Console.WriteLine($"Teleporting to {door.DestinationName}!");
                // Here you could implement actual teleportation, scene switching, etc.
            });

            Console.WriteLine($"Created interactive door to {destinationName} at position: {position}");
            return interactiveDoor;
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

            // Update doors with character position (only after load delay)
            if (characterActive && character.HasValue && timeSinceLoad > characterLoadDelay)
            {
                var characterPos = character.Value.Body.Pose.Position.ToVector3();
                foreach (var door in doors)
                {
                    door.Update(gameTime, characterPos);
                }
            }

        }

        /// <summary>
        /// Locks all doors in the scene
        /// </summary>
        public void LockAllDoors()
        {
            foreach (var door in doors)
            {
                door.Lock();
            }
        }

        /// <summary>
        /// Unlocks all doors in the scene
        /// </summary>
        public void UnlockAllDoors()
        {
            foreach (var door in doors)
            {
                door.Unlock();
            }
        }

        /// <summary>
        /// Gets a door at the specified position (within a tolerance)
        /// </summary>
        public DoorEntity GetDoorNear(Vector3 position, float tolerance = 10f)
        {
            return doors.FirstOrDefault(door =>
                Vector3.Distance(door.Position, position) <= tolerance);
        }

        /// <summary>
        /// Gets all doors in the scene
        /// </summary>
        public List<DoorEntity> GetAllDoors()
        {
            return new List<DoorEntity>(doors);
        }

        public void UpdateWithCamera(GameTime gameTime, Camera camera)
        {
            // Update the scene normally first
            Update(gameTime);

            // Only update interactions and character movement if not in menu
            if (!Globals.IsInMenuState())
            {
                // Update interaction system with camera for raycasting
                interactionSystem.Update(gameTime, camera);

                // Update character with camera (for movement direction based on camera look)
                // Only allow character movement after load delay to prevent falling through floor
                if (characterActive && character.HasValue && timeSinceLoad > characterLoadDelay)
                {
                    character.Value.UpdateCharacterGoals(Keyboard.GetState(), camera, (float)gameTime.ElapsedGameTime.TotalSeconds);
                }
            }
        }

        private void HandleInput()
        {
            var keyboard = Keyboard.GetState();

            // Test door locking functionality with O and P keys
            if (keyboard.IsKeyDown(Keys.O) && !previousKeyboard.IsKeyDown(Keys.O))
            {
                // Lock all doors
                foreach (var door in doors)
                {
                    door.Lock();
                }
                Console.WriteLine("All doors locked!");
            }

            if (keyboard.IsKeyDown(Keys.P) && !previousKeyboard.IsKeyDown(Keys.P))
            {
                // Unlock all doors
                foreach (var door in doors)
                {
                    door.Unlock();
                }
                Console.WriteLine("All doors unlocked!");
            }

            // Toggle physics wireframe rendering with L key
            if (keyboard.IsKeyDown(Keys.L) && !previousKeyboard.IsKeyDown(Keys.L))
            {
                // Toggle the bounding box renderer (which controls both bounding boxes and wireframes)
                if (boundingBoxRenderer != null)
                {
                   // boundingBoxRenderer.ShowBoundingBoxes = !boundingBoxRenderer.ShowBoundingBoxes;
                    Console.WriteLine($"Physics debug rendering: {(boundingBoxRenderer.ShowBoundingBoxes ? "ON" : "OFF")}");
                }
            }
            
            previousKeyboard = keyboard;
            
            // Mouse shooting - use camera direction
            if (Mouse.GetState().LeftButton == ButtonState.Pressed && !mouseClick)
            {
                if (characterActive && character.HasValue)
                {
                    var characterPos = character.Value.Body.Pose.Position.ToVector3();
                    
                    // Use forward direction (will be replaced by proper camera forward by screen)
                    var dir = Vector3N.UnitZ;
                    //ShootBullet(characterPos + new Vector3(0, 5, 0), dir);
                }
                mouseClick = true;
            }
            if (Mouse.GetState().LeftButton == ButtonState.Released)
            {
                mouseClick = false;
            }
        }

        public override void Draw(GameTime gameTime, Camera camera)
        {
            // Draw all entities using the base scene drawing
            base.Draw(gameTime, camera);

            // The corridor entity will be drawn with its multi-material system
            // Character is not drawn in FPS mode

            // Always draw wireframes around interactive objects
            DrawInteractiveObjectWireframes(gameTime, camera);

            // Draw wireframe visualization of static mesh collision geometry if enabled
            if (boundingBoxRenderer != null && boundingBoxRenderer.ShowBoundingBoxes)
            {
                DrawStaticMeshWireframes(gameTime, camera);
            }
        }

        /// <summary>
        /// Draws the UI elements including interaction prompts
        /// </summary>
        public void DrawUI(GameTime gameTime, Camera camera, SpriteBatch spriteBatch)
        {
            var font = Globals.fontNTR;
            if (font != null)
            {
                // Show loading indicator during character load delay
                if (timeSinceLoad <= characterLoadDelay)
                {
                    string loadingText = "Stabilizing environment...";
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

                // Draw interaction UI
                interactionSystem.DrawUI(spriteBatch, font);
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
            for (int meshIndex = 0; meshIndex < corridorBepuMeshes.Count; meshIndex++)
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

        private void DrawInteractiveObjectWireframes(GameTime gameTime, Camera camera)
        {
            var graphicsDevice = Globals.screenManager.GraphicsDevice;

            // Create a basic effect for wireframe rendering
            var basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.View = camera.View;
            basicEffect.Projection = camera.Projection;
            basicEffect.World = Matrix.Identity;

            // Draw wireframes for interactive doors using exact box collider dimensions and orientation
            foreach (var interactiveDoor in interactiveDoors)
            {
                // Use the exact same physics parameters as the InteractableDoorEntity
                var physicsOffset = new Vector3(0, 15, 0);
                var boxDimensions = new Vector3(20f, 30f, 10f);

                // Apply the door's scale to the box dimensions
                var scaledBoxDimensions = boxDimensions;// * interactiveDoor.Scale;

                if (interactiveDoor.IsTargeted)
                {
                    DrawBoxWireframe(basicEffect,
                    interactiveDoor.Position,
                    physicsOffset,
                    scaledBoxDimensions,
                    interactiveDoor.Rotation,
                    Color.Cyan);
                }
            }

            basicEffect.Dispose();
        }

        private void DrawBoxWireframe(BasicEffect effect, Vector3 position, Vector3 offset, Vector3 size, Quaternion rotation, Color color)
        {
            // Calculate half extents
            var halfSize = size * 0.5f;

            // Define the 8 corners of the box in local space
            var corners = new Vector3[]
            {
                new Vector3(-halfSize.X, -halfSize.Y, -halfSize.Z), // 0: left-bottom-back
                new Vector3( halfSize.X, -halfSize.Y, -halfSize.Z), // 1: right-bottom-back
                new Vector3( halfSize.X,  halfSize.Y, -halfSize.Z), // 2: right-top-back
                new Vector3(-halfSize.X,  halfSize.Y, -halfSize.Z), // 3: left-top-back
                new Vector3(-halfSize.X, -halfSize.Y,  halfSize.Z), // 4: left-bottom-front
                new Vector3( halfSize.X, -halfSize.Y,  halfSize.Z), // 5: right-bottom-front
                new Vector3( halfSize.X,  halfSize.Y,  halfSize.Z), // 6: right-top-front
                new Vector3(-halfSize.X,  halfSize.Y,  halfSize.Z)  // 7: left-top-front
            };

            // Transform corners to world space
            var worldMatrix = Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(position + offset);
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = Vector3.Transform(corners[i], worldMatrix);
            }

            // Define the 12 edges of the box
            var edges = new (int start, int end)[]
            {
                // Back face
                (0, 1), (1, 2), (2, 3), (3, 0),
                // Front face
                (4, 5), (5, 6), (6, 7), (7, 4),
                // Connecting edges
                (0, 4), (1, 5), (2, 6), (3, 7)
            };

            // Create vertices for all edges
            var wireframeVertices = new List<VertexPositionColor>();
            foreach (var (start, end) in edges)
            {
                wireframeVertices.Add(new VertexPositionColor(corners[start], color));
                wireframeVertices.Add(new VertexPositionColor(corners[end], color));
            }

            if (wireframeVertices.Count > 0)
            {
                try
                {
                    // Apply the effect and draw the wireframe lines
                    effect.CurrentTechnique.Passes[0].Apply();
                    Globals.screenManager.GraphicsDevice.DrawUserPrimitives(
                        PrimitiveType.LineList,
                        wireframeVertices.ToArray(),
                        0,
                        wireframeVertices.Count / 2);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error drawing interactive object wireframe: {ex.Message}");
                }
            }
        }


        // Utility methods for external access
        public List<PhysicsEntity> GetBullets() => bullets;
        public bool IsCharacterActive() => characterActive;
        public CharacterInput? GetCharacter() => character;
        public Quaternion GetCharacterInitialRotation() => characterInitialRotation;
        //public MultiMaterialRenderingEntity GetCorridor() => corridorEntity;
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose all doors
                foreach (var door in doors)
                {
                    door?.Dispose();
                }
                doors.Clear();

                // Dispose all interactive doors
                foreach (var interactiveDoor in interactiveDoors)
                {
                    interactiveDoor?.Dispose();
                }
                interactiveDoors.Clear();

                // Clear interaction system
                interactionSystem?.Clear();

                // BepuPhysics meshes are cleaned up by the physics system
                corridorBepuMeshes.Clear();
            }
            base.Dispose(disposing);
        }
    }
}