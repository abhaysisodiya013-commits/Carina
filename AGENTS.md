# AGENTS.md

## Project
Unity 2D game using Corgi Engine.

## Required project memory
- For simple one-off tasks, you do not need to read `.codex/CARINA_MEMORY.md`.
- For non-trivial Carina work, read or reference `.codex/CARINA_MEMORY.md` before acting.
- For deep or risky work, read the relevant sections carefully before planning changes.
- Treat `.codex/CARINA_MEMORY.md` as the inherited Antigravity project memory and architecture map.
- Keep that memory safe. Do not delete, overwrite, or shrink it unless the user explicitly asks.
- The memory is historical context, not live truth. Verify current code, Inspector values, prefab links, scene hierarchy, animator parameters, and save state with code search or Unity MCP before acting.

## Allowed scope
Work only in:
- Assets
- Packages
- ProjectSettings

Ignore these folders unless explicitly asked:
- Library
- Temp
- Logs
- obj
- bin
- .vs
- UserSettings
- Build
- Builds

## File inspection rules
- Do not search or read large Unity YAML files like `.prefab`, `.unity`, `.asset`, `.anim`, `.controller`, or `.meta` unless necessary.
- Inspect C# scripts and small config files first.
- Avoid broad scans of the whole project.
- If prefab/scene/animator data is needed, inspect only the specific relevant files.
- Before reading huge Unity YAML files, explain why it is necessary and what exact file will be inspected.
- If the issue depends on Inspector setup, GameObject references, prefab links, scene hierarchy, Animator parameters, or Corgi components, you may inspect targeted prefab/scene/controller files instead of guessing.

## Core rules
- Make minimal safe changes.
- Fix only the requested issue.
- Do not rewrite whole systems unless explicitly asked.
- Follow existing Corgi Engine patterns/components.
- Do not create duplicate player, camera, enemy, input, UI, or manager systems.
- Do not change unrelated gameplay logic.
- **NEVER use `git checkout`, `git restore`, `git reset`, or any destructive revert command on `.unity`, `.prefab`, `.asset`, `.anim`, `.controller`, `.meta`, or other Unity serialized files unless the user explicitly asks for that exact revert and confirms they are okay losing uncommitted Inspector/scene work.** Unity scene and prefab changes are often uncommitted user work; preserve them first by copying the file to a timestamped backup and inspect/recover with Unity MCP or targeted diffs instead.
- When the user asks for an implementation or fix, do the file/code/scene edits directly. Do not tell the user to copy-paste scripts or manually create files unless tool access is blocked or the user explicitly asks for instructions only.
- Explain the root cause before editing.
- Show changed files and summarize the diff.

## Unity/C# rules
- Keep code compatible with Unity C#.
- Preserve serialized fields, public fields, Inspector references, prefab links, and animation parameter names.
- Do not rename GameObjects, prefabs, scripts, methods, or fields unless required.
- Warn before removing components, references, or serialized data.
- After code changes, mention required Inspector/GameObject checks.
- **NEVER guess the script type of a user-provided prefab** (e.g. `ProjectilePrefab`). Always inspect the prefab's components via MCP tools or `Select-String`/`grep` before writing `GetComponent<T>()` logic, otherwise your code may silently fail by returning null.

## Response style
- Be practical, direct, and simple.
- Avoid long theory unless asked.
- Save tokens, but keep answers correct and complete.
- Prefer the safest working fix over big refactors.
- **NEVER GUESS.** If you are unsure about a component, prefab, or scene setup, you MUST use the appropriate MCP tools (`assets-get-data`, `gameobject-component-get`, etc.) or IDE search tools to inspect it first. Ignoring MCP tools leads to broken code.

---

## Understanding model quota

Every AI response = 1 model turn = 1 unit from the weekly quota limit.
- Answering the user's message = 1 turn
- Processing each tool call result = 1 turn
- So a task with 5 tool calls = ~6 turns consumed from the weekly limit

**The #1 quota waster is getting the wrong answer and redoing it.** A wrong fix that takes 6 turns to attempt + 6 turns to redo = 12 turns. Getting it right the first time = 6 turns. That's a 2x difference. Accuracy saves more quota than any other optimization.

---

## Four tool layers (cheapest first)

The agent has four layers of tools. **Always use the cheapest layer that can solve the task.**

### Layer 1 — Internal reasoning (0 quota cost)
The AI thinking before calling any tool. This is free — use it on EVERY task.
- Understand the prompt
- Classify the task type
- Identify which files/components are involved
- Plan the approach
- Decide which tool layer is needed

