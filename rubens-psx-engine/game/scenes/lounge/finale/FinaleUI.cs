using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using System;
using System.Collections.Generic;

namespace anakinsoft.game.scenes.lounge.finale
{
    /// <summary>
    /// UI for displaying finale questions and answer options
    /// </summary>
    public class FinaleUI
    {
        private SpriteFont font;
        private bool isActive;
        private FinaleQuestion currentQuestion;
        private int selectedAnswerIndex;
        private List<Rectangle> answerButtonBounds;
        private MouseState previousMouseState;
        private Texture2D whitePixel;

        // UI layout constants
        private const int QuestionY = 150;
        private const int AnswerStartY = 250;
        private const int AnswerSpacing = 60;
        private const int AnswerPadding = 15;
        private const int ButtonWidth = 800;
        private const int ButtonHeight = 50;

        // Colors
        private readonly Color questionColor = Color.White;
        private readonly Color answerHoverColor = new Color(100, 150, 255);
        private readonly Color answerNormalColor = new Color(40, 40, 60);
        private readonly Color answerTextColor = Color.White;
        private readonly Color headerColor = new Color(255, 200, 100);

        public bool IsActive => isActive;
        public int SelectedAnswerIndex => selectedAnswerIndex;
        public bool HasSelection => selectedAnswerIndex >= 0;

        public event Action<int> OnAnswerSelected;

        public FinaleUI()
        {
            answerButtonBounds = new List<Rectangle>();
            selectedAnswerIndex = -1;
        }

        public void Initialize()
        {
            font = Globals.screenManager.Content.Load<SpriteFont>("fonts/Arial");

            // Create 1x1 white pixel texture for drawing UI shapes
            whitePixel = new Texture2D(Globals.screenManager.GraphicsDevice, 1, 1);
            whitePixel.SetData(new[] { Color.White });
        }

        public void Show(FinaleQuestion question)
        {
            isActive = true;
            currentQuestion = question;
            selectedAnswerIndex = -1;
            BuildAnswerButtons();
            Console.WriteLine("[FinaleUI] Showing question: {0}", question.QuestionText);
        }

        public void Hide()
        {
            isActive = false;
            currentQuestion = null;
            selectedAnswerIndex = -1;
            answerButtonBounds.Clear();
        }

        private void BuildAnswerButtons()
        {
            answerButtonBounds.Clear();

            if (currentQuestion == null) return;

            int screenWidth = Globals.screenManager.GraphicsDevice.Viewport.Width;
            int screenHeight = Globals.screenManager.GraphicsDevice.Viewport.Height;

            for (int i = 0; i < currentQuestion.AnswerOptions.Count; i++)
            {
                int y = AnswerStartY + (i * AnswerSpacing);
                int x = (screenWidth - ButtonWidth) / 2;

                answerButtonBounds.Add(new Rectangle(x, y, ButtonWidth, ButtonHeight));
            }
        }

        public void Update(GameTime gameTime)
        {
            if (!isActive || currentQuestion == null) return;

            var mouseState = Mouse.GetState();
            var mousePosition = new Point(mouseState.X, mouseState.Y);

            // Check hover and click on answer buttons
            for (int i = 0; i < answerButtonBounds.Count; i++)
            {
                if (answerButtonBounds[i].Contains(mousePosition))
                {
                    // Check for click (mouse down and released)
                    if (mouseState.LeftButton == ButtonState.Released &&
                        previousMouseState.LeftButton == ButtonState.Pressed)
                    {
                        selectedAnswerIndex = i;
                        OnAnswerSelected?.Invoke(i);
                        Console.WriteLine("[FinaleUI] Answer selected: {0} - {1}", i, currentQuestion.AnswerOptions[i]);
                    }
                }
            }

            previousMouseState = mouseState;
        }

        public void Draw(SpriteBatch spriteBatch, int questionNumber, int totalQuestions)
        {
            if (!isActive || currentQuestion == null) return;

            int screenWidth = Globals.screenManager.GraphicsDevice.Viewport.Width;
            var mousePosition = new Point(Mouse.GetState().X, Mouse.GetState().Y);

            // Draw dark background overlay
            var fullScreenRect = new Rectangle(0, 0, screenWidth, Globals.screenManager.GraphicsDevice.Viewport.Height);
            spriteBatch.Draw(
                whitePixel,
                fullScreenRect,
                new Color(0, 0, 0, 220)
            );

            // Draw header (question number)
            string headerText = $"QUESTION {questionNumber} OF {totalQuestions}";
            Vector2 headerSize = font.MeasureString(headerText);
            Vector2 headerPos = new Vector2((screenWidth - headerSize.X) / 2, 80);
            spriteBatch.DrawString(font, headerText, headerPos, headerColor);

            // Draw category
            string categoryText = $"[{currentQuestion.Category}]";
            Vector2 categorySize = font.MeasureString(categoryText);
            Vector2 categoryPos = new Vector2((screenWidth - categorySize.X) / 2, 115);
            spriteBatch.DrawString(font, categoryText, categoryPos, new Color(150, 150, 150));

            // Draw question text
            Vector2 questionSize = font.MeasureString(currentQuestion.QuestionText);
            Vector2 questionPos = new Vector2((screenWidth - questionSize.X) / 2, QuestionY);
            spriteBatch.DrawString(font, currentQuestion.QuestionText, questionPos, questionColor);

            // Draw answer buttons
            for (int i = 0; i < currentQuestion.AnswerOptions.Count; i++)
            {
                Rectangle buttonBounds = answerButtonBounds[i];
                bool isHovered = buttonBounds.Contains(mousePosition);
                Color buttonColor = isHovered ? answerHoverColor : answerNormalColor;

                // Draw button background
                spriteBatch.Draw(
                    whitePixel,
                    buttonBounds,
                    buttonColor
                );

                // Draw button border
                DrawRectangleBorder(spriteBatch, buttonBounds, Color.White, 2);

                // Draw answer text
                string answerText = $"{i + 1}. {currentQuestion.AnswerOptions[i]}";
                Vector2 textPos = new Vector2(
                    buttonBounds.X + AnswerPadding,
                    buttonBounds.Y + (buttonBounds.Height - font.LineSpacing) / 2
                );
                spriteBatch.DrawString(font, answerText, textPos, answerTextColor);
            }
        }

        private void DrawRectangleBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            // Top
            spriteBatch.Draw(whitePixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(whitePixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            // Left
            spriteBatch.Draw(whitePixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            spriteBatch.Draw(whitePixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}
