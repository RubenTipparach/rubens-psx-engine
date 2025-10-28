using System;

namespace rubens_psx_engine
{
    /// <summary>
    /// Tracks player progress through The Lounge murder mystery
    /// </summary>
    public class LoungeGameProgress
    {
        // Story flags
        public bool HasSeenIntro { get; set; } = false;
        public bool HasTalkedToBartender { get; set; } = false;
        public bool PathologistSpawned { get; set; } = false;
        public bool HasTalkedToPathologist { get; set; } = false;
        public bool CanInterrogate { get; set; } = false;

        // Interrogation tracking
        public bool HasInterrogatedCommanderVon { get; set; } = false;
        public bool HasInterrogatedDrThorne { get; set; } = false;

        // Game state
        public int InterrogationsCompleted =>
            (HasInterrogatedCommanderVon ? 1 : 0) +
            (HasInterrogatedDrThorne ? 1 : 0);

        public bool CanMakeAccusation => InterrogationsCompleted >= 2;

        /// <summary>
        /// Reset all progress (for new game)
        /// </summary>
        public void Reset()
        {
            HasSeenIntro = false;
            HasTalkedToBartender = false;
            PathologistSpawned = false;
            HasTalkedToPathologist = false;
            CanInterrogate = false;
            HasInterrogatedCommanderVon = false;
            HasInterrogatedDrThorne = false;
        }

        /// <summary>
        /// Debug log current progress
        /// </summary>
        public void LogProgress()
        {
            Console.WriteLine("=== LOUNGE GAME PROGRESS ===");
            Console.WriteLine($"Has Seen Intro: {HasSeenIntro}");
            Console.WriteLine($"Has Talked to Bartender: {HasTalkedToBartender}");
            Console.WriteLine($"Pathologist Spawned: {PathologistSpawned}");
            Console.WriteLine($"Has Talked to Pathologist: {HasTalkedToPathologist}");
            Console.WriteLine($"Can Interrogate: {CanInterrogate}");
            Console.WriteLine($"Interrogations Completed: {InterrogationsCompleted}/2");
            Console.WriteLine($"Can Make Accusation: {CanMakeAccusation}");
            Console.WriteLine("============================");
        }
    }
}