### Layer 2 — IDE file tools (1 turn per call, no MCP)
Built-in tools: `view_file`, `write_to_file`, `grep_search`, `list_dir`, `run_command`
- **Use for**: reading/writing C# scripts, searching code, answering questions about code
- **Cost**: 1 turn per call, but multiple can run in parallel (same turn)
- **When to use**: any task that only involves C# code logic and doesn't need Unity Editor state

### Layer 3 — MCP tools via Skills (1 turn per call)
Unity MCP tools: `gameobject-component-get`, `animator-get-data`, `assets-prefab-open`, etc.
- **Use for**: anything that touches the Unity Editor — Inspector values, prefabs, scenes, animators, console logs, playmode
- **Cost**: 1 turn per call, parallel calls possible
- **When to use**: when the task involves Unity Editor state that can't be read from files
- **Skills**: each MCP tool has a SKILL.md with parameters. The skill descriptions are auto-loaded (free). Only read the full SKILL.md if you're unsure about a tool's exact parameters (costs 1 turn).

### Layer 4 — Sequential thinking MCP (1-2 turns)
Structured reasoning tool for complex/uncertain tasks.
- **Use for**: tasks with 5+ tool calls where you're genuinely uncertain about the approach
- **Cost**: 1-2 turns (call it once with all context, never iterate 5+ times)
- **When to use**: only when a wrong attempt would cost more turns to redo than the thinking call itself
- **When NOT to use**: simple fixes, single component changes, questions, anything under 5 tool calls

---

## Decision flowchart — which layer for which prompt

```
User prompt arrives
    │
    ├─ Is it a question / explanation?
    │   └─ YES → Layer 1 (think) + Layer 2 (read code if needed) → Answer. Done.
    │
    ├─ Is it a C# code-only fix?
    │   └─ YES → Layer 2 only. view_file → write_to_file. No MCP needed.
    │
    ├─ Does it need Unity Editor state?
    │   │   (Inspector values, prefab, scene, animator, console, playmode)
    │   └─ YES → Layer 3 (MCP tools). Use the tool picker table below.
    │           │
    │           ├─ Is it a simple read/modify? (1-2 MCP calls)
    │           │   └─ YES → Just do it. No sequential thinking needed.
    │           │
    │           └─ Is it complex? (5+ calls, uncertain approach)
    │               └─ YES → Layer 4 first (sequential thinking), then Layer 3.
    │
    └─ Is the prompt ambiguous?
        └─ YES → ASK the user. Don't guess. Don't call any tools.
```

---

## General workflow (every task)

### Step 1 — Understand the request
- Read the full prompt carefully. What is the user actually asking for?
- Classify: **bug fix** / **new feature** / **tweak existing** / **setup/config** / **question** / **debug**
- If the request is ambiguous or underspecified → **ASK before acting.**
  - "Change the speed" → speed of what? Movement? Projectile? Animation? **Ask.**
  - "Fix the enemy" → which enemy? What's broken? Script? AI? Health? **Ask.**
  - "It's not working" → what's "it"? What did they expect? What happens instead? **Ask.**
- Wrong guess = redo = wasted quota. Asking is always cheaper.

### Step 2 — Investigate before acting
- **Never jump straight to editing.** Understand the current state first.
- For bug fixes: read the relevant script, check console errors, inspect the component/prefab involved.
- For new features: check if Corgi Engine already provides it (abilities, AI, managers). Corgi has `CharacterHorizontalMovement`, `CharacterJump`, `Health`, `DamageOnTouch`, `AIBrain`, `AIDecision`, `AIAction`, and many more. **Don't reinvent what exists.**
- For tweaks: read the current value/code before changing it.
- For questions: read the relevant code, answer directly.
- Identify the exact files, components, and GameObjects involved before making any changes.
- **Choose the tool layer**: use the decision flowchart above. Pick the cheapest layer that can solve the task.

### Step 3 — Plan the approach
- Decide: is this a **code change**, an **Inspector/component change**, a **prefab change**, an **animation change**, or a **scene change**?
- Pick the safest approach — prefer modifying an existing system over creating a new one.
- For complex tasks (5+ changes across multiple files/components): use sequential thinking MCP to plan.
- Estimate the number of tool calls needed. Can any run in parallel? (parallel = same turn = 1 quota instead of 2)

### Step 4 — Execute with minimal changes
- Follow the plan. Make only the changes needed.
- If something unexpected comes up during execution → stop, reassess, explain to the user.
- Don't fix "bonus" things the user didn't ask for.
- Don't refactor unless explicitly asked.
- **Batch parallel calls**: if two tools don't depend on each other, call them in the same turn.

