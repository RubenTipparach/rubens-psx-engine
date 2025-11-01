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
            // Check for special high-stress medical training confrontation
            if (GetFlag("confronted_with_medical_training_high_stress"))
            {
                var stressed = GetDialogueSequence("CommanderVonMedicalTrainingStressed");
                if (stressed != null)
                {
                    Console.WriteLine("[CommanderVonStateMachine] Using high-stress medical training dialogue");
                    return stressed;
                }
            }

            switch (currentState)
            {
                case "initial":
                    return GetDialogueSequence("CommanderVonInterrogation");

                case "interrogated":
                    var followUp = GetDialogueSequence("CommanderVonFollowUp");
                    return followUp ?? CreateDefaultFollowUp();

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
                        Console.WriteLine($"[CommanderVonStateMachine] Evidence presented: {evidenceId}");

                        // Medical Training evidence is especially damning
                        if (evidenceId == "medical_training" && IsHighStress)
                        {
                            SetFlag("confronted_with_medical_training_high_stress", true);
                            Console.WriteLine($"[CommanderVonStateMachine] CRITICAL: Medical training presented at {StressPercentage:F1}% stress!");
                        }
                    }
                    break;

                case "accuse":
                    SetFlag("accused", true);
                    Console.WriteLine("[CommanderVonStateMachine] Commander Von accused");
                    break;
            }
        }

        private CharacterDialogueSequence CreateDefaultFollowUp()
        {
            return new CharacterDialogueSequence
            {
                sequence_name = "CommanderVonDefault",
                lines = new System.Collections.Generic.List<DialogueLine>
                {
                    new DialogueLine
                    {
                        speaker = config?.name ?? "Commander Von",
                        text = "I've told you everything about that night, Detective. My duty was to protect him, and I failed."
                    }
                },
                on_complete = ""
            };
        }
    }
}
