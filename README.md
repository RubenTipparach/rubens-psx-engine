# Rubens PSX Engine

A retro PSX-style rendering engine built with MonoGame, featuring low-poly aesthetics, vertex jitter, affine texture mapping, and classic PlayStation-era visual effects.

![image](RPE_logo.gif)

## Overview

Rubens PSX Engine is a game engine that recreates the distinctive visual style of PlayStation 1 era games. It combines modern development tools with nostalgic PSX rendering techniques to create unique gaming experiences.

Base code for this engine is based off: https://github.com/blendogames/fna_starterkit but was ported to MonoGame for better Content Pipeline and shader support.

## Games in Development

### **The Lounge** - Murder Mystery Detective Game (~40% Complete)
A first-person murder mystery where you play as a detective investigating a crime on a space station. Features include:
- **Dialogue System**: Dynamic conversation trees with multiple NPCs (Bartender, Pathologist, etc.)
- **Evidence Collection**: Gather and examine clues scattered throughout the lounge
- **Interrogation System**: Select and question suspects to uncover the truth
- **Character State Machines**: NPCs with complex dialogue states and story progression
- **Transcript Review**: Review interview transcripts and track investigation progress

**Current Status**: Core gameplay loop functional, interrogation system complete, working on story content and additional suspects.

### **The Corridor** - Atmospheric Horror/Exploration (~25% Complete)
A first-person atmospheric experience set in a mysterious corridor. Features include:
- FPS character controller with physics
- Environmental storytelling
- PSX-style lighting and fog effects

**Current Status**: Basic environment and movement complete, working on narrative elements.

### Prototypes & Tech Demos

#### **Procedural Planet Generator**
- Real-time procedural planet generation
- LOD system for terrain rendering
- Atmospheric effects
- Orbital camera controls

#### **RTS Terrain System**
- Grid-based terrain for strategy games
- Unit selection and movement
- Pathfinding system
- Real-time strategy gameplay foundation

#### **Other Tech Demos**
- Static Mesh Demo - Model loading and rendering showcase
- Graphics Test Scene - Shader and lighting tests
- Bepu Physics Integration - Advanced physics demonstrations
- FPS Sandbox - General testing environment

## Features

### Rendering
- **PSX-Style Effects**: Vertex jitter, affine texture mapping, dithering
- **Configurable Post-Processing**: Bloom, color tint, antialiasing
- **Retro Resolution**: Configurable low-res rendering with nearest-neighbor scaling
- **Custom Shaders**: Vertex-lit materials with PSX aesthetic

### Physics
- Integrated **BepuPhysics v2** for realistic physics simulation
- Character controller with collision detection
- Raycasting for interactions and line-of-sight
- Static and dynamic physics bodies

### Systems
- **Dialogue System**: YAML-based dialogue trees with branching conversations
- **Interaction System**: Raycast-based object interaction (20-unit range)
- **Character State Machines**: Complex NPC behavior and progression
- **UI Framework**: Custom UI for menus, dialogues, and HUD elements
- **Camera System**: FPS camera with transitions and interpolation
- **Scene Management**: Easy scene switching and organization

### Content Pipeline
- FBX model import with custom processing
- Texture pipeline with PSX-appropriate formats
- Font rendering system
- YAML data loading for dialogue and configuration

## Getting Started

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/en-us/download) (9.0 or later)
- MonoGame templates

### Installation

1. Install MonoGame templates:
```bash
dotnet new install MonoGame.Templates.CSharp
```

2. Clone the repository:
```bash
git clone https://github.com/yourusername/rubens-psx-engine.git
cd rubens-psx-engine
```

3. Restore packages:
```bash
dotnet restore
```

4. Build and run:
```bash
dotnet build
dotnet run
```

### Troubleshooting

If you encounter package mapper errors:
```bash
dotnet nuget locals -c all
dotnet restore
```

## Configuration

The engine uses `config.yml` for runtime configuration:

```yaml
# Rendering settings
dither:
  renderWidth: 320
  renderHeight: 180
  strength: 0.8
  colorLevels: 6

# Development settings
development:
  enableScreenshots: true
  skipToSuspectSelection: false  # Debug mode for The Lounge
```

## Project Structure

```
rubens-psx-engine/
├── rubens-psx-engine/
│   ├── game/
│   │   ├── scenes/
│   │   │   ├── lounge/          # The Lounge murder mystery
│   │   │   │   ├── characters/  # NPC state machines
│   │   │   │   ├── evidence/    # Evidence items
│   │   │   │   └── ui/          # Lounge-specific UI
│   │   │   └── CorridorScreen.cs # The Corridor scene
│   │   └── units/               # RTS unit system
│   ├── system/
│   │   ├── character/           # Character controller
│   │   ├── physics/             # Physics integration
│   │   ├── rendering/           # Render pipeline
│   │   └── config/              # Configuration system
│   ├── entities/                # Game objects
│   └── Content/
│       ├── Assets/              # Models, textures, fonts
│       └── Data/                # YAML configuration files
└── bepuphysics2/                # Physics engine submodule
```

## Technology Stack

- **Engine**: MonoGame (Cross-platform game framework)
- **Physics**: BepuPhysics v2 (High-performance physics simulation)
- **Language**: C# (.NET 9.0)
- **Data**: YAML (via YamlDotNet) for dialogue and configuration
- **Platform**: Windows (primary), with cross-platform potential

## Development Status

**Overall Engine**: Beta - Core systems functional, actively developing game content

**Roadmap**:
- [ ] Complete The Lounge story and additional suspects
- [ ] Expand The Corridor narrative experience
- [ ] Sound system implementation
- [ ] Save/load system
- [ ] Additional PSX-style post-processing effects
- [ ] Performance optimizations
- [ ] Linux/Mac builds

## Contributing

This is currently a personal project, but feedback and suggestions are welcome through issues.

## License

[License information to be added]

## Credits

- Base engine structure inspired by [FNA Starter Kit](https://github.com/blendogames/fna_starterkit)
- Physics powered by [BepuPhysics v2](https://github.com/bepu/bepuphysics2)
- Built with [MonoGame](https://www.monogame.net/)

## Acknowledgments

Special thanks to the MonoGame community and all the open-source contributors who make game development accessible.