### Step 5 — Verify and report
- **For script changes**: compilation happens automatically if using `script-update-or-create`. Check `console-get-logs` only if you expect errors.
- **For component changes**: mention what the user should verify in the Inspector.
- **For prefab changes**: confirm save was successful.
- **For bug fixes**: explain the root cause and why the fix works.
- Show changed files, summarize the diff, and list any Inspector checks needed.
- **Skip verification for safe changes** (adding a component, changing a field). Only verify risky/destructive changes.

---

## Handling specific task types

### Bug fixes
1. Reproduce understanding: what's broken, what should happen instead?
2. Read the relevant script (Layer 2) / inspect the component (Layer 3).
3. Identify root cause. **Explain it before editing.**
4. Make the minimal fix. Don't refactor.
5. Mention what to test after.

### New features
1. Check if Corgi Engine already has it. Don't duplicate existing systems.
2. Follow existing code patterns in the project.
3. Create new scripts only when necessary — prefer extending existing components.
4. Wire up references and explain required Inspector setup.

### Component/Inspector changes
1. Read the component first with Layer 3 (`gameobject-component-get`) — never guess field names.
2. Make the change with `gameobject-component-modify`.
3. Tell the user what was changed and where to verify.

### Debugging "it's not working"
1. Ask for specifics if the prompt is vague.
2. Check console logs for errors (Layer 3: `console-get-logs`).
3. Inspect the relevant GameObject/component (Layer 3: `gameobject-component-get`).
4. Check if the issue is code, Inspector values, missing references, or scene setup.
5. Explain the root cause, then fix.

### Animation / Animator work
1. Always read the clip/controller first with Layer 3 (`animation-get-data` / `animator-get-data`) to discover bindings, state names, parameter names.
2. Never guess animation parameter names — they must match exactly.
3. Preserve existing animation events and transitions unless explicitly asked to change them.

---

## Sequential thinking workflow (Corgi Engine / Unity)

### When to use it
Call `sequential-thinking` MCP **only when ALL three are true:**
1. Task involves 5+ tool calls (complex setup touching multiple components/prefabs/scripts)
2. You are genuinely uncertain about the correct approach
3. A wrong attempt would cost more quota to redo than the thinking call itself

**Worth it for:**
- Setting up a new enemy from scratch (prefab + AIBrain + abilities + animation)
- Debugging a runtime issue with unknown root cause
- Refactoring a system that touches multiple scripts and components

**Not worth it for (use internal thinking instead):**
- Changing a component value (2 calls)
- Adding a component (1-2 calls)
- Fixing a script bug (0 MCP calls)
- Checking animator states (1 call)

### How to use it (our workflow)
When you call it, feed it ALL context in a single thought structured like this:

```
1. User request (exact words): ...
2. Task type: script / component / prefab / animation / scene / debug
3. Corgi components involved: [name them exactly]
4. Where the fix lives: C# code / Inspector / prefab asset / scene object / animator
5. Tool layer: Layer 2 (IDE) / Layer 3 (MCP) / both
6. Tool plan (in order): [list exact tools with layer]
7. Which calls can be parallel: [list them]
8. Risks: serialized refs broken? / components removed? / Inspector checks needed?
9. Minimum turns to complete: [number]
```

Get the full plan in **1-2 calls maximum**, then stop thinking and execute.
**Never call it 5+ times iteratively** — that wastes more quota than just executing.

---

## Tool picker — right tool for every prompt

### Scripts
| Intent | Tool | Layer | Notes |
|--------|------|-------|-------|
| Read a .cs file | `view_file` | 2 | Cheapest option |
| Read with Roslyn context | `script-read` | 3 | Only if you need compilation context |
| Write / fix / create .cs | `script-update-or-create` | 3 | Roslyn-validated, auto-recompiles |
| Quick write (no validation) | `write_to_file` | 2 | Faster but no Roslyn check |
| Delete .cs | `script-delete` | 3 | |
| Run C# at editor-time | `script-execute` | 3 | **Escape hatch only** — use dedicated tools first |

### GameObjects
| Intent | Tool | Layer | Notes |
|--------|------|-------|-------|
| Find / inspect | `gameobject-find` | 3 | Use `paths` param for partial data |
| Create | `gameobject-create` | 3 | No read needed first |
| Change name/tag/layer/transform | `gameobject-modify` | 3 | Not for component fields |
| Delete | `gameobject-destroy` | 3 | Warn user first |
| Reparent | `gameobject-set-parent` | 3 | |
| Clone | `gameobject-duplicate` | 3 | |

### Components
| Intent | Tool | Layer | Notes |
|--------|------|-------|-------|
| Read component values | `gameobject-component-get` | 3 | Use `paths` for specific fields only |
| Add component | `gameobject-component-add` | 3 | Use `gameobject-component-list-all` if type name unknown |
| Change component values | `gameobject-component-modify` | 3 | Read first if field names unknown |
| Remove component | `gameobject-component-destroy` | 3 | Warn user before calling |

