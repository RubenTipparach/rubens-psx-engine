# Character Integration System Guide

## Overview

The character integration system connects dialogue, names, portraits, and metadata into a unified system. This makes it easy to add new characters and dialogue states without modifying multiple files.

## Key Components

### 1. CharacterProfile (`characters/CharacterProfile.cs`)
Unified data class containing all character information:
- **Identity**: ID, Name, Role, Species
- **Portrait**: Path, Key, and cached Texture2D
- **Metadata**: Personality, Pronouns, Gender, Age
- **Investigation**: Public Story, Secret, IsKiller, Key Evidence
- **Dialogue States**: Extensible dictionary mapping state names to sequence names
- **Flags**: Boolean flags for tracking progress
- **Custom Data**: Dictionary for game-specific data

### 2. CharacterProfileManager (`characters/CharacterProfileManager.cs`)
Centralizes character data loading and management:
- Loads all characters from YAML (`characters.yml`)
- Manages portrait loading from Content Manager
- Provides lookup by ID or name
- Filters characters (e.g., interrogatable only)
- Generates portrait keys for consistent lookup

### 3. YAML Structure (`Content/Data/Lounge/characters.yml`)
Each character in the YAML file contains:
```yaml
character_id:
  name: "Character Name"
  role: "Character Role"
  species: "Species Name"
  portrait: "textures/chars/portrait_file"

  # Dialogue sequences
  dialogue:
    - sequence_name: "IntroSequence"
      lines:
        - speaker: "Character Name"
          text: "Dialogue line..."
      on_complete: "flag_or_action"

    - sequence_name: "StateTwo"
      lines:
        - speaker: "Character Name"
          text: "More dialogue..."

  # Investigation data
  public_story: "What they claim"
  secret: "What they're hiding"
  is_killer: false
  key_evidence:
    - "Evidence item 1"
    - "Evidence item 2"
```

## How to Use

### Adding a New Character

1. **Add to `characters.yml`**:
```yaml
new_character:
  name: "New Character Name"
  role: "Their Role"
  species: "Species"
  portrait: "textures/chars/new_character_portrait"
  model: "models/characters/alien-2"

  position:
    x: 100.0
    y: 0.0
    z: 200.0

  # ... other position/camera data

  dialogue:
    - sequence_name: "NewCharacterIntro"
      lines:
        - speaker: "New Character Name"
          text: "Hello, Detective."
```

2. **Update CharacterProfileManager** (if needed):
Add loading in `LoadFromYaml()`:
```csharp
CreateProfile("new_character", yamlData.new_character);
```

3. **Portrait will auto-load** from the path specified in YAML

### Adding New Dialogue States

To add a new dialogue state to an existing character:

1. **Add to YAML**:
```yaml
bartender:
  # ... existing data
  dialogue:
    - sequence_name: "ExistingSequence"
      # ... existing dialogue

    # NEW STATE
    - sequence_name: "BartenderAngry"
      lines:
        - speaker: "Bartender"
          text: "I don't appreciate that accusation, Detective."
      on_complete: "set_bartender_hostile"
```

2. **Access in code**:
```csharp
var profile = profileManager.GetProfile("bartender");
string angrySequence = profile.GetDialogueSequence("BartenderAngry");
```

### Using Character Flags

Track progress with flags:

```csharp
// Set a flag
profile.SetFlag("has_been_interrogated", true);
profile.SetFlag("revealed_secret", true);

// Check a flag
if (profile.HasFlag("has_been_interrogated"))
{
    // Show different dialogue
}
```

### Custom Data

Store game-specific data:

```csharp
// Set custom data
profile.SetCustomData("interrogation_count", 2);
profile.SetCustomData("last_topic", "breturium");
profile.SetCustomData("suspicion_level", 75.5f);

// Get custom data
int count = profile.GetCustomData<int>("interrogation_count");
string topic = profile.GetCustomData<string>("last_topic", "none");
```

## Integration Points

### LoungeUIManager
- Loads portraits via CharacterProfileManager
- Displays character name and role from profile
- Shows portrait during dialogue/hover

### CharacterSelectionMenu
- Uses portrait keys from profiles
- Displays character metadata (name, role)
- Can filter based on profile flags

### DialogueSystem
- Retrieves dialogue sequences by name
- Can branch based on character flags
- Updates character state after sequences

### Character State Machines
- Access profile for character data
- Update flags for progression
- Track interrogation history

## Example: Full Character Integration

```csharp
// In TheLoungeScene initialization
var profileManager = new CharacterProfileManager();
profileManager.LoadFromYaml(yamlData);
profileManager.LoadPortraits();

// Get a character
var bartender = profileManager.GetProfile("bartender");

// Show their portrait
var portrait = bartender.Portrait;
var portraitKey = bartender.PortraitKey;

// Start dialogue
string introSequence = bartender.GetDialogueSequence("BartenderIntro");
dialogueSystem.StartSequence(introSequence);

// After dialogue completes
bartender.SetFlag("intro_complete", true);

// Check for next dialogue state
if (bartender.HasFlag("intro_complete") && !bartender.HasFlag("report_delivered"))
{
    string nextSequence = bartender.GetDialogueSequence("BartenderPostIntro");
    dialogueSystem.StartSequence(nextSequence);
}
```

## Best Practices

1. **Keep YAML as source of truth**: All character data starts in `characters.yml`
2. **Use meaningful state names**: `"PathologistEvidenceReview"` not `"seq3"`
3. **Track progress with flags**: Don't duplicate state in multiple places
4. **Use custom data for temporary state**: Interrogation counts, topics discussed, etc.
5. **Portrait keys are auto-generated**: Manager handles name normalization
6. **Dialogue sequences are extensible**: Add as many states as needed

## Future Enhancements

The system is designed to support:
- **Branching dialogue trees**: Multiple sequences per state
- **Conditional dialogue**: Based on flags and evidence
- **Dynamic relationships**: Character opinions of each other
- **Investigation scoring**: Track clues discovered per character
- **Multiple interrogation rounds**: Each with different dialogue
- **Character development**: Personality changes based on player actions

## Migration Notes

The existing hardcoded portrait loading in `LoungeUIManager` can be replaced with:
```csharp
// Old way (hardcoded)
characterPortraits["DrHarmon"] = Globals.screenManager.Content.Load<Texture2D>(...);

// New way (via ProfileManager)
profileManager.LoadPortraits();
characterPortraits = profileManager.GetAllPortraits();
```

The `GetCharacterInfo()` method can be replaced by:
```csharp
var profile = profileManager.GetProfileByPortraitKey(characterKey);
return (profile.Name, profile.Role);
```
