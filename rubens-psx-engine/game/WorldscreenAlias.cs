using System;

namespace rubens_psx_engine
{
    /// <summary>
    /// Backward compatibility alias for Worldscreen -> ThirdPersonSandboxScreen
    /// </summary>
    [Obsolete("Use ThirdPersonSandboxScreen instead. Worldscreen is deprecated.")]
    public class Worldscreen : ThirdPersonSandboxScreen
    {
        public Worldscreen() : base()
        {
        }
    }
}