### Prefabs
| Intent | Tool | Layer | Notes |
|--------|------|-------|-------|
| Edit prefab (all instances) | `assets-prefab-open` → modify → `assets-prefab-save` → `assets-prefab-close` | 3 | 4 calls minimum |
| Spawn into scene | `assets-prefab-instantiate` | 3 | |
| Create prefab from object | `assets-prefab-create` | 3 | |

### Animation & Animator
| Intent | Tool | Layer | Notes |
|--------|------|-------|-------|
| Inspect AnimationClip | `animation-get-data` | 3 | Read first to get binding names |
| Modify AnimationClip | `animation-modify` | 3 | |
| Inspect AnimatorController | `animator-get-data` | 3 | Read first to get state/param names |
| Modify AnimatorController | `animator-modify` | 3 | |

### Scenes
| Intent | Tool | Layer | Notes |
|--------|------|-------|-------|
| List opened scenes | `scene-list-opened` | 3 | |
| Get scene hierarchy | `scene-get-data` | 3 | |
| Open / save scene | `scene-open` / `scene-save` | 3 | |
| Create scene | `scene-create` | 3 | |

### Assets
| Intent | Tool | Layer | Notes |
|--------|------|-------|-------|
| Find by name or type | `assets-find` | 3 | Use `t:` type filter |
| Read asset data | `assets-get-data` | 3 | Use `paths` param |
| Modify asset | `assets-modify` | 3 | |
| Copy / move / delete | `assets-copy` / `assets-move` / `assets-delete` | 3 | |

### Debug & Editor
| Intent | Tool | Layer | Notes |
|--------|------|-------|-------|
| Read console logs | `console-get-logs` | 3 | Filter by type/time |
| Clear console | `console-clear-logs` | 3 | Clear before action to isolate logs |
| Check playmode/compile state | `editor-application-get-state` | 3 | |
| Start / stop / pause game | `editor-application-set-state` | 3 | |
| Screenshot Scene View | `screenshot-scene-view` | 3 | |
| Screenshot Game View | `screenshot-game-view` | 3 | |

### Profiler
| Intent | Tool | Layer | Notes |
|--------|------|-------|-------|
| Memory snapshot | `profiler-get-memory-stats` | 3 | Call parallel with rendering stats |
| FPS / frame timing | `profiler-get-rendering-stats` | 3 | |
| Script timing / GC | `profiler-get-script-stats` | 3 | |

### Packages & Input
| Intent | Tool | Layer |
|--------|------|-------|
| List installed packages | `package-list` | 3 |
| Install / remove package | `package-add` / `package-remove` | 3 |
| Read input actions | `inputsystem-get` | 3 |
| Modify input bindings | `inputsystem-binding-set` / `inputsystem-binding-add` | 3 |

---

## Corgi Engine patterns — exact tool chains

| Task | Tools in order | Layer | Turns |
|------|---------------|-------|-------|
| Read an ability's fields | `gameobject-component-get` | 3 | 2 |
| Modify an ability setting | `gameobject-component-get` → `gameobject-component-modify` | 3 | 3 |
| Add ability (type known) | `gameobject-component-add` | 3 | 2 |
| Add ability (type unknown) | `gameobject-component-list-all` → `gameobject-component-add` | 3 | 3 |
| Debug AI Brain | `gameobject-component-get` on AIBrain + AIDecision + AIAction (parallel) | 3 | 2 |
| Check animator params | `animator-get-data` | 3 | 2 |
| Edit enemy prefab | `assets-prefab-open` → `gameobject-component-get` → `gameobject-component-modify` → `assets-prefab-save` → `assets-prefab-close` | 3 | 5-6 |
| Debug runtime issue | `console-get-logs` + `editor-application-get-state` (parallel) | 3 | 2 |
| Fix a script bug | `view_file` → `write_to_file` | 2 | 2 |
| New enemy from scratch | `sequential-thinking` → then Layer 3 chain | 4→3 | 6-8 |

---

## Tool mistakes to avoid

