using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace rubens_psx_engine.system.lighting
{
    /// <summary>
    /// Manages all lights in the scene and applies them to materials
    /// </summary>
    public class LightProcessor
    {
        private List<PointLight> pointLights;
        private EnvironmentLight environmentLight;
        private int maxPointLightsPerMaterial;

        // Spatial optimization
        private bool useSpatialOptimization;
        private float spatialGridSize;

        // Statistics
        public int TotalPointLights => pointLights.Count;
        public int ActivePointLights => pointLights.Count(l => l.IsEnabled);

        public LightProcessor(int maxPointLightsPerMaterial = 8)
        {
            pointLights = new List<PointLight>();
            environmentLight = new EnvironmentLight();
            this.maxPointLightsPerMaterial = maxPointLightsPerMaterial;

            useSpatialOptimization = true;
            spatialGridSize = 50.0f; // Grid cell size for spatial partitioning
        }

        /// <summary>
        /// Set the environment lighting for the scene
        /// </summary>
        public void SetEnvironmentLight(EnvironmentLight light)
        {
            environmentLight = light ?? new EnvironmentLight();
        }

        /// <summary>
        /// Add a point light to the scene
        /// </summary>
        public void AddPointLight(PointLight light)
        {
            if (light != null && !pointLights.Contains(light))
            {
                pointLights.Add(light);
            }
        }

        /// <summary>
        /// Remove a point light from the scene
        /// </summary>
        public void RemovePointLight(PointLight light)
        {
            pointLights.Remove(light);
        }

        /// <summary>
        /// Clear all point lights
        /// </summary>
        public void ClearPointLights()
        {
            pointLights.Clear();
        }

        /// <summary>
        /// Find a point light by name
        /// </summary>
        public PointLight FindLight(string name)
        {
            return pointLights.FirstOrDefault(l => l.Name == name);
        }

        /// <summary>
        /// Update all lights
        /// </summary>
        public void Update(GameTime gameTime)
        {
            foreach (var light in pointLights)
            {
                light.Update(gameTime);
            }
        }

        /// <summary>
        /// Apply lighting to a material at a specific world position
        /// </summary>
        public void ApplyLighting(IIlluminate material, Vector3 worldPosition, BoundingBox? bounds = null)
        {
            if (material == null || !material.ReceivesLighting)
                return;

            // Apply environment lighting
            material.ApplyEnvironmentLight(environmentLight);

            // Get relevant point lights for this position
            var relevantLights = GetRelevantPointLights(worldPosition, bounds, material.MaxPointLights);

            // Apply point lights
            material.ApplyPointLights(relevantLights, worldPosition);
        }

        /// <summary>
        /// Apply lighting to an effect directly (for materials that don't implement IIlluminate)
        /// </summary>
        public void ApplyLightingToEffect(Effect effect, Vector3 worldPosition, int maxLights = 8)
        {
            if (effect == null) return;

            // Apply environment light
            environmentLight.ApplyToEffect(effect);

            // Get relevant point lights
            var relevantLights = GetRelevantPointLights(worldPosition, null, maxLights).ToList();

            // Prepare arrays for shader
            var positions = new Vector3[maxLights];
            var colors = new Vector3[maxLights];
            var ranges = new float[maxLights];
            var intensities = new float[maxLights];

            for (int i = 0; i < Math.Min(relevantLights.Count, maxLights); i++)
            {
                var light = relevantLights[i];
                positions[i] = light.Position;
                colors[i] = light.Color.ToVector3();
                ranges[i] = light.Range;
                intensities[i] = light.Intensity;
            }

            // Set shader parameters
            effect.Parameters["ActivePointLights"]?.SetValue(Math.Min(relevantLights.Count, maxLights));
            effect.Parameters["PointLightPositions"]?.SetValue(positions);
            effect.Parameters["PointLightColors"]?.SetValue(colors);
            effect.Parameters["PointLightRanges"]?.SetValue(ranges);
            effect.Parameters["PointLightIntensities"]?.SetValue(intensities);
        }

        /// <summary>
        /// Get the most relevant point lights for a given position
        /// </summary>
        private IEnumerable<PointLight> GetRelevantPointLights(Vector3 worldPosition, BoundingBox? bounds, int maxLights)
        {
            var activeLights = pointLights.Where(l => l.IsEnabled);

            if (useSpatialOptimization && bounds.HasValue)
            {
                // Use bounding box to filter lights
                activeLights = activeLights.Where(l =>
                {
                    var lightBounds = new BoundingSphere(l.Position, l.Range);
                    return bounds.Value.Intersects(lightBounds);
                });
            }

            // Sort by distance and importance (intensity * range)
            var sortedLights = activeLights
                .Select(l => new
                {
                    Light = l,
                    Distance = Vector3.Distance(l.Position, worldPosition),
                    Importance = l.Intensity * l.Range / Math.Max(1, Vector3.Distance(l.Position, worldPosition))
                })
                .Where(l => l.Distance <= l.Light.Range) // Only lights in range
                .OrderByDescending(l => l.Importance) // Most important first
                .Take(maxLights)
                .Select(l => l.Light);

            return sortedLights;
        }

        /// <summary>
        /// Debug render light positions and ranges
        /// </summary>
        public void DebugRender(GraphicsDevice graphicsDevice, BasicEffect debugEffect, Matrix view, Matrix projection)
        {
            // This would render debug spheres for each light's position and range
            // Implementation depends on your debug rendering system
            foreach (var light in pointLights.Where(l => l.IsEnabled))
            {
                // Render a small sphere at light position
                RenderDebugSphere(graphicsDevice, debugEffect, light.Position, 0.5f, light.Color, view, projection);

                // Optionally render range as wireframe sphere
                if (light.Intensity > 0)
                {
                    var rangeColor = new Color(light.Color.ToVector3() * 0.2f);
                    RenderDebugWireSphere(graphicsDevice, debugEffect, light.Position, light.Range, rangeColor, view, projection);
                }
            }
        }

        private void RenderDebugSphere(GraphicsDevice graphicsDevice, BasicEffect effect, Vector3 position, float radius, Color color, Matrix view, Matrix projection)
        {
            // Simplified debug sphere rendering
            effect.World = Matrix.CreateScale(radius) * Matrix.CreateTranslation(position);
            effect.View = view;
            effect.Projection = projection;
            effect.DiffuseColor = color.ToVector3();
            effect.EmissiveColor = color.ToVector3() * 0.5f;

            // You would need actual sphere geometry here
            // This is a placeholder for the concept
        }

        private void RenderDebugWireSphere(GraphicsDevice graphicsDevice, BasicEffect effect, Vector3 position, float radius, Color color, Matrix view, Matrix projection)
        {
            // Wireframe sphere for range visualization
            // Implementation would draw circle lines for the sphere outline
        }

        /// <summary>
        /// Create common lighting scenarios
        /// </summary>
        public void SetupIndoorLighting()
        {
            ClearPointLights();
            SetEnvironmentLight(new EnvironmentLight
            {
                DirectionalLightIntensity = 0.2f,
                AmbientLightColor = new Color(0.3f, 0.3f, 0.35f),
                AmbientLightIntensity = 0.5f
            });
        }

        public void SetupOutdoorLighting()
        {
            ClearPointLights();
            SetEnvironmentLight(EnvironmentLight.CreateDaylight());
        }

        public void SetupDungeonLighting()
        {
            ClearPointLights();
            SetEnvironmentLight(new EnvironmentLight
            {
                DirectionalLightIntensity = 0.1f,
                AmbientLightColor = new Color(0.05f, 0.05f, 0.1f),
                AmbientLightIntensity = 0.3f,
                FogEnabled = true,
                FogColor = new Color(0.0f, 0.0f, 0.05f),
                FogStart = 10.0f,
                FogEnd = 50.0f
            });
        }
    }
}