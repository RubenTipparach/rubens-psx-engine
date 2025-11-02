using System;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// State machine for Maven Kilroth
    /// Telirian smuggler - smooth, corporate, admits to breturium smuggling at high stress
    /// Sold breturium to Lucky Chen; red herring suspect
    /// </summary>
    public class MavenKilrothStateMachine : CharacterStateMachine
    {
        public MavenKilrothStateMachine(CharacterConfig characterConfig)
            : base(characterConfig)
        {
        }

        public override CharacterDialogueSequence GetCurrentDialogue()
        {
            switch (currentState)
            {
                case "initial":
                    var initialDialogue = GetDialogueSequence("MavenKilrothInterrogation");
                    return initialDialogue ?? GetDialogueSequence("MavenKilrothDefault");

                case "interrogated":
                    var followUp = GetDialogueSequence("MavenKilrothFollowUp");
                    return followUp ?? GetDialogueSequence("MavenKilrothDefault");

                default:
                    Console.WriteLine($"[MavenKilrothStateMachine] Unknown state: {currentState}");
                    return null;
            }
        }

        protected override void OnDialogueCompleteInternal(string sequenceName)
        {
            if (sequenceName == "MavenKilrothInterrogation")
            {
                TransitionTo("interrogated");
                SetFlag("has_been_interrogated", true);
                Console.WriteLine("[MavenKilrothStateMachine] Initial interrogation complete");
            }

            // Check for critical reveals
            if (sequenceName == "MavenKilrothDoubtHighStress")
            {
                SetFlag("admitted_smuggling", true);
                Console.WriteLine("[MavenKilrothStateMachine] Admitted to smuggling operation");
            }

            if (sequenceName == "MavenKilrothBreturiumHighStress")
            {
                SetFlag("admitted_breturium_smuggling", true);
                SetFlag("revealed_sold_to_lucky_chen", true);
                SetFlag("revealed_ambassador_knew", true);
                Console.WriteLine("[MavenKilrothStateMachine] CRITICAL: Admitted selling breturium to Lucky Chen!");
            }

            if (sequenceName == "MavenKilrothSecurityLogs")
            {
                SetFlag("revealed_solis_surveillance", true);
                Console.WriteLine("[MavenKilrothStateMachine] Revealed Chief Solis was planting surveillance devices");
            }
        }

        public override void OnPlayerAction(string action, object data = null)
        {
            Console.WriteLine($"[MavenKilrothStateMachine] Player action: {action}");

            switch (action)
            {
                case "present_evidence":
                    string evidenceId = data as string;
                    if (!string.IsNullOrEmpty(evidenceId))
                    {
                        SetFlag($"presented_{evidenceId}", true);
                        Console.WriteLine($"[MavenKilrothStateMachine] Evidence presented: {evidenceId} at {StressPercentage:F1}% stress");
                    }
                    break;

                case "doubt":
                    Console.WriteLine("[MavenKilrothStateMachine] Doubted - smooth operator cracks at high stress");
                    break;

                case "accuse":
                    SetFlag("accused", true);
                    Console.WriteLine("[MavenKilrothStateMachine] Maven Kilroth accused (he's innocent - red herring!)");
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
                Console.WriteLine($"[MavenKilrothStateMachine] Returning evidence dialogue for {evidenceId}: {dialogue.sequence_name}");
            }
            else
            {
                Console.WriteLine($"[MavenKilrothStateMachine] No evidence dialogue found for {evidenceId}");
            }
            return dialogue;
        }

        /// <summary>
        /// Get doubt dialogue based on current stress level
        /// Maven Kilroth cracks at 30% stress and admits to smuggling
        /// </summary>
        public CharacterDialogueSequence GetDoubtReaction()
        {
            // At high stress, admits to smuggling operation
            if (StressPercentage >= 30f)
            {
                var highStress = GetDialogueSequence("MavenKilrothDoubtHighStress");
                if (highStress != null)
                {
                    Console.WriteLine($"[MavenKilrothStateMachine] Using high-stress doubt dialogue at {StressPercentage:F1}%");
                    return highStress;
                }
            }

            // At low stress, maintains smooth composure
            var lowStress = GetDialogueSequence("MavenKilrothDoubtLowStress");
            if (lowStress != null)
            {
                Console.WriteLine($"[MavenKilrothStateMachine] Using low-stress doubt dialogue at {StressPercentage:F1}%");
                return lowStress;
            }

            return null;
        }

    }
}