| Wrong | Right | Why |
|-------|-------|-----|
| Using Layer 3 for a code-only fix | Use Layer 2 (`view_file` → `write_to_file`) | MCP not needed, saves quota |
| `script-execute` to modify a component | `gameobject-component-modify` | Dedicated tool is safer and cheaper |
| `object-modify` for a component | `gameobject-component-modify` | More specific = more accurate |
| `gameobject-modify` for component fields | `gameobject-component-modify` | `gameobject-modify` is only for name/tag/layer/transform |
| Reading .prefab YAML with `view_file` | `assets-prefab-open` + MCP tools | YAML is huge and error-prone |
| `assets-get-data` for a live component | `gameobject-component-get` | Component tool gives live scene values |
| Calling `assets-refresh` after `script-update-or-create` | Skip it | `script-update-or-create` auto-refreshes |
| Guessing a Corgi component's field name | `gameobject-component-get` first | Wrong field = silent failure |
| Creating custom AI/movement/damage scripts | Use `AIBrain`, `CharacterHorizontalMovement`, `DamageOnTouch` | Corgi already has these |
| Using Layer 4 for simple tasks | Use Layer 1 (internal thinking) | Sequential thinking wastes quota on simple tasks |
| Reading SKILL.md when you know the tool params | Skip it | Skill descriptions are already auto-loaded for free |

---

## Model quota saving

- Every tool call = 1 model turn = 1 weekly quota unit. Minimize total turns per task.
- **#1 rule: Get it right the first time.** Wrong answer + redo = 2x the quota. Accuracy > speed.
- **Parallel calls** = independent tools called in the same turn = saves quota.
  - Good: `[gameobject-find + console-get-logs]` in one turn = 2 quota
  - Bad: `gameobject-find` then `console-get-logs` = 3 quota
- **Use cheapest layer first**: Layer 2 (IDE) before Layer 3 (MCP). Layer 3 before Layer 4 (seq thinking).
- **Tier 1** (script fix, question): Layer 2 only, no MCP. Target 1-2 turns.
- **Tier 2** (component change, add ability): Layer 3, 1 read + 1 modify. Target 2-4 turns.
- **Tier 3** (multi-component, debug): Layer 4 → Layer 3, plan then batch. Target 4-8 turns.
- Skip the read step when **creating** new things (nothing to read yet).
- Skip verification for **safe** changes (adding a component, changing a field value).
- Use `paths` or `viewQuery` params in MCP get tools — partial reads are cheaper.
- Prefer scene object edits over prefab edits when possible (prefab = 4 calls minimum).
- Skill descriptions are auto-loaded. Only read full SKILL.md when parameters are unclear.
- `sequential-thinking` MCP: only when a wrong attempt costs more quota than the thinking call.

---

## Safety checks (before making changes)

### Before modifying a script
- **Check callers**: `grep_search` for the function/class name to find who calls it. Don't break callers.
- **Check serialized fields**: if renaming or removing a field, it will break Inspector references and prefab links. Warn the user.
- **Check inheritance**: if the script inherits from a Corgi class (`CharacterAbility`, `AIAction`, `AIDecision`, etc.), follow the base class pattern — override the right methods, call `base.xxx()`.

### Before modifying a component
- **Prefab vs Scene?** Always decide:
  - Change affects ALL instances (enemies, pickups, etc.) → edit the **prefab** (`assets-prefab-open` flow)
  - Change affects only THIS scene instance → edit the **scene object** directly
  - If unsure → **ask the user**
- **Check for overrides**: if the scene object has prefab overrides, editing the prefab won't affect overridden values.

### After writing C# code
- If using `script-update-or-create`: it auto-compiles and reports errors. Check the result.
- If using `write_to_file`: run `console-get-logs` afterward to verify no compilation errors.
- If errors found: fix immediately in the same conversation turn. Don't leave broken code.

### Before destroying anything
- Always tell the user WHAT will be destroyed and WHAT will break (child objects, references, etc.).
- Never destroy and recreate when modify works.

---

## Project pattern matching

Before writing ANY new script, search the project for similar existing code:

### How to find existing patterns
1. `grep_search` for similar class names or base classes in `Assets/Scripts/`
2. If creating a new AI controller → check existing ones like `ShieldGoatAIController.cs`, `SlingShotGoatAIController.cs`
3. If creating a new ability → check how existing custom abilities are structured
4. If creating a new hitbox/damage script → check `RetroSkillDamageHitbox.cs`, `MeleeWeaponHitbox.cs`

### Rules
- **Match naming conventions**: if existing scripts use `XxxController.cs`, don't create `XxxManager.cs`.
- **Match code style**: if existing scripts use `[SerializeField] private`, don't use `public`.
- **Match folder structure**: put new scripts in the same folder as similar existing ones.
- **Match Corgi patterns**: if existing abilities inherit from `CharacterAbility`, new ones must too. If existing AI actions inherit from `AIAction`, follow that pattern.
- **Don't duplicate**: if a similar script already exists, modify it instead of creating a new one (unless the user explicitly asks for a new script).

---

## Self-correction protocol

### When something goes wrong mid-execution
1. **Stop immediately.** Don't push through hoping it works.
2. **Read the error.** Is it a compilation error? A null reference? A missing component?
3. **Identify the cause** before attempting a fix. Don't guess.
4. **Fix in the same conversation** if possible — don't leave broken code for the user.

