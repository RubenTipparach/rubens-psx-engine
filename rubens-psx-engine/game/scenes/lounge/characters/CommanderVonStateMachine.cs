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
                    return followUp ?? CreateDefaultFollowUp();

                default:
                    Console.WriteLine($"[CommanderVonStateMachine] Unknown state: {currentState}");
                    return null;
            }
        }

        public override void OnDialogueComplete(string sequenceName)
        {
            MarkDialogueSeen(sequenceName);

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
