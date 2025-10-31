# Common Mistakes & Pitfalls

This document tracks common mistakes and pitfalls encountered during development to prevent them from happening again.

## SpriteFont Character Issues

### Problem: Illegal Characters in UI Text

**Error Message:**
```
System.ArgumentException: Text contains characters that cannot be resolved by this SpriteFont.
```

**Root Cause:**
The SpriteFont used in the game does not include all Unicode characters. When displaying text in UI (interaction prompts, descriptions, dialogue, etc.), using unsupported characters will crash the game.

**Common Illegal Characters:**
- Arrow symbols: `→`, `←`, `↑`, `↓`
- Special quotes: `"`, `"`, `'`, `'`
- Em dash: `—`
- En dash: `–`
- Bullet points: `•`, `◦`
- Ellipsis: `…` (use three periods `...` instead)
- Math symbols: `≠`, `≤`, `≥`, `×`, `÷`
- Accented characters (may not be supported depending on font)

**Solution:**
Always use ASCII-safe alternatives:
- Instead of `→`, use `->` or `>`
- Instead of `"text"`, use `"text"`
- Instead of `—`, use `--` or `-`
- Instead of `…`, use `...`

**Where to Check:**
- Dialogue text in `Content/Data/Lounge/dialogue.md`
- Evidence descriptions in scene initialization
- UI prompts and descriptions
- Character interaction text
- Any string that will be displayed with `SpriteBatch.DrawString()`

**Prevention:**
Before adding any display text, verify that all characters are in the standard ASCII range (avoid fancy Unicode symbols).

---

## Physics System Issues

### Problem: Missing Physics Colliders for Interactable Objects

**Symptoms:**
- Objects are visible but not interactable
- Raycasts don't detect objects
- Hover highlights don't appear

**Root Cause:**
Interactable objects need BOTH:
1. Registration with `InteractionSystem.RegisterInteractable()`
2. Physics collider via `physicsSystem.Simulation.Statics.Add()`
3. `SetStaticHandle()` called to link physics handle to object
4. **Type registration in `InteractionSystem.FindInteractableByStaticHandle()`**

**Solution:**
Follow the pattern established in `AutopsyReport` and `SuspectsFile`:
- Create `StaticHandle` with physics collider
- Store handle in object via `SetStaticHandle()`
- Add type check to `InteractionSystem.FindInteractableByStaticHandle()`

---

## Pattern Best Practices

### Factory Pattern for Evidence Items

**Do:**
- Use `EvidenceDocumentFactory` for creating evidence documents
- Store visual scale in the evidence object
- Let the factory handle all physics/visual/registration setup

**Don't:**
- Hard-code visual scales in scene files
- Manually create physics colliders for each item
- Repeat the same creation code multiple times

**Benefits:**
- Single source of truth for creation logic
- Easier to maintain and debug
- Consistent behavior across all items
- Less code duplication

---

## Last Updated
2025-10-31