### When Unity MCP tools are unavailable
1. **Stop before editing Unity-related files.** Tool access failure means Inspector/prefab/scene facts may be unavailable.
2. Check whether Unity is open with the Carina project loaded and whether the project MCP endpoint is reachable at `localhost:22436`.
3. Run `.codex/check-setup.ps1` if local shell access is available.
4. If MCP still is not visible, explain the tool-access problem first and avoid guessing Unity component, prefab, scene, animator, or Inspector state.

### When you realize you used the wrong approach
1. **Tell the user** what went wrong and why.
2. **Undo safely if possible**. For C# scripts, a targeted revert may be acceptable if the diff is known to be yours. For Unity serialized files (`.unity`, `.prefab`, `.asset`, `.anim`, `.controller`, `.meta`), do not run destructive git revert commands; first copy the current file to a timestamped backup, inspect whether the file contains user scene/Inspector work, and recover by applying a targeted patch or using Unity MCP.
3. **Propose the correct approach** before executing it.

### When MCP tool returns unexpected data
1. Don't ignore it. The data is telling you something about the actual state of the project.
2. If a component doesn't have the field you expected → you may have the wrong component. Read it again with `gameobject-component-get`.
3. If a GameObject can't be found → it may have a different name or be in a different scene. Use `gameobject-find` with a broader search.

---

## Corgi Engine architecture (know this — don't investigate it)

Understanding this saves 2-3 investigation turns per task. This is how Corgi Engine works internally:

### Core architecture
- **Character** is the central hub. Every player/enemy has a `Character` component.
- **Abilities** are separate components (`CharacterAbility` subclasses) on the same GameObject. They register with `Character` automatically.
- **CorgiController** handles physics (grounded checks, slopes, collisions). It's NOT Unity's Rigidbody.
- **Health** manages HP, damage, death, invincibility. `DamageOnTouch` triggers `Health.Damage()`.
- **InputManager** feeds input to abilities. Abilities check input in `HandleInput()`.

### AI system
- **AIBrain** is the state machine. It has states, each with `AIDecision`s (conditions) and `AIAction`s (behaviors).
- **AIDecision** subclasses: `AIDecisionDistanceToTarget`, `AIDecisionHit`, `AIDecisionTimeInState`, `AIDecisionHealth`, etc.
- **AIAction** subclasses: `AIActionPatrol`, `AIActionMoveTowardsTarget`, `AIActionShoot`, `AIActionDoNothing`, etc.
- **Brain transitions**: a state transitions when its decisions evaluate to true. The brain picks the first matching transition.

### Damage flow
```
Attacker (DamageOnTouch) → Target (Health.Damage()) → Death/Knockback/Invincibility
```
- `DamageOnTouch` requires matching `TargetLayerMask` to hit.
- `Health` fires events: `OnHit`, `OnDeath`, `OnRevive`.
- Knockback is handled by `Health` applying force to `CorgiController`.

### Animation flow
```
Ability sets animator parameter → Animator transitions → Animation plays → Animation events fire
```
- Abilities set parameters via `MMAnimatorExtensions` (e.g., `_animator.SetBool("Walking", true)`).
- Parameter names must match EXACTLY between code and Animator Controller.
- Common parameters: `Walking`, `Speed`, `Jumping`, `Idle`, `Attacking`, `Crouching`, `Grounded`.

### Key rule
**Don't create custom systems for things Corgi already handles.** Check the Corgi component list first:
- Movement → `CharacterHorizontalMovement`, `CharacterJump`, `CharacterDash`
- Combat → `CharacterHandleWeapon`, `DamageOnTouch`, `Health`, `MeleeWeapon`, `ProjectileWeapon`
- AI → `AIBrain`, `AIAction*`, `AIDecision*`
- Camera → `CinemachineCameraController` or Corgi's built-in camera

---

## Project-specific architecture (this project's patterns)

### Enemy AI controllers
This project uses a **custom GoatAIController pattern** — NOT pure Corgi AIBrain. The controllers:
- Inherit from `MonoBehaviour` (not `AIAction` or `AIDecision`)
- Cache Corgi components in `Start()`: `_brain`, `_animator`, `_characterJump`, `_horizontalMovement`, `_controller`
- Use `public` fields for Inspector tuning (speeds, distances, cooldowns, animation names)
- Drive behavior in `Update()` by reading `AIBrain` state and overriding movement/animations
- Examples: `ChaserGoatAIController`, `ShieldGoatAIController`, `SlingShotGoatAIController`, `WielderGoatAIController`

