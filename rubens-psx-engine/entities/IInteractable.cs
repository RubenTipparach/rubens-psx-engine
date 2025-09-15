using Microsoft.Xna.Framework;
using System;

namespace anakinsoft.entities
{
    /// <summary>
    /// Interface for objects that can be interacted with via raycast targeting
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Position of the interactable object in world space
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// Maximum distance from which this object can be interacted with
        /// </summary>
        float InteractionDistance { get; }

        /// <summary>
        /// Text displayed when the player aims at this object
        /// </summary>
        string InteractionPrompt { get; }

        /// <summary>
        /// Additional descriptive text shown below the prompt
        /// </summary>
        string InteractionDescription { get; }

        /// <summary>
        /// Whether this object can currently be interacted with
        /// </summary>
        bool CanInteract { get; }

        /// <summary>
        /// Whether this object is currently being aimed at
        /// </summary>
        bool IsTargeted { get; set; }

        /// <summary>
        /// Called when the player attempts to interact with this object
        /// </summary>
        void OnInteract();

        /// <summary>
        /// Called when the player starts aiming at this object
        /// </summary>
        void OnTargetEnter();

        /// <summary>
        /// Called when the player stops aiming at this object
        /// </summary>
        void OnTargetExit();
    }

    /// <summary>
    /// Base implementation of IInteractable with common functionality
    /// </summary>
    public abstract class InteractableObject : IInteractable
    {
        protected Vector3 position;
        protected float interactionDistance = 100f;
        protected string interactionPrompt = "Press E to interact";
        protected string interactionDescription = "";
        protected bool canInteract = true;
        protected bool isTargeted = false;

        // Events for external handling
        public event Action OnInteractEvent;
        public event Action OnTargetEnterEvent;
        public event Action OnTargetExitEvent;

        public virtual Vector3 Position
        {
            get => position;
            set => position = value;
        }

        public virtual float InteractionDistance
        {
            get => interactionDistance;
            set => interactionDistance = value;
        }

        public virtual string InteractionPrompt
        {
            get => interactionPrompt;
            set => interactionPrompt = value;
        }

        public virtual string InteractionDescription
        {
            get => interactionDescription;
            set => interactionDescription = value;
        }

        public virtual bool CanInteract
        {
            get => canInteract;
            set => canInteract = value;
        }

        public virtual bool IsTargeted
        {
            get => isTargeted;
            set
            {
                if (isTargeted != value)
                {
                    isTargeted = value;
                    if (isTargeted)
                        OnTargetEnter();
                    else
                        OnTargetExit();
                }
            }
        }

        public virtual void OnInteract()
        {
            if (CanInteract)
            {
                OnInteractEvent?.Invoke();
                OnInteractAction();
            }
        }

        public virtual void OnTargetEnter()
        {
            OnTargetEnterEvent?.Invoke();
            OnTargetEnterAction();
        }

        public virtual void OnTargetExit()
        {
            OnTargetExitEvent?.Invoke();
            OnTargetExitAction();
        }

        // Override these for custom behavior
        protected abstract void OnInteractAction();
        protected virtual void OnTargetEnterAction() { }
        protected virtual void OnTargetExitAction() { }
    }
}