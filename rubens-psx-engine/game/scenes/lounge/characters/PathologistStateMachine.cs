using System;
using anakinsoft.system;

namespace anakinsoft.game.scenes.lounge.characters
{
    /// <summary>
    /// State machine for Dr. Harmon Kerrigan's dialogue and behavior
    /// Handles evidence presentation and investigation logic
    /// </summary>
    public class PathologistStateMachine : CharacterStateMachine
    {
        public PathologistStateMachine(CharacterConfig characterConfig)
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
                    // First interaction - present evidence about Breturium shards
                    return GetDialogueSequence("PathologistEvidence");

                case "evidence_presented":
                    // Evidence has been discussed, waiting for player investigation
                    return null;

                case "investigation_complete":
                    // Player has completed investigation
                    return null;

                default:
                    Console.WriteLine($"[PathologistStateMachine] Unknown state: {currentState}");
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
                case "PathologistEvidence":
                    // Evidence presented - transition to investigation state
                    SetFlag("evidence_presented", true);
                    //TransitionTo("evidence_presented");
                    Console.WriteLine("[PathologistStateMachine] Evidence presented, investigation can begin");
                    break;

                default:
                    Console.WriteLine($"[PathologistStateMachine] Unknown sequence completed: {sequenceName}");
                    break;
            }
        }

        /// <summary>
        /// Handle player actions/responses
        /// </summary>
        public override void OnPlayerAction(string action, object data = null)
        {
            Console.WriteLine($"[PathologistStateMachine] Player action: {action}");

            switch (action)
            {
                case "examine_evidence":
                    // Player examines evidence on table
                    if (currentState == "evidence_presented")
                    {
                        SetFlag("evidence_examined", true);
                        Console.WriteLine("[PathologistStateMachine] Player examined evidence");
                    }
                    break;

                case "question_suspect":
                    // Player questioned a suspect
                    string suspectName = data as string;
                    if (!string.IsNullOrEmpty(suspectName))
                    {
                        SetFlag($"questioned_{suspectName}", true);
                        Console.WriteLine($"[PathologistStateMachine] Player questioned: {suspectName}");
                    }
                    break;

                case "investigation_complete":
                    // Player has completed the investigation
                    if (currentState == "evidence_presented")
                    {
                        SetFlag("investigation_complete", true);
                        TransitionTo("investigation_complete");
                        Console.WriteLine("[PathologistStateMachine] Investigation complete");
                    }
                    break;

                default:
                    Console.WriteLine($"[PathologistStateMachine] Unhandled action: {action}");
                    break;
            }
        }

        /// <summary>
        /// Check if evidence has been presented
        /// </summary>
        public bool HasPresentedEvidence()
        {
            return GetFlag("evidence_presented");
        }

        /// <summary>
        /// Check if player has examined the evidence
        /// </summary>
        public bool HasExaminedEvidence()
        {
            return GetFlag("evidence_examined");
        }
    }
}
