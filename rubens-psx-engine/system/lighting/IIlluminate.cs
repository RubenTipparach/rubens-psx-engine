using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace rubens_psx_engine.system.lighting
{
    /// <summary>
    /// Interface for materials that can receive lighting
    /// </summary>
    public interface IIlluminate
    {
        /// <summary>
        /// Whether this material receives lighting
        /// </summary>
        bool ReceivesLighting { get; set; }

        /// <summary>
        /// Maximum number of point lights this material can handle
        /// </summary>
        int MaxPointLights { get; }

        /// <summary>
        /// Apply environment lighting to the material's effect
        /// </summary>
        void ApplyEnvironmentLight(EnvironmentLight environmentLight);

        /// <summary>
        /// Apply point lights to the material's effect
        /// </summary>
        void ApplyPointLights(IEnumerable<PointLight> pointLights, Vector3 worldPosition);

        /// <summary>
        /// Clear all lighting from the material
        /// </summary>
        void ClearLighting();

        /// <summary>
        /// Get the underlying effect for advanced lighting operations
        /// </summary>
        Effect GetEffect();
    }
}