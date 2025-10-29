using System;
using System.Collections.Generic;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge
{
    /// <summary>
    /// Manages interrogation rounds, time tracking, and character rotation
    /// </summary>
    public class InterrogationRoundManager
    {
        private int totalRounds = 3;
        private int currentRound = 0;
        private int hoursRemaining = 3;
        private bool isInterrogating = false;

        // Track which characters are currently being interrogated
        private List<SelectableCharacter> currentInterrogationPair;
        private HashSet<string> dismissedCharacters = new HashSet<string>();

        public int CurrentRound => currentRound;
        public int HoursRemaining => hoursRemaining;
        public bool IsInterrogating => isInterrogating;
        public List<SelectableCharacter> CurrentPair => currentInterrogationPair;

        // Events
        public event Action<int> OnRoundStarted; // Fires with hours remaining
        public event Action<int> OnRoundEnded;
        public event Action OnAllRoundsComplete;
        public event Action<List<SelectableCharacter>> OnCharactersSpawned; // Request to spawn characters

        public InterrogationRoundManager()
        {
            hoursRemaining = totalRounds;
        }

        /// <summary>
        /// Start a new interrogation round with the selected characters
        /// </summary>
        public void StartRound(List<SelectableCharacter> selectedCharacters)
        {
            if (currentRound >= totalRounds)
            {
                Console.WriteLine("[InterrogationRoundManager] All rounds complete");
                OnAllRoundsComplete?.Invoke();
                return;
            }

            currentRound++;
            currentInterrogationPair = new List<SelectableCharacter>(selectedCharacters);
            dismissedCharacters.Clear();
            isInterrogating = true;

            Console.WriteLine($"[InterrogationRoundManager] Starting round {currentRound}/{totalRounds} - {hoursRemaining} hours remaining");

            OnRoundStarted?.Invoke(hoursRemaining);
            OnCharactersSpawned?.Invoke(currentInterrogationPair);
        }

        /// <summary>
        /// Dismiss a character from interrogation
        /// </summary>
        public void DismissCharacter(string characterName)
        {
            if (!isInterrogating || dismissedCharacters.Contains(characterName))
                return;

            dismissedCharacters.Add(characterName);
            Console.WriteLine($"[InterrogationRoundManager] Dismissed {characterName} ({dismissedCharacters.Count}/{currentInterrogationPair.Count})");

            // Check if both characters have been dismissed
            if (dismissedCharacters.Count >= currentInterrogationPair.Count)
            {
                EndRound();
            }
        }

        /// <summary>
        /// Check if a character has been dismissed
        /// </summary>
        public bool IsCharacterDismissed(string characterName)
        {
            return dismissedCharacters.Contains(characterName);
        }

        /// <summary>
        /// End the current round
        /// </summary>
        private void EndRound()
        {
            isInterrogating = false;
            hoursRemaining--;

            Console.WriteLine($"[InterrogationRoundManager] Round {currentRound} complete - {hoursRemaining} hours remaining");

            OnRoundEnded?.Invoke(hoursRemaining);

            // Check if all rounds are complete
            if (currentRound >= totalRounds || hoursRemaining <= 0)
            {
                Console.WriteLine("[InterrogationRoundManager] All interrogation rounds complete");
                OnAllRoundsComplete?.Invoke();
            }
        }

        /// <summary>
        /// Reset the manager for a new game
        /// </summary>
        public void Reset()
        {
            currentRound = 0;
            hoursRemaining = totalRounds;
            isInterrogating = false;
            currentInterrogationPair = null;
            dismissedCharacters.Clear();
        }
    }
}
