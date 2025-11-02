using System;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// State machine for Chief Petty Officer Raina Solis
    /// Head of Ship Security - by-the-book, stern, defensive about her unsanctioned investigation
    /// Red herring who tampered with crime scene
    /// </summary>
    public class ChiefSolisStateMachine : CharacterStateMachine
    {
        public ChiefSolisStateMachine(CharacterConfig characterConfig)
            : base(characterConfig)
        {
        }

        public override CharacterDialogueSequence GetCurrentDialogue()
        {
            switch (currentState)
            {
                case "initial":
                    return GetDialogueSequence("ChiefSolisInterrogation");

                case "interrogated":
                    var followUp = GetDialogueSequence("ChiefSolisFollowUp");
                    return followUp ?? GetDialogueSequence("ChiefSolisDefault");

                default:
                    Console.WriteLine($"[ChiefSolisStateMachine] Unknown state: {currentState}");
                    return null;
            }
        }

        protected override void OnDialogueCompleteInternal(string sequenceName)
        {
            if (sequenceName == "ChiefSolisInterrogation")
            {
                TransitionTo("interrogated");
                SetFlag("has_been_interrogated", true);
                Console.WriteLine("[ChiefSolisStateMachine] Initial interrogation complete");
            }

            // Check for critical reveals
            if (sequenceName == "ChiefSolisAccessCodesHighStress")
            {
                SetFlag("admitted_using_override", true);
                SetFlag("admitted_finding_body_early", true);
                SetFlag("admitted_tampering", true);
                Console.WriteLine("[ChiefSolisStateMachine] CRITICAL: Admitted to using override and finding body!");
            }

            if (sequenceName == "ChiefSolisSecurityLogHighStress")
            {
                SetFlag("admitted_editing_logs", true);
                Console.WriteLine("[ChiefSolisStateMachine] Admitted to editing security logs!");
            }

            if (sequenceName == "ChiefSolisBreturiumHighStress")
            {
                SetFlag("revealed_smuggling_network", true);
                Console.WriteLine("[ChiefSolisStateMachine] Revealed smuggling network details!");
            }
        }

        public override void OnPlayerAction(string action, object data = null)
        {
            Console.WriteLine($"[ChiefSolisStateMachine] Player action: {action}");

            switch (action)
            {
                case "present_evidence":
                    string evidenceId = data as string;
                    if (!string.IsNullOrEmpty(evidenceId))
                    {
                        SetFlag($"presented_{evidenceId}", true);
                        Console.WriteLine($"[ChiefSolisStateMachine] Evidence presented: {evidenceId} at {StressPercentage:F1}% stress");
                    }
                    break;

                case "doubt":
                    Console.WriteLine("[ChiefSolisStateMachine] Doubted - defensive about protocol");
                    break;

                case "accuse":
                    SetFlag("accused", true);
                    Console.WriteLine("[ChiefSolisStateMachine] Chief Solis accused (she's innocent - red herring!)");
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
                Console.WriteLine($"[ChiefSolisStateMachine] Returning evidence dialogue for {evidenceId}: {dialogue.sequence_name}");
            }
            else
            {
                Console.WriteLine($"[ChiefSolisStateMachine] No evidence dialogue found for {evidenceId}");
            }
            return dialogue;
        }

        /// <summary>
        /// Get doubt dialogue based on current stress level
        /// Chief Solis has different reactions at low vs high stress
        /// </summary>
        public CharacterDialogueSequence GetDoubtReaction()
        {
            // At high stress, she reveals the unsanctioned investigation
            if (StressPercentage >= 35f)
            {
                var highStress = GetDialogueSequence("ChiefSolisDoubtHighStress");
                if (highStress != null)
                {
                    Console.WriteLine($"[ChiefSolisStateMachine] Using high-stress doubt dialogue at {StressPercentage:F1}%");
                    return highStress;
                }
            }

            // At low stress, partial admission
            var lowStress = GetDialogueSequence("ChiefSolisDoubtLowStress");
            if (lowStress != null)
            {
                Console.WriteLine($"[ChiefSolisStateMachine] Using low-stress doubt dialogue at {StressPercentage:F1}%");
                return lowStress;
            }

            return null;
        }

    }
}
