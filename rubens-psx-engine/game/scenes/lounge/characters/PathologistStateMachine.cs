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
                    // First interaction - ask player to get autopsy report
                    return GetDialogueSequence("PathologistReport");

                case "waiting_for_report":
                    // Waiting for player to bring the autopsy report
                    return GetDialogueSequence("WaitingForReport");

                case "report_received":
                    // Report received, present evidence and enable crime scene file
                    return GetDialogueSequence("PathologistEvidence");

                case "done":
                    // Investigation phase - pathologist has nothing more to say
                    return GetDialogueSequence("PathologistDone");

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
                case "PathologistReport":
                    // Asked player to get autopsy report - transition to waiting
                    TransitionTo("waiting_for_report");
                    SetFlag("asked_for_report", true);
                    Console.WriteLine("[PathologistStateMachine] Asked player to get autopsy report");
                    break;

                case "PathologistEvidence":
                    // Evidence presented - transition to done state
                    TransitionTo("done");
                    SetFlag("evidence_presented", true);
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

        /// <summary>
        /// Called when player delivers the autopsy report
        /// </summary>
        public void OnAutopsyReportDelivered()
        {
            if (currentState == "waiting_for_report")
            {
                TransitionTo("report_received");
                SetFlag("has_autopsy_report", true);
                Console.WriteLine("[PathologistStateMachine] Autopsy report delivered");
            }
        }
    }
}
