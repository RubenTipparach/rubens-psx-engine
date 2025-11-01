using System;
using System.Collections.Generic;
using anakinsoft.system;
using anakinsoft.game.scenes.lounge.ui;

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
        private bool allCharactersDismissed = false; // True when both characters dismissed, waiting for player to continue

        // Track which characters are currently being interrogated
        private List<SelectableCharacter> currentInterrogationPair;
        private HashSet<string> dismissedCharacters = new HashSet<string>();

        public int CurrentRound => currentRound;
        public int HoursRemaining => hoursRemaining;
        public bool IsInterrogating => isInterrogating;
        public bool AllCharactersDismissed => allCharactersDismissed;
        public List<SelectableCharacter> CurrentPair => currentInterrogationPair;

        // Events
        public event Action<int> OnRoundStarted; // Fires with hours remaining
        public event Action OnBothCharactersDismissed; // Fires when both dismissed, characters still seated
        public event Action<int> OnRoundEnded; // Fires when player confirms next round
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
            allCharactersDismissed = false;
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

            // Mark the character as dismissed in the SelectableCharacter object
            var character = currentInterrogationPair?.Find(c => c.Name == characterName);
            if (character != null)
            {
                character.IsDismissed = true;
                Console.WriteLine($"[InterrogationRoundManager] Marked {characterName} as dismissed");
            }

            Console.WriteLine($"[InterrogationRoundManager] Dismissed {characterName} ({dismissedCharacters.Count}/{currentInterrogationPair.Count})");

            // Check if both characters have been dismissed
            if (dismissedCharacters.Count >= currentInterrogationPair.Count)
            {
                allCharactersDismissed = true;
                Console.WriteLine("[InterrogationRoundManager] Both characters dismissed - waiting for player to continue");
                OnBothCharactersDismissed?.Invoke();
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
        /// Continue to next round after both characters dismissed
        /// Called when player selects new characters after dismissing both
        /// </summary>
        public void ContinueToNextRound()
        {
            if (!allCharactersDismissed)
            {
                Console.WriteLine("[InterrogationRoundManager] ERROR: Cannot continue - not all characters dismissed");
                return;
            }

            EndRound();
        }

        /// <summary>
        /// End the current round (private - called by ContinueToNextRound)
        /// </summary>
        private void EndRound()
        {
            isInterrogating = false;
            allCharactersDismissed = false;
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
