using System;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// State machine for Lieutenant Marcus Webb
    /// Tactical Officer - ambitious, charming, overconfident
    /// Red herring who tried to frame Chief Solis, Fregoilli peace sympathizer
    /// </summary>
    public class LtWebbStateMachine : CharacterStateMachine
    {
        public LtWebbStateMachine(CharacterConfig characterConfig)
            : base(characterConfig)
        {
        }

        public override CharacterDialogueSequence GetCurrentDialogue()
        {
            switch (currentState)
            {
                case "initial":
                    return GetDialogueSequence("LtWebbInterrogation");

                case "interrogated":
                    var followUp = GetDialogueSequence("LtWebbFollowUp");
                    return followUp ?? CreateDefaultFollowUp();

                default:
                    Console.WriteLine($"[LtWebbStateMachine] Unknown state: {currentState}");
                    return null;
            }
        }

        protected override void OnDialogueCompleteInternal(string sequenceName)
        {
            if (sequenceName == "LtWebbInterrogation")
            {
                TransitionTo("interrogated");
                SetFlag("has_been_interrogated", true);
                Console.WriteLine("[LtWebbStateMachine] Initial interrogation complete");
            }

            // Check for critical reveals
            if (sequenceName == "LtWebbAccessCodesHighStress")
            {
                SetFlag("admitted_framing_solis", true);
                SetFlag("admitted_log_tampering", true);
                Console.WriteLine("[LtWebbStateMachine] CRITICAL: Admitted to framing Chief Solis!");
            }

            if (sequenceName == "LtWebbSecurityLogHighStress")
            {
                SetFlag("admitted_log_tampering", true);
                Console.WriteLine("[LtWebbStateMachine] Admitted to tampering with security logs!");
            }

            if (sequenceName == "LtWebbBreturiumHighStress")
            {
                SetFlag("revealed_fregoilli_contacts", true);
                SetFlag("revealed_smuggling_network", true);
                Console.WriteLine("[LtWebbStateMachine] Revealed Fregoilli peace network and smuggling!");
            }

            if (sequenceName == "LtWebbDoubtHighStress")
            {
                SetFlag("revealed_motive", true);
                Console.WriteLine("[LtWebbStateMachine] Revealed he wanted the Ambassador dead!");
            }
        }

        public override void OnPlayerAction(string action, object data = null)
        {
            Console.WriteLine($"[LtWebbStateMachine] Player action: {action}");

            switch (action)
            {
                case "present_evidence":
                    string evidenceId = data as string;
                    if (!string.IsNullOrEmpty(evidenceId))
                    {
                        SetFlag($"presented_{evidenceId}", true);
                        Console.WriteLine($"[LtWebbStateMachine] Evidence presented: {evidenceId} at {StressPercentage:F1}% stress");
                    }
                    break;

                case "doubt":
                    Console.WriteLine("[LtWebbStateMachine] Doubted - confident but will reveal political views under pressure");
                    break;

                case "accuse":
                    SetFlag("accused", true);
                    Console.WriteLine("[LtWebbStateMachine] Lt. Webb accused (he's innocent - red herring!)");
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
                Console.WriteLine($"[LtWebbStateMachine] Returning evidence dialogue for {evidenceId}: {dialogue.sequence_name}");
            }
            else
            {
                Console.WriteLine($"[LtWebbStateMachine] No evidence dialogue found for {evidenceId}");
            }
            return dialogue;
        }

        /// <summary>
        /// Get doubt dialogue based on current stress level
        /// Lt. Webb has different reactions at low vs high stress
        /// </summary>
        public CharacterDialogueSequence GetDoubtReaction()
        {
            // At high stress, he reveals his political motivations
            if (StressPercentage >= 40f)
            {
                var highStress = GetDialogueSequence("LtWebbDoubtHighStress");
                if (highStress != null)
                {
                    Console.WriteLine($"[LtWebbStateMachine] Using high-stress doubt dialogue at {StressPercentage:F1}%");
                    return highStress;
                }
            }

            // At low stress, confident deflection
            var lowStress = GetDialogueSequence("LtWebbDoubtLowStress");
            if (lowStress != null)
            {
                Console.WriteLine($"[LtWebbStateMachine] Using low-stress doubt dialogue at {StressPercentage:F1}%");
                return lowStress;
            }

            return null;
        }

        private CharacterDialogueSequence CreateDefaultFollowUp()
        {
            return new CharacterDialogueSequence
            {
                sequence_name = "LtWebbDefault",
                lines = new System.Collections.Generic.List<DialogueLine>
                {
                    new DialogueLine
                    {
                        speaker = config?.name ?? "Lieutenant Webb",
                        text = "I've already answered your questions, Detective. I was on the bridge. Check the logs."
                    }
                },
                on_complete = ""
            };
        }
    }
}
