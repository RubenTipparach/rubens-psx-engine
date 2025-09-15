# Dither System Upgrade Guide

## Overview

The dither system has been completely overhauled to provide authentic retro pixelated rendering. Instead of just applying dithering as a texture downsampling effect, the new system renders the entire scene at a low resolution (320x180 by default) and then scales it up with perfect pixel scaling to maintain the retro aesthetic.

## Key Changes

### ✅ What's New

1. **Dual-Resolution Rendering**: The game now renders at a configurable low resolution (default: 320x180) internally while displaying at your chosen menu resolution
2. **YAML Configuration**: All dither and post-processing settings are now in an easy-to-edit `config.yml` file
3. **Configurable Parameters**: Dither strength, color levels, and render resolution can all be tweaked
4. **Better Performance**: Rendering at low resolution improves performance while maintaining visual quality
5. **Aspect Ratio Support**: Proper letterboxing/pillarboxing when maintaining aspect ratio

### 🔧 New Files Added

- `config.yml` - Main configuration file for all rendering settings
- `system/config/RenderingConfig.cs` - Configuration management system
- `system/postprocess/ScaledPostProcessStack.cs` - Dual-resolution post-processing pipeline
- `system/postprocess/RetroRenderer.cs` - High-level renderer wrapper
- Updated `system/postprocess/DitherEffect.cs` - Now uses config settings

## Configuration Options

### Dither Settings (`config.yml`)

```yaml
dither:
  # Internal rendering resolution (lower = more pixelated)
  renderWidth: 320
  renderHeight: 180
  
  # Dithering strength (0.0 = no dithering, 1.0 = full dithering)
  strength: 0.8
  
  # Color quantization levels (lower = fewer colors, more retro)
  colorLevels: 6.0
  
  # Whether to use point sampling (pixelated) or linear sampling
  usePointSampling: true
```

### Quick Tweaks

- **More pixelated**: Lower `renderWidth`/`renderHeight` (try 240x135 or 160x90)
- **Less pixelated**: Higher `renderWidth`/`renderHeight` (try 480x270 or 640x360)
- **More dithering**: Higher `strength` (0.0 to 1.0)
- **Fewer colors**: Lower `colorLevels` (try 4.0 or 3.0 for extreme retro look)
- **Perfect pixels**: Keep `usePointSampling: true`

## Integration Guide

### Option 1: Using RetroRenderer (Recommended)

```csharp
public class ScreenManager : Game
{
    private RetroRenderer retroRenderer;
    
    protected override void LoadContent()
    {
        retroRenderer = new RetroRenderer(GraphicsDevice, this);
        retroRenderer.Initialize();
    }
    
    protected override void Draw(GameTime gameTime)
    {
        // Start rendering at low resolution
        retroRenderer.BeginScene();
        
        // Draw all your 3D/2D content here at low resolution
        DrawGame(gameTime);
        
        // Apply post-processing and scale to display resolution
        retroRenderer.EndScene();
    }
}
```

### Option 2: Using ScaledPostProcessStack Directly

```csharp
public class ScreenManager : Game
{
    private ScaledPostProcessStack postProcessStack;
    
    protected override void LoadContent()
    {
        postProcessStack = new ScaledPostProcessStack(GraphicsDevice, this);
        
        // Add effects
        postProcessStack.AddEffect(new BloomEffect());
        postProcessStack.AddEffect(new DitherEffect());
        
        postProcessStack.Initialize();
    }
    
    protected override void Draw(GameTime gameTime)
    {
        postProcessStack.BeginScene();
        DrawGame(gameTime);  // Renders at 320x180
        postProcessStack.EndScene();  // Scales to display resolution
    }
}
```

## Runtime Configuration Changes

You can modify `config.yml` at runtime and reload settings:

```csharp
// Reload configuration from file
RenderingConfigManager.ReloadConfig();

// Update effects with new settings
retroRenderer.ReloadConfig();
```

## Performance Notes

- Rendering at 320x180 instead of 1920x1080 gives roughly **30x** fewer pixels to process
- Post-processing effects run at the low resolution, improving performance
- Only the final scaling step runs at full resolution
- Use `usePointSampling: true` for authentic pixel-perfect scaling

## Visual Quality Tips

1. **Authentic PSX Look**: Use 320x180 or 256x192 resolution with 4-6 color levels
2. **Modern Retro**: Use 480x270 with higher color levels (8-12)
3. **Performance**: Lower resolution improves performance dramatically
4. **Aspect Ratios**: The system handles different aspect ratios automatically

## Migration from Old System

The old `BloomComponent` system still works alongside the new system. To migrate:

1. Replace `BloomComponent` usage with `RetroRenderer` or `ScaledPostProcessStack`
2. Move dither settings from hardcoded values to `config.yml`
3. Update your Draw() method to use `BeginScene()`/`EndScene()` pattern
4. Test different render resolutions to find your preferred look

## Troubleshooting

- **Game looks blurry**: Set `usePointSampling: true` in config
- **Too pixelated**: Increase `renderWidth`/`renderHeight`
- **Performance issues**: Lower the render resolution
- **Config not loading**: Check `config.yml` syntax and file location

The new system maintains full backward compatibility while providing much more control over the retro rendering pipeline!