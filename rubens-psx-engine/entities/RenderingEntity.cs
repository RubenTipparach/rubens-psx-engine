using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using rubens_psx_engine;
using System.Diagnostics;

namespace rubens_psx_engine.entities
{
    /// <summary>
    /// Base rendering entity that handles transformation, rendering, and visual properties
    /// </summary>
    public class RenderingEntity
    {
        protected Model model;
        protected Matrix[] transforms;
        protected Texture2D texture;
        protected Effect effect;

        // Transform properties
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }

        // Rendering properties
        public Vector3 Color { get; set; }
        public bool IsShaded { get; set; }
        public bool IsVisible { get; set; }
        
        // Model access
        public Model Model => model;

        public RenderingEntity(string modelPath, string texturePath = null, string effectPath = "shaders/surface/Unlit", bool isShaded = true)
        {
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
            Color = Vector3.One;
            IsShaded = isShaded;
            IsVisible = true;

            LoadAssets(modelPath, texturePath, effectPath);
        }

        protected virtual void LoadAssets(string modelPath, string texturePath, string effectPath)
        {
            // Load model
            try
            {
                Console.WriteLine($"[RenderingEntity] Loading model: {modelPath}");
                string modelName = modelPath.Contains(".") ? modelPath.Substring(0, modelPath.LastIndexOf(".")) : modelPath;
                model = Globals.screenManager.Content.Load<Model>(modelName);
                transforms = new Matrix[model.Bones.Count];
                Console.WriteLine($"[RenderingEntity] ✓ Model loaded successfully: {modelPath}");
            }
            catch (Exception e)
            {
                string errorMessage = $"[RENDERING ENTITY ERROR]\n\n" +
                                    $"Failed to load MODEL:\n" +
                                    $"Path: {modelPath}\n\n" +
                                    $"Error: {e.Message}\n\n" +
                                    $"Full path attempted: Content/{modelPath}.xnb\n\n" +
                                    $"{e.StackTrace}";
                Console.WriteLine(errorMessage);
                Helpers.FatalPopup(errorMessage);
                return;
            }

            // Load texture if provided
            if (!string.IsNullOrEmpty(texturePath))
            {
                try
                {
                    Console.WriteLine($"[RenderingEntity] Loading texture: {texturePath}");
                    texture = Globals.screenManager.Content.Load<Texture2D>(texturePath);
                    Console.WriteLine($"[RenderingEntity] ✓ Texture loaded successfully: {texturePath}");
                }
                catch (Exception e)
                {
                    string errorMessage = $"[RENDERING ENTITY ERROR]\n\n" +
                                        $"Failed to load TEXTURE:\n" +
                                        $"Path: {texturePath}\n\n" +
                                        $"Model loaded OK: {modelPath}\n\n" +
                                        $"Error: {e.Message}\n\n" +
                                        $"Full path attempted: Content/{texturePath}.xnb\n\n" +
                                        $"{e.StackTrace}";
                    Console.WriteLine(errorMessage);
                    Helpers.FatalPopup(errorMessage);
                    return;
                }
            }

            // Load effect
            if (!string.IsNullOrEmpty(effectPath))
            {
                try
                {
                    Console.WriteLine($"[RenderingEntity] Loading effect: {effectPath}");
                    effect = Globals.screenManager.Content.Load<Effect>(effectPath);
                    Console.WriteLine($"[RenderingEntity] ✓ Effect loaded successfully: {effectPath}");
                }
                catch (Exception e)
                {
                    string errorMessage = $"[RENDERING ENTITY ERROR]\n\n" +
                                        $"Failed to load EFFECT/SHADER:\n" +
                                        $"Path: {effectPath}\n\n" +
                                        $"Model loaded OK: {modelPath}\n" +
                                        $"Texture loaded OK: {texturePath}\n\n" +
                                        $"Error: {e.Message}\n\n" +
                                        $"Full path attempted: Content/{effectPath}.xnb\n\n" +
                                        $"{e.StackTrace}";
                    Console.WriteLine(errorMessage);
                    Helpers.FatalPopup(errorMessage);
                    return;
                }
            }

            // Apply texture and effect to model
            try
            {
                Console.WriteLine($"[RenderingEntity] Setting up model effects");
                SetupModelEffects();
                Console.WriteLine($"[RenderingEntity] ✓ Model effects setup complete");
            }
            catch (Exception e)
            {
                string errorMessage = $"[RENDERING ENTITY ERROR]\n\n" +
                                    $"Failed to SETUP MODEL EFFECTS:\n\n" +
                                    $"Model: {modelPath} (loaded OK)\n" +
                                    $"Texture: {texturePath} (loaded OK)\n" +
                                    $"Effect: {effectPath} (loaded OK)\n\n" +
                                    $"Error applying to model parts: {e.Message}\n\n" +
                                    $"{e.StackTrace}";
                Console.WriteLine(errorMessage);
                Helpers.FatalPopup(errorMessage);
            }
        }

