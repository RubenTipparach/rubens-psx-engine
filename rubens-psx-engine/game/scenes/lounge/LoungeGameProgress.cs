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
        public bool CanSelectSuspects { get; set; } = false;
        public bool CanInterrogate { get; set; } = false;

        // Interrogation tracking
        public bool HasInterrogatedCommanderVon { get; set; } = false;
        public bool HasInterrogatedDrThorne { get; set; } = false;
        public bool HasInterrogatedLtWebb { get; set; } = false;
        public bool HasInterrogatedEnsignTork { get; set; } = false;
        public bool HasInterrogatedMavenKilroth { get; set; } = false;
        public bool HasInterrogatedChiefSolis { get; set; } = false;
        public bool HasInterrogatedTvora { get; set; } = false;
        public bool HasInterrogatedLuckyChen { get; set; } = false;

        // Game state
        public int InterrogationsCompleted =>
            (HasInterrogatedCommanderVon ? 1 : 0) +
            (HasInterrogatedDrThorne ? 1 : 0) +
            (HasInterrogatedLtWebb ? 1 : 0) +
            (HasInterrogatedEnsignTork ? 1 : 0) +
            (HasInterrogatedMavenKilroth ? 1 : 0) +
            (HasInterrogatedChiefSolis ? 1 : 0) +
            (HasInterrogatedTvora ? 1 : 0) +
            (HasInterrogatedLuckyChen ? 1 : 0);

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
            HasInterrogatedLtWebb = false;
            HasInterrogatedEnsignTork = false;
            HasInterrogatedMavenKilroth = false;
            HasInterrogatedChiefSolis = false;
            HasInterrogatedTvora = false;
            HasInterrogatedLuckyChen = false;
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
