using System.Collections.Generic;

namespace anakinsoft.game.scenes.lounge.finale
{
    /// <summary>
    /// Represents a single question in the finale interrogation
    /// </summary>
    public class FinaleQuestion
    {
        public string QuestionText { get; set; }
        public List<string> AnswerOptions { get; set; }
        public int CorrectAnswerIndex { get; set; }
        public string Category { get; set; } // "killer", "method", "motive", etc.

        public FinaleQuestion()
        {
            AnswerOptions = new List<string>();
        }

        public bool IsCorrectAnswer(int selectedIndex)
        {
            return selectedIndex == CorrectAnswerIndex;
        }
    }

    /// <summary>
    /// Results from the finale interrogation
    /// </summary>
    public class FinaleResults
    {
        public bool Success { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public List<FinaleQuestionResult> QuestionResults { get; set; }

        public FinaleResults()
        {
            QuestionResults = new List<FinaleQuestionResult>();
        }

        public float AccuracyPercentage => TotalQuestions > 0 ? (float)CorrectAnswers / TotalQuestions * 100f : 0f;
    }

    /// <summary>
    /// Result for a single question
    /// </summary>
    public class FinaleQuestionResult
    {
        public string QuestionText { get; set; }
        public string Category { get; set; }
        public bool WasCorrect { get; set; }
        public string PlayerAnswer { get; set; }
        public string CorrectAnswer { get; set; }
    }
}
