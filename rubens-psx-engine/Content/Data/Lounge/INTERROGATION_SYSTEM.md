# Interrogation System Design

## Overview

The interrogation system allows the player to question suspects through a choice-based interaction system. Each suspect has a stress meter that affects their willingness to cooperate. The player must balance gathering information with managing the suspect's stress level.

## Implementation Status

### ✅ Completed
- **Stress Meter System**: Core stress tracking (0-100%) with events
- **Stress Meter UI**: Visual progress bar with color-coding (green/yellow/red)
- **Auto-Dismiss at 100%**: Characters automatically dismiss when stress maxes out
- **Round Management**: Time tracking, character spawning/despawning
- **Camera System**: Fixed transition bugs, smooth camera movement
- **Character Selection**: Suspects file shows character selection menu
- **Transcript Review**: Evidence file shows interview transcripts

### 🚧 In Progress
- **Interrogation Action UI**: 5 action buttons (Alibi, Relationship, Doubt, Accuse, Dismiss)
- **Evidence Selection UI**: For Doubt/Accuse actions
- **Stress Increase Logic**: Triggering stress on wrong answers

### ⏳ Pending
- **Correct/Wrong Answer Data**: YAML configuration for all suspects
- **Dialogue Branching**: Different responses based on player actions
- **Evidence Tracking**: Collecting and managing evidence items
- **Deduction Phase**: Final accusation system

## Core Mechanics

### Stress Meter
- **Range**: 0-100%
- **Visual**: Progress bar displayed in top-right during interrogation
- **Color Coding**: Green (0-33%), Yellow (33-66%), Red (66-100%)
- **Stress Gain**: Only increases when you wrongly accuse or wrongly doubt someone
- **Effects**:
  - **Below 100%**: No behavioral changes - suspect continues to cooperate normally
  - **Maximum Stress (100%)**: Suspect is done talking to you and self-dismisses
- **Note**: There are NO personality modifiers affecting stress at this time
- **Implementation**: `StressMeter.cs` and `StressMeterUI.cs`

### Interrogation Options

When interrogating a character, the player is presented with numbered boxes containing these options:

#### 1. **Alibi** (No Evidence Required)
- **Description**: "Where were you at the time of the murder?"
- **Purpose**: Get the suspect's official statement about their whereabouts
- **Stress Impact**: None (safe question)
- **Result**:
  - Suspect provides their alibi story
  - May reveal timeline details
  - Sets baseline for detecting lies
- **Notes**: Usually the first question asked in an interrogation

#### 2. **Relationship** (No Evidence Required)
- **Description**: "What was your relationship with the victim?"
- **Purpose**: Learn about suspect's connection to the deceased
- **Stress Impact**: None (safe question)
- **Result**:
  - Suspect describes their relationship with the Ambassador
  - May reveal motive, personal history, or emotional connections
  - Can expose hidden relationships or conflicts
- **Strategy**: Safe question that builds rapport while gathering background
- **Example Responses**:
  - "He was my superior officer, nothing more."
  - "We were working on the peace treaty together."
  - "I barely knew him, just official business."

#### 3. **Doubt** (May Require Evidence)
- **Description**: "I don't believe you. Tell me more."
- **Purpose**: Press the suspect for additional information
- **Stress Impact**: **Only increases if you're WRONG** - if you doubt correctly, no stress gain
- **Evidence Window**: Optionally present evidence to support your doubt
- **Result**:
  - **Correct Doubt (with or without evidence)**:
    - No stress gain
    - Suspect reveals more details
    - Breaks through their story
  - **Wrong Doubt**:
    - Increases stress toward 100%
    - Suspect becomes frustrated but continues answering
    - You wasted an opportunity
- **Risk**: If you're wrong, stress builds toward dismissal
- **Strategy**: Use when you suspect they're lying; attach evidence if you have it

