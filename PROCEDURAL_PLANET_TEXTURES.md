# Procedural Planet Texture System

This document explains the procedural planet texture system implementation, including shader setup, common mistakes, and troubleshooting.

## Overview

The procedural planet system generates detailed terrain using heightmaps with height-based color gradients. The system includes:

- **Height-based terrain colors**: 6 distinct terrain types based on elevation
- **Heightmap generation**: 1024x1024 resolution noise-based heightmaps
- **Custom HLSL shaders**: Cross-platform compatible shaders
- **Real-time parameter adjustment**: Interactive sliders for terrain generation
- **Water sphere rendering**: Separate animated water layer

## Core Files

### Shader Files
- `Content/Assets/shaders/surface/Unlit.fx` - Main planet shader with terrain gradients
- `Content/Assets/shaders/WaterShader.fx` - Water rendering shader

### Code Files
- `game/scenes/AdvancedProceduralPlanetScreen.cs` - Main scene implementation
- `system/procedural/ProceduralPlanetGenerator.cs` - Planet mesh and heightmap generation
- `system/procedural/WaterSphereRenderer.cs` - Water sphere implementation

## Shader System

### Terrain Color Gradients

The planet shader uses 6 distinct height-based terrain colors:

```hlsl
float3 beachColor = float3(0.96, 0.64, 0.38);      // SandyBrown
float3 lowlandColor = float3(0.0, 0.5, 0.0);       // Green
float3 midlandColor = float3(0.34, 0.68, 0.16);    // ForestGreen
float3 highlandColor = float3(0.54, 0.27, 0.07);   // SaddleBrown
float3 mountainColor = float3(0.41, 0.41, 0.41);   // DimGray
float3 peakColor = float3(1.0, 1.0, 1.0);          // White
```

### Height Thresholds

Terrain transitions occur at these normalized height values:
- Beach: 0.0 - 0.1
- Lowland: 0.1 - 0.25
- Midland: 0.25 - 0.5
- Highland: 0.5 - 0.7
- Mountain: 0.7 - 0.85
- Peak: 0.85 - 1.0

## Common Mistakes and Solutions

### 1. Shader Compilation Errors

**Problem**: `HRESULT E_INVALIDARG` when loading shaders

**Common Causes**:
- Missing shader model compatibility headers
- Using hardcoded shader models instead of macros
- Incorrect vertex input structure

**Solution**:
```hlsl
// Always include at the top of shader files
#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Use macros in technique compilation
technique Unlit
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL VS();
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
```

### 2. Shader Loading Path Issues

**Problem**: Content not found exceptions

**Common Causes**:
- Including "Assets/" in content load path
- Shader not registered in Content.mgcb
- Path case sensitivity issues

**Solution**:
```csharp
// Correct - matches Content.mgcb output path
planetShader = Content.Load<Effect>("shaders/surface/Unlit");

// Incorrect - includes Assets folder
planetShader = Content.Load<Effect>("Assets/shaders/surface/Unlit");
```

### 3. Matrix Type Mismatches

**Problem**: Shader parameter binding failures

**Common Causes**:
- Using `matrix` instead of `float4x4`
- Inconsistent matrix declarations

**Solution**:
```hlsl
// Correct - use float4x4 for compatibility
float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldInverseTranspose;
```

### 4. Vertex Input Structure Mismatches

**Problem**: Vertex data not reaching shader correctly

**Common Causes**:
- Shader expecting different vertex format than mesh provides
- Missing semantic bindings

**Solution**:
```hlsl
// Must match VertexPositionNormalTexture structure
struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TexCoord : TEXCOORD0;
};
```

### 5. Texture Sampling Issues

**Problem**: Heightmap colors appear incorrect

**Common Causes**:
- Wrong texture sampler settings
- Incorrect UV coordinates
- Missing texture binding

