using System;
using System.Collections.Generic;

namespace anakinsoft.game.scenes.lounge.finale
{
    /// <summary>
    /// Manages the finale interrogation sequence based on interrogation_finale.md
    /// </summary>
    public class FinaleManager
    {
        private List<FinaleQuestion> questions;
        private int currentQuestionIndex;
        private FinaleResults results;
        private bool finaleStarted;
        private bool finaleCompleted;

        public bool IsFinaleActive => finaleStarted && !finaleCompleted;
        public bool IsFinaleCompleted => finaleCompleted;
        public FinaleQuestion CurrentQuestion => currentQuestionIndex < questions.Count ? questions[currentQuestionIndex] : null;
        public int CurrentQuestionNumber => currentQuestionIndex + 1;
        public int TotalQuestions => questions.Count;
        public FinaleResults Results => results;

        public FinaleManager()
        {
            questions = new List<FinaleQuestion>();
            results = new FinaleResults();
            InitializeQuestions();
        }

        private void InitializeQuestions()
        {
            // 7 detailed questions covering WHO, HOW, and WHY

            // Question 1: WHO killed the Ambassador?
            questions.Add(new FinaleQuestion
            {
                QuestionText = "WHO KILLED AMBASSADOR LORATH?",
                Category = "Primary Killer",
                AnswerOptions = new List<string>
                {
                    "Dr. Harmon Kerrigan",
                    "Commander Sylara Von",
                    "Dr. Lyssa Thorne",
                    "Lieutenant Marcus Webb"
                },
                CorrectAnswerIndex = 1 // Commander Sylara Von
            });

            // Question 2: WHO was the accomplice?
            questions.Add(new FinaleQuestion
            {
                QuestionText = "WHO WAS THE ACCOMPLICE?",
                Category = "Accomplice",
                AnswerOptions = new List<string>
                {
                    "Dr. Harmon Kerrigan",
                    "Dr. Lyssa Thorne",
                    "Lieutenant Marcus Webb",
                    "The killer acted alone"
                },
                CorrectAnswerIndex = 1 // Dr. Lyssa Thorne
            });

            // Question 3: Murder weapon
            questions.Add(new FinaleQuestion
            {
                QuestionText = "WHAT WAS THE MURDER WEAPON?",
                Category = "Method - Weapon",
                AnswerOptions = new List<string>
                {
                    "Poison in the ceremonial wine",
                    "Breturium injection causing radiation poisoning",
                    "Phaser set to lethal",
                    "Manual strangulation"
                },
                CorrectAnswerIndex = 1 // Breturium injection
            });

            // Question 4: How was Ambassador incapacitated?
            questions.Add(new FinaleQuestion
            {
                QuestionText = "HOW WAS THE AMBASSADOR INCAPACITATED BEFORE THE KILLING BLOW?",
                Category = "Method - Incapacitation",
                AnswerOptions = new List<string>
                {
                    "He wasn't incapacitated - killed while awake",
                    "Stunned with a phaser first",
                    "Sedated via drugged ceremonial wine",
                    "Physically restrained by force"
                },
                CorrectAnswerIndex = 2 // Sedated via drugged wine
            });

            // Question 5: Who administered the sedative?
            questions.Add(new FinaleQuestion
            {
                QuestionText = "WHO ADMINISTERED THE SEDATIVE?",
                Category = "Method - Sedative",
                AnswerOptions = new List<string>
                {
                    "Commander Sylara Von",
                    "Dr. Lyssa Thorne",
                    "Lucky Chen (unknowingly)",
                    "The Ambassador sedated himself"
                },
                CorrectAnswerIndex = 1 // Dr. Lyssa Thorne
            });

            // Question 6: Who performed the lethal injection?
            questions.Add(new FinaleQuestion
            {
                QuestionText = "WHO PERFORMED THE LETHAL INJECTION?",
                Category = "Method - Injection",
                AnswerOptions = new List<string>
                {
                    "Dr. Lyssa Thorne",
                    "Commander Sylara Von",
                    "They both administered partial doses",
                    "An automated medical device"
                },
                CorrectAnswerIndex = 1 // Commander Sylara Von
            });

            // Question 7: Primary motive
            questions.Add(new FinaleQuestion
            {
                QuestionText = "WHAT WAS COMMANDER VON'S PRIMARY MOTIVE?",
                Category = "Motive",
                AnswerOptions = new List<string>
                {
                    "Financial gain from treaty sabotage",
                    "Secret warmonger opposing peace",
                    "Romantic obsession with the Ambassador",
                    "Revenge for a family betrayal"
                },
                CorrectAnswerIndex = 2 // Romantic obsession with the Ambassador
            });
        }

        public void StartFinale()
        {
            finaleStarted = true;
            finaleCompleted = false;
            currentQuestionIndex = 0;
            results = new FinaleResults();
            results.TotalQuestions = questions.Count;
            Console.WriteLine("[FinaleManager] Finale started with {0} questions", questions.Count);
        }

        public bool SubmitAnswer(int selectedAnswerIndex)
        {
            if (!IsFinaleActive || CurrentQuestion == null)
            {
                Console.WriteLine("[FinaleManager] Cannot submit answer - finale not active or no current question");
                return false;
            }

            var question = CurrentQuestion;
            bool isCorrect = question.IsCorrectAnswer(selectedAnswerIndex);

            // Record result
            var questionResult = new FinaleQuestionResult
            {
                QuestionText = question.QuestionText,
                Category = question.Category,
                WasCorrect = isCorrect,
                PlayerAnswer = question.AnswerOptions[selectedAnswerIndex],
                CorrectAnswer = question.AnswerOptions[question.CorrectAnswerIndex]
            };
            results.QuestionResults.Add(questionResult);

            if (isCorrect)
            {
                results.CorrectAnswers++;
                Console.WriteLine("[FinaleManager] CORRECT! Question {0}/{1}: {2}", currentQuestionIndex + 1, questions.Count, question.Category);
            }
            else
            {
                Console.WriteLine("[FinaleManager] WRONG! Question {0}/{1}: {2}", currentQuestionIndex + 1, questions.Count, question.Category);
                Console.WriteLine("[FinaleManager]   Player answered: {0}", questionResult.PlayerAnswer);
                Console.WriteLine("[FinaleManager]   Correct answer: {0}", questionResult.CorrectAnswer);
            }

            // Move to next question
            currentQuestionIndex++;

            // Check if all questions answered
            if (currentQuestionIndex >= questions.Count)
            {
                // Success only if ALL questions were answered correctly
                bool success = results.CorrectAnswers == questions.Count;
                FinaleComplete(success);
            }

            return true;
        }

        private void FinaleComplete(bool success)
        {
            finaleCompleted = true;
            results.Success = success;

            if (success)
            {
                Console.WriteLine("[FinaleManager] === FINALE SUCCESS ===");
                Console.WriteLine("[FinaleManager] All {0} questions answered correctly!", results.TotalQuestions);
            }
            else
            {
                Console.WriteLine("[FinaleManager] === FINALE FAILURE ===");
                Console.WriteLine("[FinaleManager] Answered {0}/{1} questions correctly", results.CorrectAnswers, results.TotalQuestions);
            }
        }

        public void Reset()
        {
            finaleStarted = false;
            finaleCompleted = false;
            currentQuestionIndex = 0;
            results = new FinaleResults();
            Console.WriteLine("[FinaleManager] Finale reset");
        }
    }
}
