using anakinsoft.system.character;
using anakinsoft.system.physics;
using anakinsoft.utilities;
using Demos.Demos.Characters;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuUtilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using rubens_psx_engine.entities;
using rubens_psx_engine.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Vector3N = System.Numerics.Vector3;
using QuaternionN = System.Numerics.Quaternion;

namespace anakinsoft.game.scenes
{
    public class BepuInteractionScene : Scene
    {
        CharacterControllers characters;
        CharacterInput? character;
        bool characterActive;

        public class InteractableObject
        {
            public PhysicsEntity Entity { get; set; }
            public BodyHandle BodyHandle { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public Color HighlightColor { get; set; }
            public Color DefaultColor { get; set; }
            public bool IsHighlighted { get; set; }
            public bool IsDynamic { get; set; }
        }

        List<InteractableObject> interactables;
        InteractableObject hoveredObject;

        MouseState previousMouse;
        KeyboardState previousKeyboard;

        public BepuInteractionScene() : base()
        {
            characters = null;
            physicsSystem = new PhysicsSystem(ref characters);
            interactables = new List<InteractableObject>();
            BackgroundColor = Color.CornflowerBlue;

            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();

            CreateGround();
            CreateInteractableObjects();
            CreateCharacter(new Vector3(0, 5, 20));
        }

        private void CreateGround()
        {
            var ground = CreateGround(new Vector3(0, -2f, 0), new Vector3(200, 1, 200),
                "models/cube", "textures/prototype/concrete");
            ground.IsVisible = true;
            ground.Scale = new Vector3(20f, 0.1f, 20f);
            ground.Color = new Vector3(0.4f, 0.4f, 0.4f);
        }

        private void CreateInteractableObjects()
        {
            // Static objects
            CreateStaticObject(new Vector3(-10, 2, 0), "Control Panel", "Main system control interface", Color.Blue);
            CreateStaticObject(new Vector3(10, 2, 0), "Data Terminal", "Information access point", Color.Green);
            CreateStaticObject(new Vector3(0, 2, -10), "Power Core", "Primary energy source", Color.Yellow);
            CreateStaticObject(new Vector3(-5, 2, 5), "Diagnostic Unit", "System health monitor", Color.Purple);

            // Dynamic objects (physics-enabled)
            CreateDynamicObject(new Vector3(-15, 5, -5), "Cargo Crate A", "Movable storage container", Color.Orange);
            CreateDynamicObject(new Vector3(15, 5, -5), "Cargo Crate B", "Heavy equipment case", Color.Red);
            CreateDynamicObject(new Vector3(5, 8, 5), "Supply Box", "Emergency supplies", Color.Cyan);
            CreateDynamicObject(new Vector3(-8, 12, -8), "Tool Container", "Maintenance equipment", Color.Magenta);
        }

        private void CreateStaticObject(Vector3 position, string name, string description, Color highlightColor)
        {
            var entity = CreateBox(position, new Vector3(3, 3, 3), 0f, true,
                "models/cube", "textures/prototype/brick");
            entity.Scale = new Vector3(0.15f);
            entity.Color = new Vector3(0.6f, 0.6f, 0.6f);

            var interactable = new InteractableObject
            {
                Entity = entity,
                BodyHandle = entity.BodyHandle.Value,
                Name = name,
                Description = description,
                HighlightColor = highlightColor,
                DefaultColor = Color.Gray,
                IsDynamic = false
            };

            interactables.Add(interactable);
        }

        private void CreateDynamicObject(Vector3 position, string name, string description, Color highlightColor)
        {
            var entity = CreateBox(position, new Vector3(2, 2, 2), 5f, false,
                "models/cube", "textures/prototype/brick");
            entity.Scale = new Vector3(0.1f);
            entity.Color = new Vector3(0.8f, 0.6f, 0.4f);

            // Test the new quaternion extension methods (rotate some dynamic objects for variety)
            if (name.Contains("Tool"))
            {
                entity.Rotation = QuaternionExtensions.CreateFromYawPitchRollDegrees(45, 30, 15);
            }
            else if (name.Contains("Supply"))
            {
                entity.Rotation = QuaternionExtensions.CreateFromYawPitchRollDegrees(0, 45, 0);
            }

            var interactable = new InteractableObject
            {
                Entity = entity,
                BodyHandle = entity.BodyHandle.Value,
                Name = name,
                Description = description,
                HighlightColor = highlightColor,
                DefaultColor = Color.SandyBrown,
                IsDynamic = true
            };

            interactables.Add(interactable);
        }

