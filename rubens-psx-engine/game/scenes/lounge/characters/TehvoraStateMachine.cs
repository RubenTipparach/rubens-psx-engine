using System;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// State machine for Tehvora
    /// Kullan diplomatic attache with formal demeanor
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
                    return GetDialogueSequence("TehvoraInterrogation");

                case "interrogated":
                    var followUp = GetDialogueSequence("TehvoraFollowUp");
                    return followUp ?? CreateDefaultFollowUp();

                default:
                    Console.WriteLine($"[TehvoraStateMachine] Unknown state: {currentState}");
                    return null;
            }
        }

        protected override void OnDialogueCompleteInternal(string sequenceName)
        {

            if (sequenceName == "TehvoraInterrogation")
            {
                TransitionTo("interrogated");
                SetFlag("has_been_interrogated", true);
                Console.WriteLine("[TehvoraStateMachine] Initial interrogation complete");
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
                    }
                    break;

                case "accuse":
                    SetFlag("accused", true);
                    Console.WriteLine("[TehvoraStateMachine] Tehvora accused");
                    break;
            }
        }

        private CharacterDialogueSequence CreateDefaultFollowUp()
        {
            return new CharacterDialogueSequence
            {
                sequence_name = "TehvoraDefault",
                lines = new System.Collections.Generic.List<DialogueLine>
                {
                    new DialogueLine
                    {
                        speaker = config?.name ?? "Tehvora",
                        text = "I have shared all relevant information with you, Detective. Further inquiries must be submitted through proper diplomatic channels."
                    }
                },
                on_complete = ""
            };
        }
    }
}
