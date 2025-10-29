using System;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// State machine for Chief Kala Solis
    /// Security Chief with strict protocols and no-nonsense attitude
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
                    return followUp ?? CreateDefaultFollowUp();

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
                    }
                    break;

                case "accuse":
                    SetFlag("accused", true);
                    Console.WriteLine("[ChiefSolisStateMachine] Chief Solis accused");
                    break;
            }
        }

        private CharacterDialogueSequence CreateDefaultFollowUp()
        {
            return new CharacterDialogueSequence
            {
                sequence_name = "ChiefSolisDefault",
                lines = new System.Collections.Generic.List<DialogueLine>
                {
                    new DialogueLine
                    {
                        speaker = config?.name ?? "Chief Solis",
                        text = "I've provided you with the security logs and my statement. If you need further clarification, submit a formal request."
                    }
                },
                on_complete = ""
            };
        }
    }
}
