using Microsoft.Xna.Framework;
using anakinsoft.system;
using System;
using BepuPhysics;

namespace anakinsoft.entities
{
    /// <summary>
    /// Represents a character NPC that can be interacted with for dialogue
    /// </summary>
    public class InteractableCharacter : InteractableObject
    {
        public string CharacterName { get; set; }
        public Vector3 CameraInteractionPosition { get; set; }
        public Vector3 CameraInteractionLookAt { get; set; }
        public DialogueSequence DialogueSequence { get; set; }

        // Physics handle for raycast detection
        private StaticHandle? staticHandle;

        // Events for character-specific interactions
        public event Action<DialogueSequence> OnDialogueTriggered;

        public InteractableCharacter(string characterName, Vector3 position,
            Vector3 cameraPosition, Vector3 cameraLookAt)
        {
            CharacterName = characterName;
            this.position = position;
            CameraInteractionPosition = cameraPosition;
            CameraInteractionLookAt = cameraLookAt;

            // Set default interaction properties
            interactionDistance = 150f; // Can interact from a distance
            interactionPrompt = $"Press E to talk to {characterName}";
            interactionDescription = "";
        }

        /// <summary>
        /// Sets the dialogue sequence for this character
        /// </summary>
        public void SetDialogue(DialogueSequence dialogue)
        {
            DialogueSequence = dialogue;
        }

        /// <summary>
        /// Adds a dialogue line to this character's dialogue
        /// </summary>
        public void AddDialogueLine(string text, Action onComplete = null)
        {
            if (DialogueSequence == null)
            {
                DialogueSequence = new DialogueSequence($"{CharacterName}_Dialogue");
            }

            DialogueSequence.AddLine(CharacterName, text, onComplete);
        }

        protected override void OnInteractAction()
        {
            Console.WriteLine($"Interacting with {CharacterName}");

            if (DialogueSequence != null && DialogueSequence.Lines.Count > 0)
            {
                OnDialogueTriggered?.Invoke(DialogueSequence);
            }
            else
            {
                Console.WriteLine($"{CharacterName} has no dialogue configured");
            }
        }

        protected override void OnTargetEnterAction()
        {
            Console.WriteLine($"Looking at {CharacterName}");
        }

        protected override void OnTargetExitAction()
        {
            Console.WriteLine($"Stopped looking at {CharacterName}");
        }

        /// <summary>
        /// Sets the physics static handle for this character
        /// </summary>
        public void SetStaticHandle(StaticHandle handle)
        {
            staticHandle = handle;
        }

        /// <summary>
        /// Gets the physics static handle for this character
        /// </summary>
        public StaticHandle? GetStaticHandle()
        {
            return staticHandle;
        }
    }
}
