using System;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// State machine for Lucky Chen
    /// Quartermaster with practical, no-nonsense approach
    /// </summary>
    public class LuckyChenStateMachine : CharacterStateMachine
    {
        public LuckyChenStateMachine(CharacterConfig characterConfig)
            : base(characterConfig)
        {
        }

        public override CharacterDialogueSequence GetCurrentDialogue()
        {
            switch (currentState)
            {
                case "initial":
                    var initialDialogue = GetDialogueSequence("LuckyChenInterrogation");
                    return initialDialogue ?? GetDialogueSequence("LuckyChenDefault");

                case "interrogated":
                    var followUp = GetDialogueSequence("LuckyChenFollowUp");
                    return followUp ?? GetDialogueSequence("LuckyChenDefault");

                default:
                    Console.WriteLine($"[LuckyChenStateMachine] Unknown state: {currentState}");
                    return null;
            }
        }

        protected override void OnDialogueCompleteInternal(string sequenceName)
        {

            if (sequenceName == "LuckyChenInterrogation")
            {
                TransitionTo("interrogated");
                SetFlag("has_been_interrogated", true);
                Console.WriteLine("[LuckyChenStateMachine] Initial interrogation complete");
            }
        }

        public override void OnPlayerAction(string action, object data = null)
        {
            Console.WriteLine($"[LuckyChenStateMachine] Player action: {action}");

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
                    Console.WriteLine("[LuckyChenStateMachine] Lucky Chen accused");
                    break;
            }
        }

    }
}
