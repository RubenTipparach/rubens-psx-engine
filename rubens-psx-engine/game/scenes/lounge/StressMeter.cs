using System;

namespace anakinsoft.game.scenes.lounge
{
    /// <summary>
    /// Tracks stress level for a character during interrogation
    /// Stress ranges from 0-100%
    /// At 100%, character will self-dismiss
    /// </summary>
    public class StressMeter
    {
        private float currentStress = 0f;
        private const float MaxStress = 100f;

        public float CurrentStress => currentStress;
        public float StressPercentage => (currentStress / MaxStress) * 100f;
        public bool IsMaxStress => currentStress >= MaxStress;

        // Events
        public event Action<float> OnStressChanged; // Fires with new stress percentage
        public event Action OnMaxStressReached; // Fires when stress hits 100%

        public StressMeter()
        {
            currentStress = 0f;
        }

        /// <summary>
        /// Increase stress by a given amount
        /// </summary>
        public void IncreaseStress(float amount)
        {
            if (amount <= 0) return;

            float previousStress = currentStress;
            currentStress = Math.Min(currentStress + amount, MaxStress);

            Console.WriteLine($"[StressMeter] Stress increased by {amount:F1} (was {previousStress:F1}%, now {StressPercentage:F1}%)");

            OnStressChanged?.Invoke(StressPercentage);

            // Check if max stress reached
            if (!IsMaxStress && currentStress >= MaxStress)
            {
                Console.WriteLine($"[StressMeter] MAX STRESS REACHED - character will self-dismiss");
                OnMaxStressReached?.Invoke();
            }
        }

        /// <summary>
        /// Reset stress to 0
        /// </summary>
        public void Reset()
        {
            currentStress = 0f;
            Console.WriteLine($"[StressMeter] Reset to 0%");
            OnStressChanged?.Invoke(0f);
        }
    }
}
