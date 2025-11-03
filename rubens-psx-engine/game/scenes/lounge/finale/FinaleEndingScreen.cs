using anakinsoft.system.cameras;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using rubens_psx_engine;
using System;

namespace anakinsoft.game.scenes.lounge.finale
{
    /// <summary>
    /// Displays the success or failure ending screen with stats
    /// </summary>
    public class FinaleEndingScreen
    {
        private SpriteFont font;
        private bool isActive;
        private FinaleResults results;
        private MouseState previousMouseState;
        private Rectangle restartButtonBounds;
        private Rectangle quitButtonBounds;
        private Texture2D whitePixel;

        // Colors
        private readonly Color successColor = new Color(100, 255, 100);
        private readonly Color failureColor = new Color(255, 100, 100);
        private readonly Color statsColor = Color.White;
        private readonly Color correctColor = new Color(100, 255, 100);
        private readonly Color wrongColor = new Color(255, 100, 100);
        private readonly Color buttonHoverColor = new Color(100, 150, 255);
        private readonly Color buttonNormalColor = new Color(40, 40, 60);

        public bool IsActive => isActive;

        public event Action OnRestartRequested;
        public event Action OnQuitRequested;

        public FinaleEndingScreen()
        {
        }

        public void Initialize()
        {
            font = Globals.screenManager.Content.Load<SpriteFont>("fonts/Arial");

            // Create 1x1 white pixel texture for drawing UI shapes
            whitePixel = new Texture2D(Globals.screenManager.GraphicsDevice, 1, 1);
            whitePixel.SetData(new[] { Color.White });
        }

        public void Show(FinaleResults finaleResults, Camera camera)
        {
            isActive = true;
            results = finaleResults;

            // Position camera at (0, 20, 0) looking towards positive Z (0, 20, 10)
            camera.Position = new Vector3(0, 20, 0);
            Vector3 lookTarget = new Vector3(0, 20, 10);
            Vector3 lookDirection = Vector3.Normalize(lookTarget - camera.Position);

            // Create rotation quaternion from look direction
            Quaternion rotation = Quaternion.CreateFromRotationMatrix(
                Matrix.CreateLookAt(Vector3.Zero, lookDirection, Vector3.Up)
            );
            rotation = Quaternion.Inverse(rotation); // Invert because CreateLookAt gives view space

            camera.SetRotation(rotation);

            // Show mouse cursor
            Globals.screenManager.IsMouseVisible = true;

            int screenWidth = Globals.screenManager.GraphicsDevice.Viewport.Width;
            int screenHeight = Globals.screenManager.GraphicsDevice.Viewport.Height;

            // Position buttons at bottom
            int buttonWidth = 330;
            int buttonHeight = 50;
            int buttonY = screenHeight - 100;
            int buttonSpacing = 50;

            restartButtonBounds = new Rectangle(
                (screenWidth / 2) - buttonWidth - buttonSpacing / 2,
                buttonY,
                buttonWidth,
                buttonHeight
            );

            quitButtonBounds = new Rectangle(
                (screenWidth / 2) + buttonSpacing / 2,
                buttonY,
                buttonWidth,
                buttonHeight
            );

            Console.WriteLine("[FinaleEndingScreen] Showing ending - Success: {0}, Accuracy: {1:F1}%, Camera: {2}",
                results.Success, results.AccuracyPercentage, camera.Position);
        }

        public void Hide()
        {
            isActive = false;
            results = null;
        }

        public void Update(GameTime gameTime)
        {
            if (!isActive || results == null) return;

            var mouseState = Mouse.GetState();
            var mousePosition = new Point(mouseState.X, mouseState.Y);

            // Check for button clicks
            if (mouseState.LeftButton == ButtonState.Released &&
                previousMouseState.LeftButton == ButtonState.Pressed)
            {
                if (restartButtonBounds.Contains(mousePosition))
                {
                    OnRestartRequested?.Invoke();
                }
                else if (quitButtonBounds.Contains(mousePosition))
                {
                    OnQuitRequested?.Invoke();
                }
            }

            previousMouseState = mouseState;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!isActive || results == null) return;

            int screenWidth = Globals.screenManager.GraphicsDevice.Viewport.Width;
            int screenHeight = Globals.screenManager.GraphicsDevice.Viewport.Height;
            var mousePosition = new Point(Mouse.GetState().X, Mouse.GetState().Y);

            // Draw dark background
            var fullScreenRect = new Rectangle(0, 0, screenWidth, screenHeight);
            spriteBatch.Draw(
                whitePixel,
                fullScreenRect,
                new Color(0, 0, 0, 240)
            );

            int yOffset = 100;

            if (results.Success)
            {
                DrawSuccessScreen(spriteBatch, screenWidth, ref yOffset);
            }
            else
            {
                DrawFailureScreen(spriteBatch, screenWidth, ref yOffset);
            }

            // Draw buttons
            DrawButton(spriteBatch, restartButtonBounds, "RESTART INVESTIGATION", mousePosition);
            DrawButton(spriteBatch, quitButtonBounds, "QUIT TO MENU", mousePosition);
        }

