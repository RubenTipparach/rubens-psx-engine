using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace TerrainGenerator
{
    public class TerrainPreview : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private TerrainData terrainData;
        private Texture2D terrainTexture;
        private BasicEffect effect;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;

        private Vector3 cameraPosition;
        private Vector3 cameraTarget;
        private float cameraYaw;
        private float cameraPitch;
        private MouseState previousMouseState;
        private KeyboardState previousKeyboardState;

        public TerrainPreview(TerrainData terrain)
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            terrainData = terrain;
        }

        protected override void Initialize()
        {
            cameraPosition = new Vector3(terrainData.Width * terrainData.Scale / 2,
                                        terrainData.HeightScale * 5,
                                        terrainData.Height * terrainData.Scale * 1.5f);
            cameraTarget = new Vector3(terrainData.Width * terrainData.Scale / 2, 0,
                                      terrainData.Height * terrainData.Scale / 2);
            cameraYaw = MathF.PI;
            cameraPitch = -0.3f;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            string texturePath = @"..\..\Content\Assets\textures\prototype\prototype_512x512_green1.png";
            try
            {
                using (var stream = System.IO.File.OpenRead(texturePath))
                {
                    terrainTexture = Texture2D.FromStream(GraphicsDevice, stream);
                }
            }
            catch
            {
                terrainTexture = new Texture2D(GraphicsDevice, 1, 1);
                terrainTexture.SetData(new[] { Color.Green });
            }

            effect = new BasicEffect(GraphicsDevice);
            effect.TextureEnabled = true;
            effect.Texture = terrainTexture;
            effect.EnableDefaultLighting();
            effect.PreferPerPixelLighting = true;

            vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalTexture),
                                           terrainData.Vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(terrainData.Vertices);

            indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits,
                                        terrainData.Indices.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(terrainData.Indices);
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();

            if (keyboardState.IsKeyDown(Keys.Escape))
                Exit();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float moveSpeed = 20.0f * deltaTime;
            float rotSpeed = 2.0f * deltaTime;


            if (mouseState.RightButton == ButtonState.Pressed)
            {
                float deltaX = mouseState.X - previousMouseState.X;
                float deltaY = mouseState.Y - previousMouseState.Y;

                cameraYaw -= deltaX * 0.01f;
                cameraPitch -= deltaY * 0.01f;
                cameraPitch = MathHelper.Clamp(cameraPitch, -1.5f, 1.5f);
            }

            Vector3 forward = new Vector3(
                (float)(Math.Cos(cameraPitch) * Math.Sin(cameraYaw)),
                (float)Math.Sin(cameraPitch),
                (float)(Math.Cos(cameraPitch) * Math.Cos(cameraYaw))
            );

            Vector3 right = Vector3.Cross(forward, Vector3.Up);
            right.Normalize();

            if (keyboardState.IsKeyDown(Keys.W))
                cameraPosition += forward * moveSpeed;
            if (keyboardState.IsKeyDown(Keys.S))
                cameraPosition -= forward * moveSpeed;
            if (keyboardState.IsKeyDown(Keys.A))
                cameraPosition -= right * moveSpeed;
            if (keyboardState.IsKeyDown(Keys.D))
                cameraPosition += right * moveSpeed;
            if (keyboardState.IsKeyDown(Keys.Space))
                cameraPosition.Y += moveSpeed;
            if (keyboardState.IsKeyDown(Keys.LeftShift))
                cameraPosition.Y -= moveSpeed;

            cameraTarget = cameraPosition + forward;

            previousMouseState = mouseState;
            previousKeyboardState = keyboardState;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            Matrix view = Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Up);
            Matrix projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(60),
                GraphicsDevice.Viewport.AspectRatio,
                0.1f, 1000f);

            effect.View = view;
            effect.Projection = projection;
            effect.World = Matrix.Identity;

            GraphicsDevice.SetVertexBuffer(vertexBuffer);
            GraphicsDevice.Indices = indexBuffer;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0,
                                                    terrainData.Indices.Length / 3);
            }

            base.Draw(gameTime);
        }
    }
}