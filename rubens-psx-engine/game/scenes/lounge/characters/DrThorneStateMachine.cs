using System;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// State machine for Dr. Lyssa Thorne
    /// Xenopathologist who is nervous and academic
    /// </summary>
    public class DrThorneStateMachine : CharacterStateMachine
    {
        public DrThorneStateMachine(CharacterConfig characterConfig)
            : base(characterConfig)
        {
        }

        public override CharacterDialogueSequence GetCurrentDialogue()
        {
            switch (currentState)
            {
                case "initial":
                    return GetDialogueSequence("DrThorneInterrogation");

                case "interrogated":
                    var followUp = GetDialogueSequence("DrThorneFollowUp");
                    return followUp ?? CreateDefaultFollowUp();

                default:
                    Console.WriteLine($"[DrThorneStateMachine] Unknown state: {currentState}");
                    return null;
            }
        }

        public override void OnDialogueComplete(string sequenceName)
        {
            MarkDialogueSeen(sequenceName);

            if (sequenceName == "DrThorneInterrogation")
            {
                TransitionTo("interrogated");
                SetFlag("has_been_interrogated", true);
                Console.WriteLine("[DrThorneStateMachine] Initial interrogation complete");
            }
        }

        public override void OnPlayerAction(string action, object data = null)
        {
            Console.WriteLine($"[DrThorneStateMachine] Player action: {action}");

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
                    Console.WriteLine("[DrThorneStateMachine] Dr. Thorne accused");
                    break;
            }
        }

        private CharacterDialogueSequence CreateDefaultFollowUp()
        {
            return new CharacterDialogueSequence
            {
                sequence_name = "DrThorneDefault",
                lines = new System.Collections.Generic.List<DialogueLine>
                {
                    new DialogueLine
                    {
                        speaker = config?.name ?? "Dr. Thorne",
                        text = "I-I already told you everything I know! The cultural exchange paper, the death rituals—do you need me to explain again?"
                    }
                },
                on_complete = ""
            };
        }
    }
}