        private void DrawSuccessScreen(SpriteBatch spriteBatch, int screenWidth, ref int yOffset)
        {
            // Success title
            string title = "=== CASE SOLVED ===";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2((screenWidth - titleSize.X) / 2, yOffset);
            spriteBatch.DrawString(font, title, titlePos, successColor);
            yOffset += 80;

            // Success message
            string message = "CONGRATULATIONS, DETECTIVE!";
            Vector2 messageSize = font.MeasureString(message);
            Vector2 messagePos = new Vector2((screenWidth - messageSize.X) / 2, yOffset);
            spriteBatch.DrawString(font, message, messagePos, successColor);
            yOffset += 60;

            // Flavor text
            string[] successText = new string[]
            {
                "You correctly identified Commander Sylara Von and Dr. Lyssa Thorne",
                "as the conspirators behind Ambassador Lorath's murder.",
                "",
                "The Telirian delegation accepts your findings.",
                "The peace treaty will proceed. War has been averted.",
                "",
                "Billions of lives have been saved because of your deductive work."
            };

            foreach (var line in successText)
            {
                Vector2 lineSize = font.MeasureString(line);
                Vector2 linePos = new Vector2((screenWidth - lineSize.X) / 2, yOffset);
                spriteBatch.DrawString(font, line, linePos, statsColor);
                yOffset += 35;
            }

            // Stats
            yOffset += 20;
            string stats = $"Perfect Score: {results.TotalQuestions}/{results.TotalQuestions} Correct";
            Vector2 statsSize = font.MeasureString(stats);
            Vector2 statsPos = new Vector2((screenWidth - statsSize.X) / 2, yOffset);
            spriteBatch.DrawString(font, stats, statsPos, correctColor);
        }

        private void DrawFailureScreen(SpriteBatch spriteBatch, int screenWidth, ref int yOffset)
        {
            // Failure title
            string title = "=== CASE FAILED ===";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2((screenWidth - titleSize.X) / 2, yOffset);
            spriteBatch.DrawString(font, title, titlePos, failureColor);
            yOffset += 80;

            // Failure message
            string message = "INSUFFICIENT EVIDENCE";
            Vector2 messageSize = font.MeasureString(message);
            Vector2 messagePos = new Vector2((screenWidth - messageSize.X) / 2, yOffset);
            spriteBatch.DrawString(font, message, messagePos, failureColor);
            yOffset += 60;

            // Flavor text
            string[] failureText = new string[]
            {
                "The Telirian delegation rejects your accusation.",
                "Unable to deliver justice, diplomatic relations collapse.",
                "",
                "The Telirian warships board the station.",
                "War breaks out across three star systems.",
                "",
                "14 billion lives lost."
            };

            foreach (var line in failureText)
            {
                Vector2 lineSize = font.MeasureString(line);
                Vector2 linePos = new Vector2((screenWidth - lineSize.X) / 2, yOffset);
                spriteBatch.DrawString(font, line, linePos, failureColor);
                yOffset += 35;
            }

            // Stats
            yOffset += 30;
            string stats = $"Score: {results.CorrectAnswers}/{results.TotalQuestions} Correct ({results.AccuracyPercentage:F0}%)";
            Vector2 statsSize = font.MeasureString(stats);
            Vector2 statsPos = new Vector2((screenWidth - statsSize.X) / 2, yOffset);
            spriteBatch.DrawString(font, stats, statsPos, statsColor);
            yOffset += 50;

            // Show what they got wrong
            string detailsHeader = "=== REVIEW ===";
            Vector2 detailsHeaderSize = font.MeasureString(detailsHeader);
            Vector2 detailsHeaderPos = new Vector2((screenWidth - detailsHeaderSize.X) / 2, yOffset);
            spriteBatch.DrawString(font, detailsHeader, detailsHeaderPos, statsColor);
            yOffset += 40;

            // List each question result (first 3 for space)
            int questionsToShow = Math.Min(3, results.QuestionResults.Count);
            for (int i = 0; i < questionsToShow; i++)
            {
                var questionResult = results.QuestionResults[i];
                Color resultColor = questionResult.WasCorrect ? correctColor : wrongColor;
                string resultText = questionResult.WasCorrect ? "[OK]" : "[X]";

                string questionText = $"{resultText} {questionResult.Category}";
                if (!questionResult.WasCorrect)
                {
                    questionText += $" - Wrong: {questionResult.PlayerAnswer}";
                }

                Vector2 questionTextSize = font.MeasureString(questionText);
                Vector2 questionTextPos = new Vector2((screenWidth - questionTextSize.X) / 2, yOffset);
                spriteBatch.DrawString(font, questionText, questionTextPos, resultColor);
                yOffset += 30;
            }
        }

        private void DrawButton(SpriteBatch spriteBatch, Rectangle bounds, string text, Point mousePosition)
        {
            bool isHovered = bounds.Contains(mousePosition);
            Color buttonColor = isHovered ? buttonHoverColor : buttonNormalColor;

            // Background
            spriteBatch.Draw(whitePixel, bounds, buttonColor);

            // Border
            int thickness = 2;
            spriteBatch.Draw(whitePixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), Color.White);
            spriteBatch.Draw(whitePixel, new Rectangle(bounds.X, bounds.Y + bounds.Height - thickness, bounds.Width, thickness), Color.White);
            spriteBatch.Draw(whitePixel, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), Color.White);
            spriteBatch.Draw(whitePixel, new Rectangle(bounds.X + bounds.Width - thickness, bounds.Y, thickness, bounds.Height), Color.White);

            // Text
            Vector2 textSize = font.MeasureString(text);
            Vector2 textPos = new Vector2(
                bounds.X + (bounds.Width - textSize.X) / 2,
                bounds.Y + (bounds.Height - textSize.Y) / 2
            );
            spriteBatch.DrawString(font, text, textPos, Color.White);
        }
    }
}