**Solution**:
```hlsl
sampler HeightmapSampler = sampler_state
{
    Texture = <HeightmapTexture>;
    MinFilter = POINT;  // Preserves heightmap precision
    MagFilter = POINT;
    MipFilter = NONE;
    AddressU = WRAP;
    AddressV = WRAP;
};
```

## Parameter Setup

### Required Shader Parameters

```csharp
// In C# scene code
planetShader.Parameters["World"].SetValue(world);
planetShader.Parameters["View"].SetValue(camera.View);
planetShader.Parameters["Projection"].SetValue(camera.Projection);
planetShader.Parameters["WorldInverseTranspose"].SetValue(Matrix.Transpose(Matrix.Invert(world)));
planetShader.Parameters["CameraPosition"].SetValue(camera.Position);
planetShader.Parameters["HeightmapTexture"].SetValue(planetGenerator.HeightmapTexture);
```

### Optional Parameters

```csharp
planetShader.Parameters["Brightness"]?.SetValue(1.0f);
planetShader.Parameters["VertexJitterAmount"]?.SetValue(30.0f);
planetShader.Parameters["AffineAmount"]?.SetValue(0.0f);
```

## Content Pipeline Setup

### Content.mgcb Configuration

Ensure shaders are properly registered:

```
#begin Assets/shaders/surface/Unlit.fx
/importer:EffectImporter
/processor:EffectProcessor
/processorParam:DebugMode=Auto
/build:Assets/shaders/surface/Unlit.fx
/copy:shaders/surface/Unlit.xnb
```

## Testing and Debugging

### 1. Verify Shader Compilation

```bash
cd rubens-psx-engine
dotnet build
```

Look for shader compilation in build output. Any HLSL errors will appear here.

### 2. Runtime Testing

Key indicators of working system:
- No HRESULT errors during shader loading
- Planet renders with color gradients
- Heightmap generation saves successfully
- Camera controls work smoothly

### 3. Common Debug Outputs

```
Failed to load water shader: [Expected - water shader has separate issues]
Heightmap saved to: .\heightmap_seed_XXX.png [Good - indicates working system]
```

## Performance Considerations

### Heightmap Resolution
- Default: 1024x1024 (good balance of detail/performance)
- Higher: 2048x2048+ (very detailed, impacts performance)
- Lower: 512x512 (faster generation, less detail)

### Mesh Subdivision
- Default: 3 levels (good for most cases)
- Higher: 4+ levels (more triangles, smoother sphere)
- Lower: 2 levels (fewer triangles, visible faceting)

## Controls

### Runtime Controls
- **R**: Randomize seed and regenerate planet
- **V**: Toggle between vertex colors and shader rendering
- **O**: Toggle water sphere visibility
- **S**: Save current heightmap to disk
- **Tab**: Toggle UI visibility

### UI Sliders
- **Continent Frequency**: Controls size of landmasses
- **Mountain Frequency**: Controls mountain detail density
- **Ocean Level**: Adjusts water sphere size
- **Mountain Height**: Controls terrain elevation variance

## Troubleshooting

### Application Crashes on Startup
1. Check shader compilation in build output
2. Verify Content.mgcb has correct shader paths
3. Ensure all required shader parameters are set
4. Check for null reference exceptions in shader loading

### Incorrect Colors
1. Verify heightmap texture is being passed to shader
2. Check height threshold values in GetTerrainColor()
3. Ensure texture sampler settings are correct
4. Verify brightness parameter is set properly

### Performance Issues
1. Reduce heightmap resolution
2. Lower mesh subdivision levels
3. Disable post-processing if enabled
4. Check for excessive draw calls

### Water Rendering Issues
Water shader uses separate implementation and may have independent compilation issues. The planet system works independently of water rendering.

## Future Enhancements

Planned improvements include:
- Normal map generation from heightmaps
- Enhanced directional lighting
- Improved water shader with waves and transparency
- Texture blending between terrain types
- Atmospheric scattering effects