        private void CreateCharacter(Vector3 position)
        {
            characterActive = true;
            character = new CharacterInput(characters, position.ToVector3N(),
                new Capsule(0.5f * 10, 1 * 10),
                minimumSpeculativeMargin: 0.1f,
                mass: 1f,
                maximumHorizontalForce: 100,
                maximumVerticalGlueForce: 10,
                jumpVelocity: 50,
                speed: 40,
                maximumSlope: 45f.ToRadians());
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            HandleInput();
        }

        private void HandleInput()
        {
            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();

            if (keyboard.IsKeyDown(Keys.L) && !previousKeyboard.IsKeyDown(Keys.L))
            {
                if (boundingBoxRenderer != null)
                {
                    boundingBoxRenderer.ShowBoundingBoxes = !boundingBoxRenderer.ShowBoundingBoxes;
                    Console.WriteLine($"Bounding boxes: {(boundingBoxRenderer.ShowBoundingBoxes ? "ON" : "OFF")}");
                }
            }

            if (mouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released)
            {
                if (hoveredObject != null && hoveredObject.IsDynamic)
                {
                    ApplyForceToObject(hoveredObject);
                }
            }

            previousKeyboard = keyboard;
            previousMouse = mouse;
        }

        private void ApplyForceToObject(InteractableObject obj)
        {
            if (characterActive && character.HasValue)
            {
                var charPos = character.Value.Body.Pose.Position;
                var objPos = physicsSystem.Simulation.Bodies[obj.BodyHandle].Pose.Position;
                var direction = Vector3N.Normalize(objPos - charPos);

                var force = direction * 500f;
                physicsSystem.Simulation.Bodies[obj.BodyHandle].ApplyLinearImpulse(force);

                Console.WriteLine($"Applied force to {obj.Name}!");
            }
        }

        public void UpdateWithCamera(GameTime gameTime, Camera camera)
        {
            Update(gameTime);
            PerformBepuRaycast(camera);

            if (characterActive && character.HasValue)
            {
                character.Value.UpdateCharacterGoals(Keyboard.GetState(), camera, (float)gameTime.ElapsedGameTime.TotalSeconds);
            }
        }

        private void PerformBepuRaycast(Camera camera)
        {
            var mouse = Mouse.GetState();
            var viewport = Globals.screenManager.GraphicsDevice.Viewport;

            Vector3 nearPoint = viewport.Unproject(
                new Vector3(mouse.X, mouse.Y, 0),
                camera.Projection,
                camera.View,
                Microsoft.Xna.Framework.Matrix.Identity);

            Vector3 farPoint = viewport.Unproject(
                new Vector3(mouse.X, mouse.Y, 1),
                camera.Projection,
                camera.View,
                Microsoft.Xna.Framework.Matrix.Identity);

            Vector3 rayDirection = Vector3.Normalize(farPoint - nearPoint);

            ResetObjectHighlights();

            var rayStart = nearPoint.ToVector3N();
            var rayDir = rayDirection.ToVector3N();
            float maxDistance = 1000f;

            var hitHandler = new RayHitHandler();
            physicsSystem.Simulation.RayCast(rayStart, rayDir, maxDistance, physicsSystem.BufferPool, ref hitHandler);

            if (hitHandler.Hit)
            {
                var hitObject = FindInteractableByBodyHandle(hitHandler.HitBodyHandle);
                if (hitObject != null)
                {
                    hitObject.IsHighlighted = true;
                    hitObject.Entity.Color = hitObject.HighlightColor.ToVector3();
                    hoveredObject = hitObject;
                }
            }
        }

        private void ResetObjectHighlights()
        {
            foreach (var obj in interactables)
            {
                obj.IsHighlighted = false;
                obj.Entity.Color = obj.DefaultColor.ToVector3();
            }
            hoveredObject = null;
        }