**When creating a new enemy**: follow this pattern exactly. Read an existing GoatAIController first.

### Custom abilities (Retro* pattern)
Player abilities extend `CharacterAbility`:
- `RetroAirAttackAnimationOverride` — overrides air attack animations
- `RetroRageModeAnimator` — rage mode visual effects
- `RetroSkillAnimationInput` — skill-based animation input handling

**When creating a new player ability**: inherit from `CharacterAbility`, override `Initialization()`, `HandleInput()`, `ProcessAbility()`. Call `base.xxx()`.

### Custom effects (after-image pattern)
Visual effects inherit from `MonoBehaviour`:
- `RetroDashAfterImage`, `RetroJumpGhostAfterImage`, `RetroSwordSlashAfterImage`
- Pattern: spawn sprite copies, fade them out over time

### Dialogue integration
- `DialogueBubbleSetup` — sets up dialogue bubbles on NPCs
- `DialogueCorgiAutoWalk` — auto-walks the player to dialogue targets
- Uses **Pixel Crushers Dialogue System** (`Assets/LocalPackage/Pixel Crushers/`)

### Custom AI actions
- `AIActionPatrolWithPause` extends `AIActionPatrol` — adds pause behavior between patrols

### Naming conventions
- Enemy controllers: `[Name]GoatAIController.cs`
- Player abilities: `Retro[AbilityName].cs`
- After-image effects: `Retro[Effect]AfterImage.cs`
- Editor tools: `Assets/Editor/[Name].cs`

---

## Common Unity error diagnosis

Map errors to causes **before investigating** — saves 1-3 debugging turns:

| Error | Most likely cause | Fix |
|-------|------------------|-----|
| `NullReferenceException` | Missing Inspector reference or `GetComponent` returned null | Check the Inspector for unassigned fields. Check the GameObject has the component. |
| `MissingComponentException` | Component not added to the GameObject | Add the component via Inspector or `gameobject-component-add`. |
| `InvalidOperationException` in Corgi | Calling ability methods before `Initialization()` | Ensure ability is on same GameObject as `Character`. Check `Start` vs `Initialization` timing. |
| Animation not playing | Parameter name mismatch between code and Animator | Read `animator-get-data` to get exact parameter names. They're case-sensitive. |
| `DamageOnTouch` not working | Layer mask mismatch | Check `TargetLayerMask` on `DamageOnTouch` and the target's Layer. |
| Enemy not moving | `AIBrain` stuck in wrong state, or movement speed = 0 | Check `AIBrain` current state + `CharacterHorizontalMovement.MovementSpeed`. |
| Ability not triggering | Ability not added to GameObject, or input not mapped | Check `Character` GameObject has the ability component. Check `InputManager` bindings. |
| Prefab changes not appearing | Scene object has prefab override on that field | Check for overrides in Inspector (bold = overridden). Apply or revert. |
| Script won't compile | Missing `using` directive or namespace | Check error in `console-get-logs`. Add the missing `using`. |
| `SendMessage has no receiver` | Calling method on wrong GameObject | Check the message target. The component with that method must be on the target object. |

### Diagnosis workflow (saves turns)
1. **Read the exact error first** — `console-get-logs` with error filter. Don't guess the error type.
2. **Match to table above** — if it matches, apply the fix directly. No extra investigation needed.
3. **If no match** — then investigate: read the script, check the component, inspect the Inspector.

---

## Visual debugging (when to use screenshots)

Screenshots save turns when the issue is **visual** — faster than reading 20 component fields.

### When to use screenshots
- "My character looks wrong" → `screenshot-game-view`
- "The enemy isn't positioned right" → `screenshot-scene-view`
- "The animation looks broken" → `screenshot-game-view` during playmode
- "The UI doesn't look right" → `screenshot-game-view`
- User sends a screenshot → compare with `screenshot-game-view` to verify your fix

### When NOT to use screenshots
- Code logic bugs (use `console-get-logs` + `view_file` instead)
- Component value issues (use `gameobject-component-get` instead)
- Performance problems (use profiler tools instead)

### Screenshot + data parallel trick
When debugging a visual issue, call `screenshot-game-view` AND `gameobject-component-get` **in parallel** (same turn). You get both visual context and data in 1 turn instead of 2.

---

## Cross-conversation awareness

### When to check previous conversations
- User says "continue from before" or "remember what we did" → read the conversation transcript
- User references a feature/fix from a previous session → check conversation summaries
- A bug appears in something you fixed before → the previous conversation has the context
- For Antigravity migration context, read `.codex/CARINA_MEMORY.md` first. Treat it as historical context, not current truth; verify with code and Unity MCP before acting.

