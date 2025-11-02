using System;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// State machine for Dr. Lyssa Thorne
    /// Xenobiologist and research liaison - nervous, breaks under pressure
    /// Accomplice who drugged the Ambassador
    /// </summary>
    public class DrThorneStateMachine : CharacterStateMachine
    {
        public DrThorneStateMachine(CharacterConfig characterConfig)
            : base(characterConfig)
        {
        }

        public override CharacterDialogueSequence GetCurrentDialogue()
        {
            switch (currentState)
            {
                case "initial":
                    return GetDialogueSequence("DrThorneInterrogation");

                case "interrogated":
                    var followUp = GetDialogueSequence("DrThorneFollowUp");
                    return followUp ?? CreateDefaultFollowUp();

                default:
                    Console.WriteLine($"[DrThorneStateMachine] Unknown state: {currentState}");
                    return null;
            }
        }

        protected override void OnDialogueCompleteInternal(string sequenceName)
        {
            if (sequenceName == "DrThorneInterrogation")
            {
                TransitionTo("interrogated");
                SetFlag("has_been_interrogated", true);
                Console.WriteLine("[DrThorneStateMachine] Initial interrogation complete");
            }

            // Check for confession about drugging the Ambassador
            if (sequenceName == "DrThorneDatapadHighStress")
            {
                SetFlag("confessed_to_drugging", true);
                SetFlag("revealed_accomplice", true);
                Console.WriteLine("[DrThorneStateMachine] CONFESSION: Admitted to drugging the Ambassador!");
            }
        }

        public override void OnPlayerAction(string action, object data = null)
        {
            Console.WriteLine($"[DrThorneStateMachine] Player action: {action}");

            switch (action)
            {
                case "present_evidence":
                    string evidenceId = data as string;
                    if (!string.IsNullOrEmpty(evidenceId))
                    {
                        SetFlag($"presented_{evidenceId}", true);
                        Console.WriteLine($"[DrThorneStateMachine] Evidence presented: {evidenceId} at {StressPercentage:F1}% stress");
                    }
                    break;

                case "doubt":
                    Console.WriteLine("[DrThorneStateMachine] Doubted - nervous personality");
                    break;

                case "accuse":
                    SetFlag("accused", true);
                    Console.WriteLine("[DrThorneStateMachine] Dr. Thorne accused");
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
                Console.WriteLine($"[DrThorneStateMachine] Returning evidence dialogue for {evidenceId}: {dialogue.sequence_name}");
            }
            else
            {
                Console.WriteLine($"[DrThorneStateMachine] No evidence dialogue found for {evidenceId}");
            }
            return dialogue;
        }

        /// <summary>
        /// Get doubt dialogue based on current stress level
        /// Dr. Thorne has different reactions at low vs high stress
        /// </summary>
        public CharacterDialogueSequence GetDoubtReaction()
        {
            // At high stress, she admits more
            if (StressPercentage >= 25f)
            {
                var highStress = GetDialogueSequence("DrThorneDoubtHighStress");
                if (highStress != null)
                {
                    Console.WriteLine($"[DrThorneStateMachine] Using high-stress doubt dialogue at {StressPercentage:F1}%");
                    return highStress;
                }
            }

            // At low stress, partial admission
            var lowStress = GetDialogueSequence("DrThorneDoubtLowStress");
            if (lowStress != null)
            {
                Console.WriteLine($"[DrThorneStateMachine] Using low-stress doubt dialogue at {StressPercentage:F1}%");
                return lowStress;
            }

            return null;
        }

        private CharacterDialogueSequence CreateDefaultFollowUp()
        {
            return new CharacterDialogueSequence
            {
                sequence_name = "DrThorneDefault",
                lines = new System.Collections.Generic.List<DialogueLine>
                {
                    new DialogueLine
                    {
                        speaker = config?.name ?? "Dr. Thorne",
                        text = "I've told you everything I know, Detective. The Ambassador was a good man. I... I miss our conversations."
                    }
                },
                on_complete = ""
            };
        }
    }
}
