using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace rubens_psx_engine
{
    //Basic game object. Renders a 3D model.
    public class Entity
    {
        protected Model myModel;
        protected Matrix[] transforms;

        protected Vector3 modelPosition;
        public Vector3 GetPosition { get { return modelPosition; } }

        protected Matrix modelRotation;
        public Matrix GetRotation { get { return modelRotation; } }

        protected Vector3 color;
        public Vector3 GetColor { get { return color; } }

        protected bool shaded;

        protected float scale;
        Texture2D texture;

        public Effect ps1Effect { get; private set; }


        public Entity(string modelPath, string texturePath, bool isShaded = true)
        {
            modelPosition = Vector3.Zero;

            try
            {
                //Attempt to load model.
                myModel = Globals.screenManager.Content.Load<Model>(modelPath.Substring(0, modelPath.LastIndexOf(".xnb")));

                texture = Globals.screenManager.Content.Load<Texture2D>(texturePath);
                ps1Effect = Globals.screenManager.Content.Load<Effect>("shaders/surface/Unlit");

                foreach (ModelMesh mesh in myModel.Meshes)
                {
                    foreach (ModelMeshPart part in mesh.MeshParts)
                    {
                        ps1Effect.Parameters["Texture"].SetValue(texture);

                        part.Effect = ps1Effect;
                    }
                }

                ps1Effect.CurrentTechnique = ps1Effect.Techniques["Unlit"];

            }
            catch (Exception e)
            {
                Helpers.FatalPopup($"Failed to load model:\n'{modelPath} or {texturePath}'\n\nError: {e.Message}");
            }

            transforms = new Matrix[myModel.Bones.Count];
            modelRotation = Matrix.Identity;
            

            color = new Vector3(1f, 1f, 1f);
            scale = 1.0f;
            shaded = isShaded;
        }

        public virtual void SetScale(float value)
        {
            scale = value;
        }

        public virtual void SetShaded(bool value)
        {
            shaded = value;
        }

        public virtual void SetRotation(Matrix _matrix)
        {
            modelRotation = _matrix;
        }

        public virtual void SetPosition(Vector3 _position)
        {
            modelPosition = _position;
        }

        public virtual void SetColor(Vector3 _color)
        {
            color = _color;
        }

        public void SetModel(string path)
        {
            try
            {
                myModel = Globals.screenManager.Content.Load<Model>(path.Substring(0, path.LastIndexOf(".xnb")));
            }
            catch (Exception e)
            {
                Helpers.ErrorPopup(string.Format("Failed to load model:\n{0}\n\n{1}", path, e.Message));
            }

            transforms = new Matrix[myModel.Bones.Count];
        }

        public virtual void Draw3D(GameTime gameTime, Camera camera)
        {
            myModel.CopyAbsoluteBoneTransformsTo(transforms);
            float rotationAngle = (float)gameTime.TotalGameTime.TotalSeconds * -.2f;
            modelRotation = Matrix.CreateRotationY(rotationAngle);
            foreach (ModelMesh mesh in myModel.Meshes)
            {
                // do this for custom effects
                foreach(Effect effect in mesh.Effects)
                {
                    var view = camera.View;
                    var projection = camera.Projection;
                    var world = transforms[mesh.ParentBone.Index] * modelRotation * Matrix.CreateScale(scale) * Matrix.CreateTranslation(modelPosition);
                    //ps1Effect.Parameters["WorldViewProj"]?.SetValue(world * view * projection);

                    effect.Parameters["World"].SetValue(world);
                    effect.Parameters["View"].SetValue(view);
                    effect.Parameters["Projection"].SetValue(projection);
                    effect.Parameters["Texture"].SetValue(texture);

                }

                // do this for basic effects
                //foreach (BasicEffect effect in mesh.Effects)
                //{
                //    effect.LightingEnabled = true;
                //    effect.TextureEnabled = true;
                //    effect.Texture = texture;
                //    if (shaded)
                //    {
                //        effect.DiffuseColor = color;
                //        effect.DirectionalLight0.DiffuseColor = new Vector3(.7f, .7f, .7f);
                //        Vector3 lightAngle = new Vector3(0, -45, 0);
                //        lightAngle.Normalize();
                //        effect.DirectionalLight0.Direction = lightAngle;
                //        effect.AmbientLightColor = new Vector3(.5f, .5f, .5f);
                //        effect.AmbientLightColor = new Vector3(1, 1, 1);

                //    }
                //    else
                //    {
                //        effect.DiffuseColor = color;
                //        effect.AmbientLightColor = new Vector3(10, 10, 10);
                //    }


                //    effect.View = camera.View;
                //    effect.Projection = camera.Projection;
                //    effect.World = transforms[mesh.ParentBone.Index] * modelRotation * Matrix.CreateScale(scale) * Matrix.CreateTranslation(modelPosition);
                //}



                mesh.Draw();
            }



        }

        // --- end of file
    }
}