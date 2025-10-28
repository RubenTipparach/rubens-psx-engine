using System;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// State machine for Bartender Zix's dialogue and behavior
    /// Handles intro dialogue and directing player to pathologist
    /// </summary>
    public class BartenderStateMachine : CharacterStateMachine
    {
        public BartenderStateMachine(CharacterConfig characterConfig)
            : base(characterConfig)
        {
        }

        /// <summary>
        /// Get the current dialogue based on state
        /// </summary>
        public override CharacterDialogueSequence GetCurrentDialogue()
        {
            switch (currentState)
            {
                case "initial":
                    // First interaction - show intro dialogue
                    return GetDialogueSequence("BartenderIntro");

                case "post_intro":
                    // After intro, direct player to pathologist
                    return GetDialogueSequence("BartenderPostIntro");

                case "idle":
                    // Player has talked to pathologist, bartender has nothing new to say
                    return null;

                default:
                    Console.WriteLine($"[BartenderStateMachine] Unknown state: {currentState}");
                    return null;
            }
        }

        /// <summary>
        /// Handle dialogue completion and state transitions
        /// </summary>
        public override void OnDialogueComplete(string sequenceName)
        {
            MarkDialogueSeen(sequenceName);

            switch (sequenceName)
            {
                case "BartenderIntro":
                    // Intro complete - spawn pathologist and transition
                    SetFlag("pathologist_spawned", true);
                    TransitionTo("post_intro");
                    Console.WriteLine("[BartenderStateMachine] Pathologist should be spawned");
                    break;

                case "BartenderPostIntro":
                    // Player has been directed to pathologist
                    // TransitionTo("post_intro");
                    break;

                default:
                    Console.WriteLine($"[BartenderStateMachine] Unknown sequence completed: {sequenceName}");
                    break;
            }
        }

        /// <summary>
        /// Handle player actions/responses
        /// </summary>
        public override void OnPlayerAction(string action, object data = null)
        {
            Console.WriteLine($"[BartenderStateMachine] Player action: {action}");

            switch (action)
            {
                case "pathologist_met":
                    // Player has talked to pathologist, bartender goes idle
                    if (currentState == "post_intro")
                    {
                        SetFlag("player_met_pathologist", true);
                        TransitionTo("idle");
                    }
                    break;

                default:
                    Console.WriteLine($"[BartenderStateMachine] Unhandled action: {action}");
                    break;
            }
        }

        /// <summary>
        /// Check if pathologist should be spawned
        /// </summary>
        public bool ShouldSpawnPathologist()
        {
            return GetFlag("pathologist_spawned");
        }
    }
}