### How to check
- Conversation summaries are provided at the start of each session
- Full transcripts are at `<appDataDir>\brain\<conversation-id>\.system_generated\logs\transcript.jsonl`
- Use `grep` on transcripts to find specific topics without reading the whole log

### Rules
- **Don't re-investigate** what a previous conversation already figured out. Read the transcript first.
- **Don't undo previous fixes** unless the user explicitly asks. If a previous fix seems wrong, explain and ask.
- **Build on previous work** — if a system was already set up, extend it rather than replacing it.

---

## Pre-flight checklist (before EVERY change)

Run this 5-point mental check before executing. Costs 0 quota — it's Layer 1 thinking.

```
□ 1. Am I sure WHAT to change? (If not → ASK the user)
□ 2. Am I sure WHERE it lives? (Code / Inspector / Prefab / Animator / Scene?)
□ 3. Have I checked for existing patterns? (grep for similar scripts/components)
□ 4. Will this break anything? (callers, serialized refs, prefab overrides?)
□ 5. Am I using the cheapest tool layer? (Layer 2 before 3, Layer 3 before 4)
```

If any checkbox is NO → investigate first. Don't proceed with uncertainty.

---

## Multi-file change ordering

When a task touches multiple files, **order matters** to avoid cascading compilation errors:

### Script changes (compile dependencies)
1. **Base classes / interfaces first** — if you're creating a new class that others inherit from, write it first.
2. **Dependencies before dependents** — if Script A references Script B, write B first.
3. **One script at a time** — write, let it compile, then write the next. Don't batch-write 3 scripts at once unless they have no cross-references.

### Mixed changes (script + component + prefab)
1. **Script first** — write and compile the C# code.
2. **Component second** — add/modify components that use the new script.
3. **Prefab last** — save prefab changes after components are configured.
4. **Animator if needed** — update animator parameters to match new code.

### Why this matters
Writing a script that references a type that doesn't exist yet → compilation error → wastes a turn fixing it. Following the order above prevents this.

---

## Common Corgi Engine gotchas

Things that trip up AI agents specifically:

| Gotcha | Why it fails | Prevention |
|--------|-------------|------------|
| Using `Rigidbody2D` for movement | Corgi uses `CorgiController`, NOT Unity physics | Always use `CorgiController.SetForce()` or `SetHorizontalForce()` |
| Adding ability without `Character` | Abilities need `Character` component to initialize | Check that the GameObject has `Character` component |
| Calling ability code in `Start()` | Corgi abilities initialize in `Initialization()`, which runs AFTER `Start()` | Override `Initialization()` instead of using `Start()` |
| Hardcoding animation parameter names | Parameter names vary per Animator Controller | Always read `animator-get-data` first to get exact names |
| Setting `Health` to 0 for "instant kill" | Setting `MaxHealth` to 0 breaks the system | Use `Health.Kill()` method instead |
| Using `transform.position` for movement | Bypasses `CorgiController` physics/collision | Use `CorgiController.SetForce()` or teleport via `CorgiController.SetPosition()` |
| Forgetting `TargetLayerMask` on `DamageOnTouch` | Damage silently does nothing | Always verify layers match between attacker and target |
| Creating a new `InputManager` | Corgi already has one — duplicates cause input conflicts | Use the existing `InputManager`, add new buttons to it |
| Putting AI logic in `Update()` in an `AIAction` | Corgi AIActions use `PerformAction()`, not `Update()` | Override `PerformAction()` for AI actions |
| Setting walk speed on wrong component | `CharacterHorizontalMovement` has `WalkSpeed`, not `Character` | Check which component owns the field first |

---

## Tool used

- always tell which tool you used to solve the problem and justify your answer based on the tool's response, but you dont need to show the tool response itself


## What changes made

- always tell what changes you made to the file and which file in short and simple, You dont need to explain it deeply but cover the full context of the changes.

## Inspector field explanations

- When adding or mentioning new public/serialized Inspector fields, always tell the user exactly where to find them: GameObject/component name, Inspector section/header, and field names.
- Explain what each field controls in practical terms, including value direction for offsets/sizes when relevant.
- If the field only has a visible effect during runtime or a specific state, say that clearly. Example: cooldown timer text is visible only while a skill is cooling down and `Show Skill Cooldown Timers` is enabled.

## Verify Component Attachments Before Editing
- **Never assume a script drives an action just because of its name.** (e.g. assuming `RestartScreenManager` handles the restart button).
- Before modifying a script for a UI or gameplay behavior, **trace the actual event** (like `OnClick`) or read the Prefab/GameObject to confirm which script is *actually attached*.
- Modifying an unattached script wastes time and quota. Use IDE tools (`grep` on `.prefab`) or MCP tools to verify what component is actually used by the object in question.