        private InteractableObject FindInteractableByBodyHandle(BodyHandle handle)
        {
            return interactables.FirstOrDefault(obj => obj.BodyHandle.Value == handle.Value);
        }

        public override void Draw(GameTime gameTime, Camera camera)
        {
            base.Draw(gameTime, camera);
        }

        public void DrawUI(GameTime gameTime, Camera camera, SpriteBatch spriteBatch)
        {
            DrawUIText(gameTime, camera, spriteBatch);
        }

        private void DrawUIText(GameTime gameTime, Camera camera, SpriteBatch spriteBatch)
        {
            var font = Globals.fontNTR;

            string instructions = "BEPU Physics Interaction Scene\n\nWASD = Move, Mouse = Look around\nHover over objects for info\nClick dynamic objects to push them\nL = Toggle bounding boxes";
            spriteBatch.DrawString(font, instructions, new Vector2(20, 20) + Vector2.One, Color.Black);
            spriteBatch.DrawString(font, instructions, new Vector2(20, 20), Color.White);

            if (hoveredObject != null)
            {
                var worldPos = hoveredObject.Entity.Position + new Vector3(0, 3, 0);
                var screenPos = Project3DToScreen(worldPos, camera);

                if (screenPos.HasValue)
                {
                    string objectType = hoveredObject.IsDynamic ? "[DYNAMIC]" : "[STATIC]";
                    string info = $">>> {hoveredObject.Name} {objectType} <<<\n{hoveredObject.Description}";

                    if (hoveredObject.IsDynamic)
                    {
                        info += "\n[Click to apply force]";
                    }

                    var textSize = font.MeasureString(info);
                    var textPos = screenPos.Value - textSize / 2;

                    var screenViewport = Globals.screenManager.GraphicsDevice.Viewport;
                    textPos.X = Microsoft.Xna.Framework.MathHelper.Clamp(textPos.X, 10, screenViewport.Width - textSize.X - 10);
                    textPos.Y = Microsoft.Xna.Framework.MathHelper.Clamp(textPos.Y, 10, screenViewport.Height - textSize.Y - 10);

                    spriteBatch.DrawString(font, info, textPos + Vector2.One * 2, Color.Black);
                    spriteBatch.DrawString(font, info, textPos, hoveredObject.HighlightColor);
                }
            }

            var objectInfo = $"Objects: {interactables.Count} ({interactables.Count(o => o.IsDynamic)} dynamic, {interactables.Count(o => !o.IsDynamic)} static)";
            var objectTextSize = font.MeasureString(objectInfo);
            var viewport = Globals.screenManager.GraphicsDevice.Viewport;
            var bottomRightPos = new Vector2(viewport.Width - objectTextSize.X - 20, viewport.Height - objectTextSize.Y - 20);

            spriteBatch.DrawString(font, objectInfo, bottomRightPos + Vector2.One, Color.Black);
            spriteBatch.DrawString(font, objectInfo, bottomRightPos, Color.LightGray);
        }

        private Vector2? Project3DToScreen(Vector3 worldPosition, Camera camera)
        {
            var viewport = Globals.screenManager.GraphicsDevice.Viewport;
            var screenPos = viewport.Project(worldPosition, camera.Projection, camera.View, Microsoft.Xna.Framework.Matrix.Identity);

            if (screenPos.Z < 0 || screenPos.Z > 1)
                return null;

            return new Vector2(screenPos.X, screenPos.Y);
        }
    }

    public struct RayHitHandler : IRayHitHandler
    {
        public bool Hit;
        public BodyHandle HitBodyHandle;
        public float HitDistance;

        public bool AllowTest(CollidableReference collidable)
        {
            return true;
        }

        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return true;
        }

        public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3N normal, CollidableReference collidable, int childIndex)
        {
            if (t < maximumT)
            {
                Hit = true;
                HitDistance = t;
                maximumT = t;

                if (collidable.Mobility == CollidableMobility.Dynamic || collidable.Mobility == CollidableMobility.Kinematic)
                {
                    HitBodyHandle = collidable.BodyHandle;
                }
                else
                {
                    HitBodyHandle = new BodyHandle(collidable.StaticHandle.Value);
                }
            }
        }
    }
}