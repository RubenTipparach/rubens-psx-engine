using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine;
using rubens_psx_engine.Extensions;
using rubens_psx_engine.system;
using rubens_psx_engine.entities;
using anakinsoft.system;
using anakinsoft.system.physics;
using anakinsoft.entities;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine
{
    /// <summary>
    /// Manages all characters in The Lounge scene
    /// </summary>
    public class LoungeCharacterManager
    {
        private readonly LoungeCharacterLoader loader;
        private readonly Dictionary<string, CharacterInstance> characters = new Dictionary<string, CharacterInstance>();
        private readonly List<RenderingEntity> allModels = new List<RenderingEntity>();
        private readonly float levelScale;

        // Character keys
        public const string BARTENDER = "bartender";
        public const string PATHOLOGIST = "pathologist";

        public LoungeCharacterManager(PhysicsSystem physics, InteractionSystem interaction, float scale)
        {
            loader = new LoungeCharacterLoader(physics, interaction, scale);
            levelScale = scale;
        }

        /// <summary>
        /// Initialize bartender character
        /// </summary>
        public void LoadBartender()
        {
            if (characters.ContainsKey(BARTENDER))
            {
                Console.WriteLine("Bartender already loaded, skipping...");
                return;
            }

            Vector3 bartenderPosition = new Vector3(25f, 0, -25f) * levelScale;
            Vector3 cameraInteractionPosition = bartenderPosition + new Vector3(0, 20, 20);
            Vector3 cameraLookAt = bartenderPosition + new Vector3(0, 0, -10);
            Quaternion rotation = QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 0, 0);
            Vector3 colliderSize = new Vector3(10f * levelScale, 48f * levelScale, 10f * levelScale);

            var instance = loader.CreateCharacter(
                "Bartender",
                bartenderPosition,
                rotation,
                cameraInteractionPosition,
                cameraLookAt,
                "models/characters/alien",
                "textures/chars/(NPC) bartender zix",
                0.25f,
                colliderSize,
                isSitting: false
            );

            characters[BARTENDER] = instance;
            allModels.Add(instance.Model);
        }

        /// <summary>
        /// Initialize pathologist character (spawned after bartender dialogue)
        /// </summary>
        public void LoadPathologist()
        {
            if (characters.ContainsKey(PATHOLOGIST))
            {
                Console.WriteLine("Pathologist already loaded, skipping...");
                return;
            }

            Vector3 pathologistPosition = new Vector3(-29f, 0, 28f) * (10f * levelScale);
            Vector3 cameraInteractionPosition = pathologistPosition + new Vector3(15, 20, 0);
            Vector3 cameraLookAt = pathologistPosition + new Vector3(0, 10, 0);
            Quaternion rotation = QuaternionExtensions.CreateFromYawPitchRollDegrees(90, 0, 0);
            Vector3 colliderSize = new Vector3(15f * levelScale, 30f * levelScale, 15f * levelScale); // Sitting height

            var instance = loader.CreateCharacter(
                "Dr. Harmon Kerrigan",
                pathologistPosition,
                rotation,
                cameraInteractionPosition,
                cameraLookAt,
                "models/characters/alien-2",
                "textures/chars/Dr Harmon - CMO",
                0.3f,
                colliderSize,
                isSitting: true
            );

            characters[PATHOLOGIST] = instance;
            allModels.Add(instance.Model);
        }

        /// <summary>
        /// Get character by key
        /// </summary>
        public CharacterInstance GetCharacter(string key)
        {
            return characters.ContainsKey(key) ? characters[key] : null;
        }

        /// <summary>
        /// Get character interaction by key
        /// </summary>
        public InteractableCharacter GetInteraction(string key)
        {
            return GetCharacter(key)?.Interaction;
        }

        /// <summary>
        /// Check if character is loaded
        /// </summary>
        public bool IsCharacterLoaded(string key)
        {
            return characters.ContainsKey(key);
        }

        /// <summary>
        /// Get all character models for rendering
        /// </summary>
        public List<RenderingEntity> GetAllModels()
        {
            return allModels;
        }

        /// <summary>
        /// Draw collider for character (debug visualization)
        /// </summary>
        public void DrawCharacterCollider(string key, Camera camera, InteractionSystem interactionSystem, GraphicsDevice graphicsDevice)
        {
            var character = GetCharacter(key);
            if (character == null) return;

            var basicEffect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                View = camera.View,
                Projection = camera.Projection,
                World = Matrix.Identity
            };

            // Check if character is being targeted
            bool isTargeted = interactionSystem?.CurrentTarget == character.Interaction;
            Color colliderColor = isTargeted ? Color.Yellow : Color.Magenta;

            // Draw debug box
            DrawDebugBox(character.ColliderCenter, character.ColliderSize, colliderColor, basicEffect, graphicsDevice);

            basicEffect.Dispose();
        }

        /// <summary>
        /// Draw all character colliders
        /// </summary>
        public void DrawAllColliders(Camera camera, InteractionSystem interactionSystem, GraphicsDevice graphicsDevice)
        {
            foreach (var key in characters.Keys)
            {
                DrawCharacterCollider(key, camera, interactionSystem, graphicsDevice);
            }
        }

        /// <summary>
        /// Get portrait key for hovered character
        /// </summary>
        public string GetHoveredCharacterPortrait(InteractionSystem interactionSystem)
        {
            if (interactionSystem?.CurrentTarget is InteractableCharacter hoveredChar)
            {
                foreach (var kvp in characters)
                {
                    if (kvp.Value.Interaction == hoveredChar)
                    {
                        return kvp.Key switch
                        {
                            BARTENDER => "NPC_Bartender",
                            PATHOLOGIST => "DrHarmon",
                            _ => null
                        };
                    }
                }
            }
            return null;
        }

        private void DrawDebugBox(Vector3 center, Vector3 size, Color color, BasicEffect effect, GraphicsDevice graphicsDevice)
        {
            var vertices = new List<VertexPositionColor>();

            // Calculate half extents
            float halfWidth = size.X / 2f;
            float halfHeight = size.Y / 2f;
            float halfDepth = size.Z / 2f;

            // Define 8 corners of the box
            Vector3[] corners = new Vector3[8]
            {
                center + new Vector3(-halfWidth, -halfHeight, -halfDepth),
                center + new Vector3(halfWidth, -halfHeight, -halfDepth),
                center + new Vector3(halfWidth, halfHeight, -halfDepth),
                center + new Vector3(-halfWidth, halfHeight, -halfDepth),
                center + new Vector3(-halfWidth, -halfHeight, halfDepth),
                center + new Vector3(halfWidth, -halfHeight, halfDepth),
                center + new Vector3(halfWidth, halfHeight, halfDepth),
                center + new Vector3(-halfWidth, halfHeight, halfDepth)
            };

            // Define 12 edges
            int[][] edges = new int[][]
            {
                new int[] {0, 1}, new int[] {1, 2}, new int[] {2, 3}, new int[] {3, 0}, // Bottom face
                new int[] {4, 5}, new int[] {5, 6}, new int[] {6, 7}, new int[] {7, 4}, // Top face
                new int[] {0, 4}, new int[] {1, 5}, new int[] {2, 6}, new int[] {3, 7}  // Vertical edges
            };

            // Add vertices for each edge
            foreach (var edge in edges)
            {
                vertices.Add(new VertexPositionColor(corners[edge[0]], color));
                vertices.Add(new VertexPositionColor(corners[edge[1]], color));
            }

            // Draw lines
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices.ToArray(), 0, vertices.Count / 2);
            }
        }
    }
}