        protected virtual void SetupModelEffects()
        {
            if (model == null) return;

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    if (effect != null)
                    {
                        if (texture != null && effect.Parameters["Texture"] != null)
                        {
                            effect.Parameters["Texture"].SetValue(texture);
                        }
                        part.Effect = effect;
                        if (effect.Techniques["Unlit"] != null)
                        {
                            effect.CurrentTechnique = effect.Techniques["Unlit"];
                        }
                    }
                    else if (part.Effect is BasicEffect basicEffect)
                    {
                        // Fallback to BasicEffect if no custom effect
                        basicEffect.TextureEnabled = texture != null;
                        if (texture != null)
                        {
                            basicEffect.Texture = texture;
                        }
                    }
                }
            }
        }

        public virtual Matrix GetWorldMatrix()
        {
            return Matrix.CreateScale(Scale) * 
                   Matrix.CreateFromQuaternion(Rotation) * 
                   Matrix.CreateTranslation(Position);
        }

        public virtual void Update(GameTime gameTime)
        {
            // Override in derived classes for custom update logic
        }

        public virtual void Draw(GameTime gameTime, Camera camera)
        {
            if (!IsVisible || model == null) return;

            model.CopyAbsoluteBoneTransformsTo(transforms);
            Matrix world = GetWorldMatrix();

            foreach (ModelMesh mesh in model.Meshes)
            {
                Matrix meshWorld = transforms[mesh.ParentBone.Index] * world;

                foreach (Effect meshEffect in mesh.Effects)
                {
                    // Set common effect parameters
                    if (meshEffect.Parameters["World"] != null)
                        meshEffect.Parameters["World"].SetValue(meshWorld);
                    if (meshEffect.Parameters["View"] != null)
                        meshEffect.Parameters["View"].SetValue(camera.View);
                    if (meshEffect.Parameters["Projection"] != null)
                        meshEffect.Parameters["Projection"].SetValue(camera.Projection);

                    // Handle BasicEffect lighting
                    if (meshEffect is BasicEffect basicEffect)
                    {
                        SetupBasicEffectLighting(basicEffect);
                    }
                }
                try
                {
                    mesh.Draw();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"RenderingEntity: ERROR drawing mesh {mesh.Name}: {ex.Message}");
                }
            }
        }

        protected virtual void SetupBasicEffectLighting(BasicEffect effect)
        {
            effect.LightingEnabled = IsShaded;

            if (IsShaded)
            {
                effect.DiffuseColor = Color;
                effect.DirectionalLight0.DiffuseColor = new Vector3(0.7f, 0.7f, 0.7f);
                effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(20, -60, -60));
                effect.AmbientLightColor = new Vector3(0.3f, 0.3f, 0.3f);
            }
            else
            {
                effect.DiffuseColor = Color;
                effect.AmbientLightColor = new Vector3(1f, 1f, 1f);
            }
        }

        public virtual void SetModel(string modelPath)
        {
            try
            {
                string modelName = modelPath.Contains(".") ? modelPath.Substring(0, modelPath.LastIndexOf(".")) : modelPath;
                model = Globals.screenManager.Content.Load<Model>(modelName);
                transforms = new Matrix[model.Bones.Count];
                SetupModelEffects();
            }
            catch (Exception e)
            {
                Helpers.ErrorPopup($"Failed to load model: {modelPath}\n\n{e.Message}");
            }
        }

        public virtual void SetTexture(string texturePath)
        {
            try
            {
                texture = Globals.screenManager.Content.Load<Texture2D>(texturePath);
                SetupModelEffects();
            }
            catch (Exception e)
            {
                Helpers.ErrorPopup($"Failed to load texture: {texturePath}\n\n{e.Message}");
            }
        }
    }
}