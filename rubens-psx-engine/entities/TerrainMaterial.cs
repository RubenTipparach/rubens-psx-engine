using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rubens_psx_engine.system.lighting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace rubens_psx_engine.entities
{
    /// <summary>
    /// Specialized material for terrain rendering with multi-light support
    /// </summary>
    public class TerrainMaterial : Material, IIlluminate
    {
        // IIlluminate implementation
        public bool ReceivesLighting { get; set; } = true;
        public int MaxPointLights => 8;

        // Terrain-specific properties
        public Vector2 TextureTiling { get; set; } = new Vector2(10.0f, 10.0f);
        public Vector3 LightDirection { get; set; } = Vector3.Down;

        public TerrainMaterial(string texturePath = null)
            : base("shaders/surface/VertexLitStandard", texturePath)
        {
        }

        public override void Apply(Camera camera, Matrix worldMatrix)
        {
            if (effect == null) return;

            // Set standard matrices
            effect.Parameters["World"]?.SetValue(worldMatrix);
            effect.Parameters["View"]?.SetValue(camera.View);
            effect.Parameters["Projection"]?.SetValue(camera.Projection);

            // Set texture and tiling
            if (texture != null)
            {
                effect.Parameters["Texture"]?.SetValue(texture);
            }
            effect.Parameters["TextureTiling"]?.SetValue(TextureTiling);

            // Set light direction
            effect.Parameters["LightDirection"]?.SetValue(LightDirection);

            // Apply current technique passes
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
            }
        }

        public void ApplyEnvironmentLight(EnvironmentLight environmentLight)
        {
            if (effect == null || environmentLight == null) return;

            environmentLight.ApplyToEffect(effect);
        }

        public void ApplyPointLights(IEnumerable<PointLight> pointLights, Vector3 worldPosition)
        {
            if (effect == null) return;

            var lights = pointLights.Take(MaxPointLights).ToList();

            // Prepare arrays for shader
            var positions = new Vector3[MaxPointLights];
            var colors = new Vector3[MaxPointLights];
            var ranges = new float[MaxPointLights];
            var intensities = new float[MaxPointLights];

            for (int i = 0; i < lights.Count; i++)
            {
                var light = lights[i];
                positions[i] = light.Position;
                colors[i] = light.Color.ToVector3();
                ranges[i] = light.Range;
                intensities[i] = light.Intensity;
            }

            // Set shader parameters
            effect.Parameters["ActivePointLights"]?.SetValue(lights.Count);
            effect.Parameters["PointLightPositions"]?.SetValue(positions);
            effect.Parameters["PointLightColors"]?.SetValue(colors);
            effect.Parameters["PointLightRanges"]?.SetValue(ranges);
            effect.Parameters["PointLightIntensities"]?.SetValue(intensities);
        }

        public void ClearLighting()
        {
            if (effect == null) return;

            // Clear point lights
            effect.Parameters["ActivePointLights"]?.SetValue(0);

            // Set default environment light
            effect.Parameters["LightDirection"]?.SetValue(Vector3.Down);
            effect.Parameters["LightColor"]?.SetValue(Vector3.One);
            effect.Parameters["LightIntensity"]?.SetValue(0.0f);
            effect.Parameters["AmbientColor"]?.SetValue(Vector3.Zero);
        }

        public Effect GetEffect()
        {
            return effect;
        }

        public override bool IsCompatibleWithVertexDeclaration(VertexDeclaration vertexDeclaration)
        {
            // Terrain requires VertexPositionNormalTexture
            return vertexDeclaration.GetVertexElements().Any(e => e.VertexElementUsage == VertexElementUsage.Position) &&
                   vertexDeclaration.GetVertexElements().Any(e => e.VertexElementUsage == VertexElementUsage.Normal) &&
                   vertexDeclaration.GetVertexElements().Any(e => e.VertexElementUsage == VertexElementUsage.TextureCoordinate);
        }
    }
}