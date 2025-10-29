using System;
using System.Collections.Generic;
using System.Linq;
using anakinsoft.system;
using anakinsoft.game.scenes.lounge.ui;

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

        // Transcript tracking: Each subject (sequence name) maps to list of character's lines
        protected Dictionary<string, List<string>> transcriptSubjects;
        protected List<string> subjectOrder; // Track order subjects were discussed

        public CharacterStateMachine(CharacterConfig characterConfig)
        {
            config = characterConfig;
            currentState = "initial";
            flags = new Dictionary<string, bool>();
            dialogueHistory = new List<string>();
            transcriptSubjects = new Dictionary<string, List<string>>();
            subjectOrder = new List<string>();
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
        /// Handle dialogue completion and state transitions (sealed - calls OnDialogueCompleteInternal)
        /// Automatically records transcript and calls derived class implementation
        /// </summary>
        public void OnDialogueComplete(string sequenceName)
        {
            // Mark as seen in history
            MarkDialogueSeen(sequenceName);

            // Automatically record transcript subject
            var dialogue = GetDialogueSequence(sequenceName);
            if (dialogue != null)
            {
                RecordTranscriptSubject(dialogue);
            }

            // Call derived class implementation for state transitions
            OnDialogueCompleteInternal(sequenceName);
        }

        /// <summary>
        /// Internal method for derived classes to handle dialogue completion logic
        /// (state transitions, flags, etc.)
        /// </summary>
        protected abstract void OnDialogueCompleteInternal(string sequenceName);

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

        /// <summary>
        /// Record a dialogue sequence as a transcript subject
        /// Extracts only this character's lines and stores them under the sequence name
        /// </summary>
        protected void RecordTranscriptSubject(CharacterDialogueSequence yamlDialogue)
        {
            if (yamlDialogue == null) return;

            string subject = yamlDialogue.sequence_name;

            // Skip if we've already recorded this subject
            if (transcriptSubjects.ContainsKey(subject))
                return;

            // Extract only this character's lines
            List<string> characterLines = new List<string>();
            foreach (var line in yamlDialogue.lines)
            {
                if (line.speaker == config.name)
                {
                    characterLines.Add(line.text);
                }
            }

            // Record subject
            transcriptSubjects[subject] = characterLines;
            subjectOrder.Add(subject);

            Console.WriteLine($"[{config.name}] Recorded transcript subject: {subject} ({characterLines.Count} lines)");
        }

        /// <summary>
        /// Get all transcript subjects in the order they were discussed
        /// </summary>
        public List<string> GetTranscriptSubjects()
        {
            return new List<string>(subjectOrder);
        }

        /// <summary>
        /// Get lines for a specific subject
        /// </summary>
        public List<string> GetSubjectLines(string subject)
        {
            if (transcriptSubjects.ContainsKey(subject))
                return new List<string>(transcriptSubjects[subject]);
            return new List<string>();
        }

        /// <summary>
        /// Check if character has been interviewed (has any transcript subjects)
        /// </summary>
        public bool HasBeenInterviewed()
        {
            return subjectOrder.Count > 0;
        }
    }
}
