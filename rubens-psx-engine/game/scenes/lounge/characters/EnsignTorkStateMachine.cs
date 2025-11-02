using System;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// State machine for Ensign Tork
    /// Junior engineer who is eager and inexperienced
    /// </summary>
    public class EnsignTorkStateMachine : CharacterStateMachine
    {
        public EnsignTorkStateMachine(CharacterConfig characterConfig)
            : base(characterConfig)
        {
        }

        public override CharacterDialogueSequence GetCurrentDialogue()
        {
            switch (currentState)
            {
                case "initial":
                    var initialDialogue = GetDialogueSequence("EnsignTorkInterrogation");
                    return initialDialogue ?? GetDialogueSequence("EnsignTorkDefault");

                case "interrogated":
                    var followUp = GetDialogueSequence("EnsignTorkFollowUp");
                    return followUp ?? GetDialogueSequence("EnsignTorkDefault");

                default:
                    Console.WriteLine($"[EnsignTorkStateMachine] Unknown state: {currentState}");
                    return null;
            }
        }

        protected override void OnDialogueCompleteInternal(string sequenceName)
        {

            if (sequenceName == "EnsignTorkInterrogation")
            {
                TransitionTo("interrogated");
                SetFlag("has_been_interrogated", true);
                Console.WriteLine("[EnsignTorkStateMachine] Initial interrogation complete");
            }
        }

        public override void OnPlayerAction(string action, object data = null)
        {
            Console.WriteLine($"[EnsignTorkStateMachine] Player action: {action}");

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
                    Console.WriteLine("[EnsignTorkStateMachine] Ensign Tork accused");
                    break;
            }
        }

    }
}
