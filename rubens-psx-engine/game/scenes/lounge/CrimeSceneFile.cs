using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using anakinsoft.entities;
using System;
using System.Collections.Generic;

namespace rubens_psx_engine
{
    /// <summary>
    /// Crime scene file that allows reviewing interview transcripts
    /// </summary>
    public class CrimeSceneFile : InteractableObject
    {
        public string Name { get; private set; }
        public List<SuspectTranscript> Transcripts { get; private set; }

        public event Action<CrimeSceneFile> OnFileOpened;

        public CrimeSceneFile(
            string name,
            Vector3 position)
        {
            Name = name;
            this.position = position;
            Transcripts = new List<SuspectTranscript>();
            interactionDistance = 100f;
            interactionPrompt = "[E] Review Crime Scene File";
        }

        /// <summary>
        /// Add or update a transcript for a suspect
        /// </summary>
        public void AddTranscript(string suspectName, string content, bool wasQuestioned)
        {
            var existing = Transcripts.Find(t => t.SuspectName == suspectName);
            if (existing != null)
            {
                existing.Content = content;
                existing.WasQuestioned = wasQuestioned;
                existing.LastUpdated = DateTime.Now;
            }
            else
            {
                Transcripts.Add(new SuspectTranscript
                {
                    SuspectName = suspectName,
                    Content = content,
                    WasQuestioned = wasQuestioned,
                    LastUpdated = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Get transcript for a suspect
        /// </summary>
        public SuspectTranscript GetTranscript(string suspectName)
        {
            return Transcripts.Find(t => t.SuspectName == suspectName);
        }

        /// <summary>
        /// Handle interaction - open the file
        /// </summary>
        protected override void OnInteractAction()
        {
            Console.WriteLine($"Opening {Name}");
            OnFileOpened?.Invoke(this);
        }

        /// <summary>
        /// Override InteractionPrompt based on file state
        /// </summary>
        public override string InteractionPrompt
        {
            get
            {
                int questionedCount = Transcripts.FindAll(t => t.WasQuestioned).Count;
                int totalCount = Transcripts.Count;

                if (questionedCount == 0)
                    return "[E] Review Crime Scene File (No interviews yet)";

                return $"[E] Review Crime Scene File ({questionedCount}/{totalCount} interviewed)";
            }
        }
    }

    /// <summary>
    /// Represents a transcript for a suspect
    /// </summary>
    public class SuspectTranscript
    {
        public string SuspectName { get; set; }
        public string Content { get; set; }
        public bool WasQuestioned { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
