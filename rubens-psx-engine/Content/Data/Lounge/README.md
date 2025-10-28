# The Lounge - Murder Mystery Game Data

This folder contains all data files for "The Lounge" murder mystery game.

## Files

### characters.yml
**Main character configuration file** - Contains all character data including:
- Character metadata (name, species, role, personality, portrait paths, model paths)
- Transform data (position, rotation, scale)
- Camera interaction settings
- Physics collider dimensions
- Investigation details (secrets, motives, evidence)
- Complete dialogue sequences for each character
- Game settings (spawn sequence, scaling factors)

**Build Configuration:** Set to `CopyToOutputDirectory: PreserveNewest` in the .csproj file

**Usage:**
```csharp
using anakinsoft.system;

// Load all character data
var characterData = CharacterDataLoader.LoadCharacterData();

// Get specific character
var bartender = characterData.GetCharacter("bartender");

// Convert to game objects
Vector3 position = bartender.Position.ToVector3(characterData.GameSettings.PositionScale);
Quaternion rotation = bartender.Rotation.ToQuaternion();

// Create dialogue
DialogueSequence dialogue = CharacterDataLoader.CreateDialogueFromData(bartender);
```

### evidence.md
Detective's evidence briefing - Contains all clues and forensic findings about the murder case.

### murder_mystery_characters_guide.md
Character profiles and investigation guide - Detailed background on all suspects, their motives, and relationships.

## Character Data Structure

Each character in `characters.yml` includes:

```yaml
character_id:
  # Basic Info
  name: "Character Name"
  species: "Species"
  role: "Job Title"
  personality: "Personality traits"
  portrait: "textures/chars/portrait_file"
  model: "models/characters/model_name"

  # Transform
  position: {x, y, z}
  rotation: {yaw, pitch, roll}  # in degrees
  scale: 0.3

  # Camera Settings
  camera_position: {x, y, z}
  camera_look_at: {x, y, z}

  # Physics
  collider: {width, height, depth}

  # Investigation
  public_story: "What they claim"
  secret: "Hidden truth"
  is_killer: true/false
  red_herring: true/false
  key_evidence: ["clue 1", "clue 2"]

  # Dialogue
  dialogue:
    sequence_name: "SequenceName"
    lines:
      - speaker: "Speaker Name"
        text: "Dialogue text"
    on_complete: "action_to_trigger"
```

## Characters

### Core Characters
- **Bartender (Zix)** - Introduces the case
- **Dr. Harmon Kerrigan** - Pathologist who presents evidence

### Suspects (Primary)
- **Commander Sylara Von** - Ambassador's Head of Security ⚠️ KILLER
- **Dr. Lyssa Thorne** - Xenoanthropologist ⚠️ ACCOMPLICE

### Suspects (Red Herrings)
- **Lieutenant Marcus Webb** - Tactical Officer
- **Ensign Tork** - Junior Engineer
- **Maven Kilroth** - Trade Negotiator
- **Chief Petty Officer Raina Solis** - Head of Ship Security
- **T'Vora** - Federation Diplomatic Attaché
- **Lucky Chen** - Ship's Quartermaster

## Coordinate System

- Position values are in local scene units
- Multiply by `game_settings.position_scale` (default: 10.0) to get world coordinates
- Rotations are in degrees (Yaw, Pitch, Roll)
- Y-axis is up

## Editing Guidelines

1. **Positions**: Use the in-game debug visualizer to find character positions
2. **Dialogue**: Keep lines under 150 characters for readability
3. **Portraits**: Portrait images must exist in Content/textures/chars/
4. **Models**: Model files must exist in Content/models/characters/
5. **Validation**: Run `CharacterDataLoader.LoadCharacterData()` to validate YAML syntax

## Integration Status

- ✅ YAML structure defined
- ✅ Character data loader implemented
- ✅ Build configuration set up
- ⏳ Scene integration (in progress)
- ⏳ Dialogue system integration (in progress)
- ⏳ Investigation system (planned)

## Future Enhancements

- [ ] Load suspect positions dynamically from YAML
- [ ] Replace hardcoded dialogue with YAML data
- [ ] Add character relationship graph
- [ ] Implement evidence tracking system
- [ ] Add interrogation dialogue trees
- [ ] Create accusation/solution validation
