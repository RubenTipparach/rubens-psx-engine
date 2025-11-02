using System;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// State machine for T'Vora
    /// Kulluran intelligence operative monitoring breturium smuggling network
    /// CRITICAL: Knew about murder in advance but let it happen ("needs of many")
    /// </summary>
    public class TehvoraStateMachine : CharacterStateMachine
    {
        public TehvoraStateMachine(CharacterConfig characterConfig)
            : base(characterConfig)
        {
        }

        public override CharacterDialogueSequence GetCurrentDialogue()
        {
            switch (currentState)
            {
                case "initial":
                    var initialDialogue = GetDialogueSequence("TVoraInterrogation");
                    if (initialDialogue == null)
                        initialDialogue = GetDialogueSequence("TehvoraInterrogation");
                    return initialDialogue ?? GetDialogueSequence("TehvoraDefault");

                case "interrogated":
                    var followUp = GetDialogueSequence("TVoraFollowUp");
                    if (followUp == null)
                        followUp = GetDialogueSequence("TehvoraFollowUp");
                    return followUp ?? GetDialogueSequence("TehvoraDefault");

                default:
                    Console.WriteLine($"[TehvoraStateMachine] Unknown state: {currentState}");
                    return null;
            }
        }

        protected override void OnDialogueCompleteInternal(string sequenceName)
        {
            // Handle both old and new sequence names
            if (sequenceName == "TehvoraInterrogation" || sequenceName == "TVoraInterrogation")
            {
                TransitionTo("interrogated");
                SetFlag("has_been_interrogated", true);
                Console.WriteLine("[TehvoraStateMachine] Initial interrogation complete");
            }

            // Check for critical intelligence reveals
            if (sequenceName == "TVoraDoubtHighStress")
            {
                SetFlag("revealed_kulluran_intelligence", true);
                SetFlag("revealed_tracking_smuggling", true);
                SetFlag("revealed_failed_to_prevent_death", true);
                Console.WriteLine("[TehvoraStateMachine] CRITICAL: Revealed Kulluran Intelligence operative!");
            }

            if (sequenceName == "TVoraBreturiumHighStress")
            {
                SetFlag("revealed_tracking_breturium", true);
                SetFlag("revealed_warned_ambassador", true);
                SetFlag("knew_about_murder_risk", true);
                Console.WriteLine("[TehvoraStateMachine] CRITICAL: Admitted she knew Ambassador was a target!");
            }

            if (sequenceName == "TVoraSecurityLogs")
            {
                SetFlag("revealed_solis_interference", true);
                SetFlag("knows_solis_found_body", true);
                Console.WriteLine("[TehvoraStateMachine] Revealed Chief Solis's unauthorized investigation");
            }

            if (sequenceName == "TVoraCommanderVonConnection")
            {
                SetFlag("hints_medical_operation_gone_wrong", true);
                Console.WriteLine("[TehvoraStateMachine] Hinted at medical operation gone wrong");
            }
        }

        public override void OnPlayerAction(string action, object data = null)
        {
            Console.WriteLine($"[TehvoraStateMachine] Player action: {action}");

            switch (action)
            {
                case "present_evidence":
                    string evidenceId = data as string;
                    if (!string.IsNullOrEmpty(evidenceId))
                    {
                        SetFlag($"presented_{evidenceId}", true);
                        Console.WriteLine($"[TehvoraStateMachine] Evidence presented: {evidenceId} at {StressPercentage:F1}% stress");
                    }
                    break;

                case "doubt":
                    Console.WriteLine("[TehvoraStateMachine] Doubted - Kulluran logic under pressure");
                    break;

                case "accuse":
                    SetFlag("accused", true);
                    Console.WriteLine("[TehvoraStateMachine] T'Vora accused (she's innocent - Kulluran Intelligence operative)");
                    break;
            }
        }

        /// <summary>
        /// Get evidence presentation dialogue based on evidence ID and stress level
        /// T'Vora has deep knowledge of the smuggling network
        /// </summary>
        public CharacterDialogueSequence GetEvidenceReaction(string evidenceId)
        {
            var dialogue = GetEvidenceDialogue(evidenceId);
            if (dialogue != null)
            {
                Console.WriteLine($"[TehvoraStateMachine] Returning evidence dialogue for {evidenceId}: {dialogue.sequence_name}");
            }
            else
            {
                Console.WriteLine($"[TehvoraStateMachine] No evidence dialogue found for {evidenceId}");
            }
            return dialogue;
        }

        /// <summary>
        /// Get doubt dialogue based on current stress level
        /// T'Vora is VERY difficult to crack (40% threshold) due to Kulluran emotional control
        /// </summary>
        public CharacterDialogueSequence GetDoubtReaction()
        {
            // At high stress, reveals Kulluran Intelligence mission
            if (StressPercentage >= 40f)
            {
                var highStress = GetDialogueSequence("TVoraDoubtHighStress");
                if (highStress != null)
                {
                    Console.WriteLine($"[TehvoraStateMachine] Using high-stress doubt dialogue at {StressPercentage:F1}%");
                    return highStress;
                }
            }

            // At low stress, maintains perfect Kulluran composure
            var lowStress = GetDialogueSequence("TVoraDoubtLowStress");
            if (lowStress != null)
            {
                Console.WriteLine($"[TehvoraStateMachine] Using low-stress doubt dialogue at {StressPercentage:F1}%");
                return lowStress;
            }

            return null;
        }

    }
}