#### 4. **Accuse** (Requires Evidence)
- **Description**: "You're the killer. This proves it."
- **Purpose**: Directly accuse the suspect of murder with evidence
- **Stress Impact**: **Only if WRONG** - fills meter to 100% and suspect self-dismisses
- **Evidence Window**: Opens automatically to select evidence
- **Result**:
  - **Correct Accusation**:
    - No stress gain
    - Suspect may confess or break down
    - Case can be closed
    - May reveal accomplices
  - **Wrong Accusation**:
    - Stress instantly reaches 100%
    - Suspect is done talking and self-dismisses immediately
    - Interrogation ends
  - **Strategic accusation** (know they didn't do it):
    - Can pressure suspect to reveal information about the real killer
    - "I know you didn't kill him, but you know who did."
    - May trade information to clear their name
- **Risk**: High stakes - wrong accusation ends interrogation immediately
- **Notes**: Point of no return - must select evidence to proceed

#### 5. **Dismiss** (No Evidence Required)
- **Description**: "That's all for now."
- **Purpose**: End the interrogation session
- **Confirmation Required**: "Are you sure you want to dismiss this suspect?"
  - Prevents accidental dismissal
  - Must select YES to confirm or NO to cancel
- **Stress Impact**: None (ends session)
- **Result**:
  - Suspect provides parting comments
  - Leaves cooperatively
  - Can be interrogated again later
  - **Note**: Stress level doesn't affect dismissal behavior (only 100% stress causes self-dismissal)
- **Strategy**: Use when you've gathered enough information or need to regroup
- **Note**: Wide button at bottom of screen to separate it from main interrogation actions

## UI Design

### Interrogation Screen Layout

```
┌─────────────────────────────────────────────────────────┐
│  [Character Portrait]    [Character Name]               │
│  [Stress Meter: ████████░░░░░░░░░░░░░] 40%            │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  "I was in my quarters all night, Detective.            │
│   I had no reason to visit the Ambassador."             │
│                                                          │
├─────────────────────────────────────────────────────────┤
│  ┌─────────────┐ ┌─────────────┐                       │
│  │      1      │ │      2      │                       │
│  │    ALIBI    │ │ RELATIONSHIP│                       │
│  │             │ │             │                       │
│  │    (Safe)   │ │    (Safe)   │                       │
│  └─────────────┘ └─────────────┘                       │
│                                                          │
│  ┌─────────────┐ ┌─────────────┐                       │
│  │      3      │ │      4      │                       │
│  │    DOUBT    │ │   ACCUSE    │                       │
│  │ (Evidence?) │ │ (Evidence!) │                       │
│  │   (Risk)    │ │ (High Risk) │                       │
│  └─────────────┘ └─────────────┘                       │
│                                                          │
│  ┌───────────────────────────────────────────────┐     │
│  │                  5. DISMISS                    │     │
│  │         (Ends Interrogation - Confirm?)        │     │
│  └───────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────┘
```

**Dismiss Confirmation Dialog:**
```
┌─────────────────────────────────────────┐
│  Are you sure you want to dismiss       │
│  this suspect?                           │
│                                          │
│  You can return to question them later.  │
│                                          │
│  ┌─────────┐        ┌─────────┐        │
│  │   YES   │        │    NO   │        │
│  └─────────┘        └─────────┘        │
└─────────────────────────────────────────┘
```

### Button Layout Rationale

**2x2 Grid for Main Actions:**
- Top Row: Low-stress questions (Alibi, Relationship)
- Bottom Row: High-risk actions (Doubt, Accuse)
- Grouped together for easy visual scanning
- Clear progression from safe to risky

**Dismiss Button Separated:**
- Full-width button at bottom
- Visually distinct from interrogation actions
- Requires confirmation to prevent accidents
- Clear "exit" position familiar from other UIs

**Benefits:**
- Reduces accidental dismissals
- Groups related actions together
- Clear visual hierarchy (info gathering → pressure → exit)
- Confirmation dialog provides safety net

### Stress Meter Visual States

```
Below 100%:  [████████████████░░░░] Yellow/Orange (showing current progress)
At 100%:     [████████████████████] Red (suspect self-dismisses)
```

**Note**: Color changes are just visual feedback - behavior only changes at exactly 100%

## Evidence System Integration

### Evidence Items
- Stored in player inventory
- Can be selected during "Press with Evidence" or "Accuse"
- Each piece of evidence has:
  - **Name**: Brief identifier
  - **Description**: What it is
  - **Relevance**: Which suspects/topics it relates to
  - **Strength**: How damning the evidence is

### Example Evidence Chain
```
1. Access Log → Shows suspect entered Ambassador's quarters
2. Breturium Sample → Links to medical bay theft
3. Telirian Wine Glass → Contains sedative residue
4. Diplomatic Code → Used to access quarters
5. Medical Records → Shows suspect has training
```

### Using Evidence

Evidence is presented through the **Doubt** and **Accuse** options:

**Doubt Option Flow:**
```
Player selects "Doubt" →
Dialog: "Use evidence?" [Yes/No] →
  IF Yes:
    Evidence inventory opens →
    Player selects evidence →
    Suspect must respond to evidence (+10-20% stress)
  IF No:
    Suspect responds to general doubt (+15-25% stress)
```

**Accuse Option Flow:**
```
Player selects "Accuse" →
Evidence inventory opens automatically (required) →
Player selects evidence to support accusation →
Suspect responds to accusation (+100% stress) →
  IF evidence is strong: Confession or major revelation
  IF evidence is weak: Interrogation ends, suspect leaves
```

**Key Points:**
- Evidence is NOT a separate button
- **Doubt** optionally uses evidence
- **Accuse** always requires evidence
- Stronger evidence = better results, lower stress increase
- Wrong evidence = wasted opportunity, higher stress

## Interrogation Flow

### Example Session Flow

```
START INTERROGATION
├─ Option 1: ALIBI (+5%)
│  └─ Suspect: "I was in the gym from 0200 to 0400 hours."
│
├─ Option 2: RELATIONSHIP (+5%)
│  └─ Suspect: "The Ambassador and I worked together. We were colleagues."
│
├─ Option 3: DOUBT without evidence (+15%)
│  └─ Suspect: "Fine! I left the gym at 0300. I went to get a drink."
│
├─ Option 3: DOUBT with evidence - Access Log (+20%)
│  └─ Suspect: "Okay! I... I went to his quarters, but he was already dead!"
│
├─ Option 4: ACCUSE with Medical Knowledge evidence (+100%)
│  └─ SUCCESS: Suspect confesses to murder
│  └─ FAIL: Suspect demands lawyer, interrogation ends
│
└─ Option 5: DISMISS
   └─ If stress < 70%: "I hope you find the real killer, Detective."
   └─ If stress >= 70%: [Suspect leaves without speaking]
```

## Strategic Considerations

### Optimal Interrogation Strategy
1. **Start with safe questions**: Alibi and Relationship to build baseline
2. **Gather evidence first**: Search the scene before interrogating
3. **Use Doubt strategically**: Only when you sense deception or have supporting evidence
4. **Build evidence chain**: Connect multiple pieces before using Doubt with evidence
5. **Save Accuse**: Only use when you have overwhelming evidence
6. **Monitor stress**: Don't push too hard too fast

### When to Use Each Option

**ALIBI**
- Beginning of interrogation
- Need to understand timeline
- Establishing baseline for lies
- Safe question with minimal stress

**RELATIONSHIP**
- Beginning of interrogation
- Understanding motive and connections
- Revealing hidden relationships
- Safe question with minimal stress

**DOUBT (without evidence)**
- Suspect's story has inconsistencies
- You don't have hard evidence yet
- Fishing for more information
- Suspect seems nervous

**DOUBT (with evidence)**
- Have specific proof contradicting their story
- Need to break through a lie without fully accusing
- Building pressure before final accusation
- More effective than doubt alone

**ACCUSE (requires evidence)**
- Have overwhelming evidence
- Ready to close the case
- Strategic pressure (know they'll crack)
- Point of no return

**DISMISS**
- Gathered enough information for now
- Need to investigate more before proceeding
- Stress is getting too high
- Need to interrogate others first

## Character-Specific Behaviors

**Note**: At this time, there are NO personality modifiers affecting stress. All characters have the same stress behavior:
- Stress only increases when you make WRONG accusations or doubts
- All characters self-dismiss at exactly 100% stress
- No character-specific stress multipliers or resistance values

## Future Enhancements

### Advanced Features
- **Relationship System**: Prior interactions affect stress rates
- **Good Cop/Bad Cop**: Multiple interrogation styles
- **Time Pressure**: Limited questions before lawyers arrive
- **Witness Confrontation**: Present testimony from other suspects
- **Lie Detection**: Mini-game or visual tells
- **Multiple Evidence Chains**: Combine evidence for stronger accusations
- **Partial Confessions**: Suspects admit to lesser crimes
- **Alibi Checking**: Cross-reference with other witness statements

### Dialogue Branching
```yaml
interrogation_state:
  low_stress_responses:
    - "I'm happy to help, Detective."
    - "Ask me anything."

  medium_stress_responses:
    - "I've told you everything I know."
    - "Why are you pushing me so hard?"

  high_stress_responses:
    - "I want a lawyer."
    - "This is harassment!"
    - "I'm done talking."
```

## Implementation Notes

### Data Structure
```yaml
character_interrogation:
  current_stress: 0  # Tracks stress 0-100

  responses:
    alibi:
      text: "I was in my quarters all night."
      is_correct: true  # Determines if stress increases
      reveals: ["quarters_alibi"]

    relationship:
      text: "The Ambassador and I were colleagues, nothing more."
      is_correct: true  # Safe question - always correct
      reveals: ["professional_relationship"]

    doubt_correct:
      text: "Fine! I stepped out briefly, but I didn't do anything!"
      stress_gain: 0  # No stress for correct doubt
      reveals: ["bar_visit", "timeline_gap"]

    doubt_wrong:
      text: "I've told you the truth, Detective!"
      stress_gain: 25  # Penalty for wrong doubt
      reveals: ["defensive_response"]

    accuse_correct:
      evidence_required: "medical_knowledge"
      text: "Alright! I did it! But I had good reason..."
      stress_gain: 0  # No stress for correct accusation
      reveals: ["confession", "motive_revealed"]
      ends_interrogation: true

    accuse_wrong:
      text: "That's ridiculous! I'm done talking to you!"
      stress_gain: 100  # Instant 100% stress
      ends_interrogation: true
      self_dismiss: true  # Character leaves immediately

    dismiss:
      text: "I hope you catch the real killer, Detective."
      # Stress level doesn't affect dismiss behavior
```

### Stress Calculation
```
Simplified System:
- Alibi: 0% stress (always safe)
- Relationship: 0% stress (always safe)
- Doubt (Correct): 0% stress
- Doubt (Wrong): +25% stress (or configurable value)
- Accuse (Correct): 0% stress
- Accuse (Wrong): +100% stress (instant self-dismissal)

Note: NO personality modifiers at this time
```

## Testing Scenarios

### Scenario 1: Successful Interrogation
- Start → Alibi → Evidence → Evidence → Accuse → Case Closed

### Scenario 2: Failed Accusation
- Start → Doubt → Doubt → Accuse (wrong evidence) → Suspect leaves

### Scenario 3: Strategic Pressure
- Start → Alibi → Doubt → Evidence → Dismiss → Return later with more evidence

### Scenario 4: Red Herring
- Interrogate innocent suspect → Build stress → They reveal information about real killer

## Summary

The interrogation system provides:
- **5 Core Options**: Alibi, Relationship, Doubt, Accuse, Dismiss
- **Dynamic Evidence System**: Evidence attached to Doubt and Accuse, not a separate option
- **Player Agency**: Multiple paths to uncover truth
- **Risk/Reward**: Balance information gain vs. stress management
- **Strategic Depth**: Choose when to press with evidence vs. bluff
- **Replayability**: Different approaches yield different information
- **Narrative Depth**: Character personalities shine through responses
- **Detective Feel**: Strategic thinking and evidence management

### Key Mechanics Recap
- **Stress System**: Simplified - only increases on WRONG doubts or accusations
- **Alibi & Relationship**: Safe questions - never increase stress
- **Doubt**: Optional evidence attachment - only increases stress if WRONG
- **Accuse**: Mandatory evidence - wrong accusation = instant 100% stress and self-dismissal
- **Dismiss**: End interrogation with confirmation dialog
- **Evidence**: Not a button - used through Doubt and Accuse options
- **100% Stress**: Only behavioral change - suspect is done talking and self-dismisses
- **No Personality Modifiers**: All characters follow the same stress rules

### UI Layout Recap
- **2x2 Grid**: Four main buttons arranged together (Alibi, Relationship, Doubt, Accuse)
  - Top row: Safe questions
  - Bottom row: High-risk actions
- **Dismiss Separated**: Full-width button at bottom with confirmation dialog
- **Confirmation Dialog**: Prevents accidental dismissal with Yes/No choice

This system transforms interrogations from simple dialogue trees into engaging tactical encounters where every choice matters.

---

## Implementation Checklist

### Core Systems
- [ ] **1. Stress Meter System**
  - [ ] Create stress tracking class (0-100%)
  - [ ] Implement simplified stress gain (only on wrong doubts/accusations)
  - [ ] Add visual stress meter UI component (color-coded: yellow/orange/red)
  - [ ] Implement 100% stress = self-dismissal behavior

### UI Components
- [ ] **2. Interrogation Screen UI**
  - [ ] Create main interrogation screen layout
  - [ ] Implement 2x2 button grid for main actions
  - [ ] Add character portrait display
  - [ ] Add stress meter display at top
  - [ ] Create dialogue text display area
  - [ ] Style buttons with risk indicators (Safe, Risk, High Risk)

- [ ] **3. Button Actions**
  - [ ] Implement "Alibi" button (no stress - always safe)
  - [ ] Implement "Relationship" button (no stress - always safe)
  - [ ] Implement "Doubt" button with optional evidence (stress only if wrong)
  - [ ] Implement "Accuse" button with required evidence (100% stress if wrong)
  - [ ] Implement "Dismiss" button (wide, bottom position)

- [ ] **4. Confirmation Dialog**
  - [ ] Create dismiss confirmation popup
  - [ ] Add "Are you sure?" message with context
  - [ ] Implement Yes/No buttons
  - [ ] Handle confirmation result (proceed or cancel)

### Evidence System Integration
- [ ] **5. Evidence Window**
  - [ ] Create evidence selection UI/popup
  - [ ] Display player's collected evidence items
  - [ ] Implement evidence selection for Doubt option (optional)
  - [ ] Implement evidence selection for Accuse option (required)
  - [ ] Show evidence name, description, and icon

- [ ] **6. Evidence Logic**
  - [ ] Check evidence relevance to suspect/topic
  - [ ] Calculate stress based on evidence strength
  - [ ] Implement correct vs. wrong evidence responses
  - [ ] Track evidence usage per suspect

### Character Response System
- [ ] **7. Response Data Structure**
  - [ ] Extend character YAML to include interrogation responses
  - [ ] Add alibi, relationship, doubt, accuse response text
  - [ ] Add stress resistance values per character
  - [ ] Define evidence requirements for each response

- [ ] **8. Response Logic**
  - [ ] Implement response selection based on action and evidence
  - [ ] Determine if doubt/accusation is correct or wrong
  - [ ] Apply stress only for wrong doubts/accusations
  - [ ] Implement confession triggers for correct accusations
  - [ ] Implement self-dismissal at 100% stress

### Interrogation Flow
- [ ] **9. Session Management**
  - [ ] Initialize interrogation session with character
  - [ ] Track current stress level during session
  - [ ] Manage dialogue history
  - [ ] Handle interrogation end conditions

- [ ] **10. State Tracking**
  - [ ] Track which questions have been asked
  - [ ] Track evidence presented
  - [ ] Prevent duplicate questions (or allow with consequences)
  - [ ] Flag characters as "interviewed" in character profiles

### End Conditions
- [ ] **11. Dismissal**
  - [ ] Implement dismiss with confirmation
  - [ ] Generate parting comments
  - [ ] Implement self-dismissal at 100% stress

- [ ] **12. Accusation Outcomes**
  - [ ] Handle correct accusation → confession
  - [ ] Handle wrong accusation → suspect leaves permanently
  - [ ] Handle strategic accusation → information trading
  - [ ] Mark suspect status (confessed, lawyer demanded, etc.)

### Character Personality System 13.
- [ ] **Note**: NO personality modifiers at this time
  - [ ] All characters follow same stress rules
  - [ ] Stress only increases on wrong doubts/accusations
  - [ ] All characters self-dismiss at 100% stress
  - [ ] **Future**: May add personality-specific dialogue responses (not stress modifiers)

### Testing & Polish
- [ ] **Testing Scenarios**
  - [ ] Test successful interrogation path (alibi → doubt → accuse → confession)
  - [ ] Test failed accusation (wrong evidence → suspect leaves)
  - [ ] Test strategic pressure (innocent suspect reveals info)
  - [ ] Test red herring path (follow wrong lead)
  - [ ] Test stress meter edge cases (0%, 100%)
  - [ ] Test dismiss at various stress levels

- [ ] **Polish**
  - [ ] Add sound effects for button clicks
  - [ ] Add sound effects for stress increases
  - [ ] Add visual feedback for correct/wrong evidence
  - [ ] Add animations for character reactions
  - [ ] Add particle effects for confession/breakthrough moments
  - [ ] Test all dialogue flows for typos and coherence

### Integration with Game Systems
- [ ] **Evidence Table Integration**
  - [ ] Link collected evidence to interrogation system
  - [ ] Update evidence status when used in interrogation

- [ ] **Game Progress Integration**
  - [ ] Track interrogation completion in LoungeGameProgress
  - [ ] Unlock new dialogue/areas based on confessions
  - [ ] Update case board with discovered information

- [ ] **Character Profile Integration**
  - [ ] Load interrogation data from CharacterProfile
  - [ ] Update profile flags during interrogation
  - [ ] Track confession status in profile

### Documentation
- [ ] **Code Documentation**
  - [ ] Document interrogation system classes
  - [ ] Add XML comments to public methods
  - [ ] Create usage examples

- [ ] **Design Documentation**
  - [x] Complete interrogation system design doc
  - [ ] Create dialogue writing guide for characters
  - [ ] Document evidence creation workflow

---

## Priority Order

**Phase 1: Core Foundation**
1. Stress Meter System
2. Basic UI Layout (buttons, portrait, stress display)
3. Character response data structure

**Phase 2: Basic Functionality**
4. Implement 5 button actions
5. Evidence window/selection
6. Response logic (with/without evidence)

**Phase 3: Advanced Features**
7. Confirmation dialogs
8. End conditions (dismiss, accuse outcomes, self-dismissal at 100%)
9. Correct/wrong logic for doubts and accusations

**Phase 4: Integration & Polish**
10. Integrate with character profiles
11. Testing all scenarios
12. Polish (sounds, animations, effects)

---

**Status**: Design Complete - Ready for Implementation
