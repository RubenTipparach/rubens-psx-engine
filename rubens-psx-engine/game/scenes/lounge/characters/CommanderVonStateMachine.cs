using System;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// State machine for Commander Sylar Von
    /// Bodyguard with military precision and loyalty
    /// </summary>
    public class CommanderVonStateMachine : CharacterStateMachine
    {
        public CommanderVonStateMachine(CharacterConfig characterConfig)
            : base(characterConfig)
        {
        }

        public override CharacterDialogueSequence GetCurrentDialogue()
        {
            switch (currentState)
            {
                case "initial":
                    return GetDialogueSequence("CommanderVonInterrogation");

                case "interrogated":
                    var followUp = GetDialogueSequence("CommanderVonFollowUp");
                    return followUp ?? GetDialogueSequence("CommanderVonDefault");

                default:
                    Console.WriteLine($"[CommanderVonStateMachine] Unknown state: {currentState}");
                    return null;
            }
        }

        protected override void OnDialogueCompleteInternal(string sequenceName)
        {

            if (sequenceName == "CommanderVonInterrogation")
            {
                TransitionTo("interrogated");
                SetFlag("has_been_interrogated", true);
                Console.WriteLine("[CommanderVonStateMachine] Initial interrogation complete");
            }
        }

        public override void OnPlayerAction(string action, object data = null)
        {
            Console.WriteLine($"[CommanderVonStateMachine] Player action: {action}");

            switch (action)
            {
                case "present_evidence":
                    string evidenceId = data as string;
                    if (!string.IsNullOrEmpty(evidenceId))
                    {
                        SetFlag($"presented_{evidenceId}", true);
                        Console.WriteLine($"[CommanderVonStateMachine] Evidence presented: {evidenceId} at {StressPercentage:F1}% stress");
                    }
                    break;

                case "accuse":
                    SetFlag("accused", true);
                    Console.WriteLine("[CommanderVonStateMachine] Commander Von accused");
                    break;
            }
        }

        /// <summary>
        /// Get evidence presentation dialogue based on evidence ID and stress level
        /// </summary>
        public CharacterDialogueSequence GetEvidenceReaction(string evidenceId)
        {
            var dialogue = GetEvidenceDialogue(evidenceId);
            if (dialogue != null)
            {
                Console.WriteLine($"[CommanderVonStateMachine] Returning evidence dialogue for {evidenceId}: {dialogue.sequence_name}");
            }
            else
            {
                Console.WriteLine($"[CommanderVonStateMachine] No evidence dialogue found for {evidenceId}");
            }
            return dialogue;
        }

    }
}
