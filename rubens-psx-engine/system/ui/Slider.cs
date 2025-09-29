using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace rubens_psx_engine.system.ui
{
    public class Slider
    {
        private Rectangle bounds;
        private Rectangle sliderBounds;
        private bool isDragging = false;
        private float minValue;
        private float maxValue;
        private float currentValue;
        private string label;
        private SpriteFont font;

        public float Value
        {
            get => currentValue;
            set => currentValue = MathHelper.Clamp(value, minValue, maxValue);
        }

        public event Action<float> ValueChanged;

        public Slider(Rectangle bounds, float minValue, float maxValue, float initialValue, string label, SpriteFont font)
        {
            this.bounds = bounds;
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.currentValue = MathHelper.Clamp(initialValue, minValue, maxValue);
            this.label = label;
            this.font = font;

            UpdateSliderBounds();
        }

        public void Update(GameTime gameTime)
        {
            var mouseState = Mouse.GetState();
            var mousePos = new Point(mouseState.X, mouseState.Y);

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                if (!isDragging && bounds.Contains(mousePos))
                {
                    isDragging = true;
                }

                if (isDragging)
                {
                    float normalizedX = MathHelper.Clamp((mousePos.X - bounds.X) / (float)bounds.Width, 0f, 1f);
                    float newValue = minValue + normalizedX * (maxValue - minValue);

                    if (Math.Abs(newValue - currentValue) > 0.001f)
                    {
                        currentValue = newValue;
                        UpdateSliderBounds();
                        ValueChanged?.Invoke(currentValue);
                    }
                }
            }
            else
            {
                isDragging = false;
            }
        }

        private void UpdateSliderBounds()
        {
            float normalized = (currentValue - minValue) / (maxValue - minValue);
            int sliderX = bounds.X + (int)(normalized * bounds.Width);
            sliderBounds = new Rectangle(sliderX - 5, bounds.Y - 2, 10, bounds.Height + 4);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            // Draw track
            spriteBatch.Draw(pixelTexture, bounds, Color.Gray);

            // Draw slider handle
            spriteBatch.Draw(pixelTexture, sliderBounds, isDragging ? Color.White : Color.LightGray);

            // Draw label and value
            string text = $"{label}: {currentValue:F2}";
            Vector2 textPos = new Vector2(bounds.X, bounds.Y - 25);
            spriteBatch.DrawString(font, text, textPos + Vector2.One, Color.Black);
            spriteBatch.DrawString(font, text, textPos, Color.White);
        }
    }
}