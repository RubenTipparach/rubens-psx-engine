using Microsoft.Xna.Framework;
using rubens_psx_engine.entities;
using anakinsoft.system.physics;
using System;
using BepuPhysics;
using BepuUtilities.Memory;

namespace rubens_psx_engine.game.scenes
{
    public class TerrainScene : Scene
    {
        private StaticMesh terrainMesh;
        private float[,] heightData;
        private int terrainWidth = 200;
        private int terrainHeight = 200;
        private float cellSize = 5f;
        private float heightScale = 30f;

        public TerrainScene(PhysicsSystem physics) : base(physics)
        {
            BackgroundColor = new Color(135, 206, 235);
        }

        public override void Initialize()
        {
            base.Initialize();

            GenerateHeightmap();
            CreateTerrainMesh();
            CreateTerrainVisual();
            AddEnvironmentObjects();
        }

        private void GenerateHeightmap()
        {
            heightData = new float[terrainWidth, terrainHeight];
            Random random = new Random(42);

            for (int x = 0; x < terrainWidth; x++)
            {
                for (int z = 0; z < terrainHeight; z++)
                {
                    float fx = x / (float)terrainWidth;
                    float fz = z / (float)terrainHeight;

                    float noise1 = (float)(Math.Sin(fx * Math.PI * 4) * Math.Cos(fz * Math.PI * 4)) * 0.5f;
                    float noise2 = (float)(Math.Sin(fx * Math.PI * 8) * Math.Cos(fz * Math.PI * 8)) * 0.25f;
                    float noise3 = (float)(Math.Sin(fx * Math.PI * 16) * Math.Cos(fz * Math.PI * 16)) * 0.125f;

                    float distanceFromCenter = Vector2.Distance(
                        new Vector2(fx - 0.5f, fz - 0.5f) * 2,
                        Vector2.Zero);
                    float edgeFalloff = Math.Max(0, 1 - distanceFromCenter);
                    edgeFalloff = edgeFalloff * edgeFalloff;

                    heightData[x, z] = (noise1 + noise2 + noise3) * edgeFalloff;

                    if (Math.Abs(fx - 0.5f) < 0.1f && Math.Abs(fz - 0.5f) < 0.15f)
                    {
                        heightData[x, z] *= 0.1f;
                    }
                }
            }
        }

        private void CreateTerrainMesh()
        {
            terrainMesh = StaticMesh.CreateHeightmap(
                heightData,
                cellSize,
                heightScale,
                physicsSystem.Simulation,
                physicsSystem.BufferPool);

            Vector3 terrainPosition = new Vector3(
                -terrainWidth * cellSize * 0.5f,
                -10f,
                -terrainHeight * cellSize * 0.5f);

            terrainMesh.AddToSimulation(physicsSystem.Simulation, terrainPosition, Quaternion.Identity);
        }

        private void CreateTerrainVisual()
        {
            int centerX = terrainWidth / 2;
            int centerZ = terrainHeight / 2;
            float y = heightData[centerX, centerZ] * heightScale - 10f;

            Vector3 groundPos = new Vector3(0, y - 5, 0);
            Vector3 groundScale = new Vector3(terrainWidth * cellSize / 10f, 1f, terrainHeight * cellSize / 10f);

            var groundEntity = CreateBox(groundPos, groundScale, 0, true,
                "models/cube", "textures/prototype/grass");
            groundEntity.IsVisible = true;
        }

        private void AddEnvironmentObjects()
        {
            Random random = new Random(123);

            for (int i = 0; i < 20; i++)
            {
                float x = (random.Next(30, 170) - 100) * cellSize;
                float z = (random.Next(30, 170) - 100) * cellSize;

                int gridX = Math.Clamp((int)((x / cellSize) + terrainWidth / 2f), 0, terrainWidth - 1);
                int gridZ = Math.Clamp((int)((z / cellSize) + terrainHeight / 2f), 0, terrainHeight - 1);
                float y = heightData[gridX, gridZ] * heightScale - 5f;

                Vector3 rockPos = new Vector3(x, y, z);
                float rockScale = 2f + (float)random.NextDouble() * 3f;

                CreateSphere(rockPos, rockScale, 0, true, "models/sphere", "textures/prototype/concrete");
            }

            for (int i = 0; i < 10; i++)
            {
                float x = (random.Next(20, 180) - 100) * cellSize;
                float z = (random.Next(20, 180) - 100) * cellSize;

                int gridX = Math.Clamp((int)((x / cellSize) + terrainWidth / 2f), 0, terrainWidth - 1);
                int gridZ = Math.Clamp((int)((z / cellSize) + terrainHeight / 2f), 0, terrainHeight - 1);
                float y = heightData[gridX, gridZ] * heightScale - 8f;

                Vector3 pillarPos = new Vector3(x, y + 10, z);
                Vector3 pillarSize = new Vector3(3f, 20f, 3f);

                CreateBox(pillarPos, pillarSize, 0, true, "models/cube", "textures/prototype/dark");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                terrainMesh?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}