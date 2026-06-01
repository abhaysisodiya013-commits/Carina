# AGENTS.md

## Project
Unity 2D game using Corgi Engine.

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

## Token/credit saving rules
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
- Explain the root cause before editing.
- Show changed files and summarize the diff.

## Unity/C# rules
- Keep code compatible with Unity C#.
- Preserve serialized fields, public fields, Inspector references, prefab links, and animation parameter names.
- Do not rename GameObjects, prefabs, scripts, methods, or fields unless required.
- Warn before removing components, references, or serialized data.
- After code changes, mention required Inspector/GameObject checks.

## Response style
- Be practical, direct, and simple.
- Avoid long theory unless asked.
- Save tokens, but keep answers correct and complete.
- Prefer the safest working fix over big refactors.
- When unsure, inspect relevant files first instead of guessing.