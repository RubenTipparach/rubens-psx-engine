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

        // Stress tracking
        private float currentStress = 0f;
        private const float MaxStress = 100f;

        // Stress thresholds (loaded from config or defaults)
        private float doubtEffectiveThreshold = 30f;   // Default: 30%
        private float accuseEffectiveThreshold = 50f;  // Default: 50%

        // Events
        public event Action<float> OnStressChanged; // Fires with new stress percentage
        public event Action OnMaxStressReached; // Fires when stress hits 100%

        public CharacterStateMachine(CharacterConfig characterConfig)
        {
            config = characterConfig;
            currentState = "initial";
            flags = new Dictionary<string, bool>();
            dialogueHistory = new List<string>();
            transcriptSubjects = new Dictionary<string, List<string>>();
            subjectOrder = new List<string>();
            currentStress = 0f;

            // Load stress thresholds from config if available
            if (characterConfig.stress_thresholds != null)
            {
                doubtEffectiveThreshold = characterConfig.stress_thresholds.doubt_effective;
                accuseEffectiveThreshold = characterConfig.stress_thresholds.accuse_effective;
                Console.WriteLine($"[{config.name}] Loaded stress thresholds: Doubt={doubtEffectiveThreshold}%, Accuse={accuseEffectiveThreshold}%");
            }
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
        /// Get current stress level (0-100)
        /// </summary>
        public float CurrentStress => currentStress;

        /// <summary>
        /// Get stress as percentage (0-100%)
        /// </summary>
        public float StressPercentage => (currentStress / MaxStress) * 100f;

        /// <summary>
        /// Check if stress has reached maximum
        /// </summary>
        public bool IsMaxStress => currentStress >= MaxStress;

        /// <summary>
        /// Check if stress is at or above 50%
        /// </summary>
        public bool IsHighStress => currentStress >= (MaxStress * 0.5f);

        /// <summary>
        /// Check if doubt action is currently effective based on stress threshold
        /// </summary>
        public bool IsDoubtEffective => StressPercentage >= doubtEffectiveThreshold;

        /// <summary>
        /// Check if accuse action is currently effective based on stress threshold
        /// </summary>
        public bool IsAccuseEffective => StressPercentage >= accuseEffectiveThreshold;

        /// <summary>
        /// Get doubt effective threshold
        /// </summary>
        public float DoubtEffectiveThreshold => doubtEffectiveThreshold;

        /// <summary>
        /// Get accuse effective threshold
        /// </summary>
        public float AccuseEffectiveThreshold => accuseEffectiveThreshold;

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
        /// Get evidence dialogue based on evidence ID and current stress level
        /// Returns the appropriate dialogue variant based on stress thresholds
        /// </summary>
        protected CharacterDialogueSequence GetEvidenceDialogue(string evidenceId)
        {
            if (config.dialogue == null)
                return null;

            // Find all evidence dialogues for this evidence ID
            var evidenceDialogues = config.dialogue
                .Where(d => d.action == "present_evidence" && d.evidence_id == evidenceId)
                .ToList();

            if (evidenceDialogues.Count == 0)
                return null;

            // Filter by stress level
            float currentStressPercent = StressPercentage;

            foreach (var dialogue in evidenceDialogues)
            {
                // Check stress_above threshold (default 0 if not set)
                float minStress = dialogue.requires_stress_above;
                float maxStress = dialogue.requires_stress_below > 0 ? dialogue.requires_stress_below : 100f;

                if (currentStressPercent >= minStress && currentStressPercent < maxStress)
                {
                    Console.WriteLine($"[{config.name}] Found evidence dialogue for {evidenceId} at {currentStressPercent:F1}% stress: {dialogue.sequence_name}");
                    return dialogue;
                }
            }

            // Fallback: return first dialogue if no stress match
            Console.WriteLine($"[{config.name}] No stress-matched dialogue for {evidenceId}, using first variant");
            return evidenceDialogues.FirstOrDefault();
        }

        /// <summary>
        /// Convert YAML dialogue to game DialogueSequence
        /// </summary>
        public DialogueSequence ConvertToDialogueSequence(CharacterDialogueSequence yamlDialogue)
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

        /// <summary>
        /// Increase stress by a given amount
        /// </summary>
        public void IncreaseStress(float amount)
        {
            if (amount <= 0) return;

            float previousStress = currentStress;
            currentStress = Math.Min(currentStress + amount, MaxStress);

            Console.WriteLine($"[{config.name}] Stress increased by {amount:F1} (was {previousStress:F1}, now {currentStress:F1}, {StressPercentage:F1}%)");

            OnStressChanged?.Invoke(StressPercentage);

            // Check if max stress reached
            if (previousStress < MaxStress && currentStress >= MaxStress)
            {
                Console.WriteLine($"[{config.name}] MAX STRESS REACHED - character will self-dismiss");
                OnMaxStressReached?.Invoke();
            }
        }

        /// <summary>
        /// Reset stress to 0
        /// </summary>
        public void ResetStress()
        {
            currentStress = 0f;
            Console.WriteLine($"[{config.name}] Stress reset to 0%");
            OnStressChanged?.Invoke(0f);
        }
    }
}
