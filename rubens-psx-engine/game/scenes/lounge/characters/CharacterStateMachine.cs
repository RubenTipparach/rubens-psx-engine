using System;
using System.Collections.Generic;
using System.Linq;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// Base state machine for character dialogue and behavior
    /// Each character type derives from this to implement their own logic
    /// </summary>
    public abstract class CharacterStateMachine
    {
        protected CharacterConfig config;
        protected string currentState;
        protected Dictionary<string, bool> flags;  // Track game state flags
        protected List<string> dialogueHistory;   // Track what's been said

        public CharacterStateMachine(CharacterConfig characterConfig)
        {
            config = characterConfig;
            currentState = "initial";
            flags = new Dictionary<string, bool>();
            dialogueHistory = new List<string>();
        }

        /// <summary>
        /// Get the character's name
        /// </summary>
        public string CharacterName => config.name;

        /// <summary>
        /// Get current state name
        /// </summary>
        public string CurrentState => currentState;

        /// <summary>
        /// Get the next dialogue sequence based on current state
        /// </summary>
        public abstract CharacterDialogueSequence GetCurrentDialogue();

        /// <summary>
        /// Handle dialogue completion and state transitions
        /// </summary>
        public abstract void OnDialogueComplete(string sequenceName);

        /// <summary>
        /// Handle player actions/responses
        /// </summary>
        public abstract void OnPlayerAction(string action, object data = null);

        /// <summary>
        /// Set a state flag
        /// </summary>
        public void SetFlag(string flagName, bool value)
        {
            flags[flagName] = value;
            Console.WriteLine($"[{config.name}] Flag set: {flagName} = {value}");
        }

        /// <summary>
        /// Check a state flag
        /// </summary>
        public bool GetFlag(string flagName)
        {
            return flags.ContainsKey(flagName) && flags[flagName];
        }

        /// <summary>
        /// Transition to a new state
        /// </summary>
        protected void TransitionTo(string newState)
        {
            Console.WriteLine($"[{config.name}] State transition: {currentState} -> {newState}");
            currentState = newState;
        }

        /// <summary>
        /// Mark dialogue as seen
        /// </summary>
        protected void MarkDialogueSeen(string sequenceName)
        {
            if (!dialogueHistory.Contains(sequenceName))
            {
                dialogueHistory.Add(sequenceName);
            }
        }

        /// <summary>
        /// Check if dialogue has been seen
        /// </summary>
        protected bool HasSeenDialogue(string sequenceName)
        {
            return dialogueHistory.Contains(sequenceName);
        }

        /// <summary>
        /// Get dialogue sequence by name from config
        /// </summary>
        protected CharacterDialogueSequence GetDialogueSequence(string sequenceName)
        {
            return config.dialogue?.FirstOrDefault(d => d.sequence_name == sequenceName);
        }

        /// <summary>
        /// Convert YAML dialogue to game DialogueSequence
        /// </summary>
        protected DialogueSequence ConvertToDialogueSequence(CharacterDialogueSequence yamlDialogue)
        {
            if (yamlDialogue == null) return null;

            var sequence = new DialogueSequence(yamlDialogue.sequence_name);

            foreach (var line in yamlDialogue.lines)
            {
                sequence.AddLine(line.speaker, line.text);
            }

            // Handle completion action
            if (!string.IsNullOrEmpty(yamlDialogue.on_complete))
            {
                sequence.OnSequenceComplete = () =>
                {
                    OnDialogueComplete(yamlDialogue.sequence_name);
                };
            }

            return sequence;
        }
    }
}
