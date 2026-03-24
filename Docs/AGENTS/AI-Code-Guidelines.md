# C# / Unity Code Guidelines — AI Agent Reference

> Optimized for AI coding agents. Derived from the full Style and Architecture documentation in the project wiki.
> This is a condensed reference for maximum context efficiency.

---

# STYLE

## Formatting

- **4 spaces** per indent. No tabs.
- **K&R braces** — opening brace on same line as statement.
- **No cuddled** `else` / `catch` / `finally` — place on own line after `}`.
- **Single-statement `if`**: multiline body only, never one-line form. Braces optional unless any block in the chain uses braces or body spans multiple lines.
- **Multiline rule**: either entire construct on one line, or one item per line — never mixed. Applies to args, params, conditions, base lists, constraints.
- Multiline closing `)` on its own line; opening `{` on the same line as `)`.
- No more than one consecutive blank line.
- No spurious spaces inside parentheses.

```csharp
// K&R + no cuddled else
if (isReady) {
    StartRun();
}
else {
    Wait();
}

// Single-statement if — multiline form
if (source == null)
    throw new ArgumentNullException(nameof(source));

// NEVER: if (source == null) throw new ArgumentNullException(nameof(source));

// Multiline args
void ApplyDamage(
    int count,
    string source,
    float multiplier
) {
    // ...
}

// try/catch
try {
    DoWork();
}
catch (Exception ex) {
    Log(ex);
}
finally {
    Cleanup();
}
```

## Naming

| Element | Convention | Example |
|---|---|---|
| Types, namespaces, methods, properties | PascalCase | `PlayerStats`, `InitializeCombat()` |
| Interfaces | `I` prefix | `IDamageable` |
| Attributes | `Attribute` suffix | `DamageBoostAttribute` |
| Generic type params | `T` prefix, descriptive | `TItem`, `TLeft` |
| Private / protected fields | `_camelCase` | `_maxHealth` |
| Serialized fields (`[SerializeField]`) | camelCase (no `_`) | `healthBarRoot` |
| Locals / parameters | camelCase | `moveSpeed` |
| Static fields (non-static class) | `s_` prefix | `s_lookup` |
| `[ThreadStatic]` fields | `t_` prefix | `t_cachedValue` |
| Constants | UPPER_SNAKE_CASE | `DEFAULT_LIVES` |
| Non-flags enum | Singular noun | `WeaponType` |
| Flags enum | Plural noun | `MovementStates` |

- Classes **sealed by default**. Base classes use `Base` suffix (`WeaponBase`).
- Properties always PascalCase. Prefer public properties over public fields.
- `static readonly` order (not `readonly static`).
- In static classes, omit `s_` prefix for static fields.
- Avoid abbreviations unless widely accepted (`HUD`, `GUI`). Short names OK in tight scopes (loops, lambdas).
- Prefer descriptive names. `int width;` not `int x;`.
- Use Unicode escapes (`\uXXXX`) instead of literal non-ASCII characters.

## Language Usage

- Prefer `var` by default. Explicit type only when type is unclear from context.
- Use target-typed `new()` for field definitions and returns where type is already stated. Do NOT use `new()` when type is only known from earlier context.
- Use C# keywords over BCL names: `int` not `Int32`, `string` not `String`, `float` not `Single`.
- Prefer `nameof(...)` over string literals.
- Avoid `this.` unless needed for disambiguation.
- Visibility: only specify if not the default. Visibility modifier comes first.
- `using` directives at top of file, outside namespace. Sorted alphabetically, `System.*` first.
- `readonly` wherever possible.

## Data Types and Immutability

- Prefer `record` and `record struct` for data carrier types (requires C# 10 enabled & `IsExternalInit` shim).
- Prefer immutable data over mutable data.
- Prefer `readonly` members and `init`-only setters over mutable setters.
- For struct-based data carriers, use `readonly record struct` in 99% of cases.
- Use mutable structs only when there is a clear, measured performance or architectural reason.

## Member Ordering Within a Type

Follow this template. Reasonable exceptions are allowed.

1. Constants + `static readonly`
2. Static members (fields, properties, events, operators, factory methods)
3. `[SerializeField]` members
4. Instance fields (non-serialized)
5. Events
6. Properties / Indexers (public → internal/protected → private)
7. Constructors (may include factory methods nearby)
8. Unity lifecycle (`Awake`, `OnEnable`/`OnDisable`, `Start`, `Update`/`FixedUpdate`/`LateUpdate`, `OnDestroy`, collision callbacks)
9. Public methods
10. Internal / protected methods
11. Private methods
12. Interface implementations (inline if central to API; group at bottom if plumbing)
13. Nested types (bottom; public API-essential ones may go near top)

## Local Functions

- Use only when needed in a single parent method and the method stays small.
- Declare after an explicit `return` to separate main logic from helpers.
- Promote to private method if reused or growing.

## File Style Precedence

If a file already follows a different style, preserve it unless the team refactors fully.

---

# ARCHITECTURE

## Module Boundaries

- A module owns its data, logic, state transitions, and public API.
- Cross-module interaction only through public API (interfaces, events).
- No cross-module internal access. No "god manager".
- Internal structure follows MVC: Model (state + logic), View (presentation), Controller (coordination).

## Dependency Direction

- High-level code must not depend on low-level details.
- No cross-module `FindObjectOfType`. No static singletons as service locators.
- Program to interfaces, wire in composition root.

## Communication

- Direct method calls for request/response.
- Events for broadcasting state changes / decoupled reactions.

## State Ownership

- Single-writer principle: each state has exactly one mutation owner.
- Configuration → `ScriptableObject`. Runtime state → services/models.
- Prefer immutable inputs. Explicit commands over random field mutation.

## Unity Boundary Rule

- MonoBehaviours are **adapters** (wiring + callbacks + references), not brains.
- Put actual logic in pure C# classes.
- Exception: tiny, isolated components where layering adds no value.

## Async

- Always **UniTask** (if present). Never coroutines for async work, UniTask alternative is Awaitables.
- Always pass `CancellationToken`.
- Destruction cancellation: using unitys built in destroy cancellation token.
- Fire-and-forget: `UniTaskVoid` + `.Forget()`.

## Error Handling

- Validate inputs at module boundaries. Fail fast in dev.
- Prefer logging + safe continuation over throwing.
- Unity null check: use `if (!obj)` / `if (obj)`, not `== null`.
- Null-conditional on Unity objects: `obj.OrNull()?.NextCall()` (if `OrNull()` extension not present create an internal version on a per system basis, this should check for unity lifecycle as the null-conditional checks C# object lifecycle and Unity overloads that).

## Performance

- No per-frame allocations in hot paths.
- Pool frequently reused objects; pool ownership belongs to one object.

## Component References

- Avoid `GetComponent` in runtime init.
- Prefer `[SerializeField]` and OnValidate, or (if present) SceneRefAttribute: `[Self]`, `[Parent]`, `[Child]` (Requires ValidatedMonoBehaviour or `this.ValidateRefs` call in OnValidate).
- `FindObjectOfType` almost never — document if used, include rationale.

## Namespaces & Assemblies

- Namespace follows folder path from `Scripts/` (excluding `Scripts`).
  - `Scripts/Player/Combat/X.cs` → `namespace Player.Combat`
- `.asmdef` per module. Assembly name follows folder structure.
  - `Scripts/Player/Combat/Player.Combat.asmdef`
- Gameplay must never depend on UI. Game must function without UI present.

## Nullable Reference Types (NRT)

- Enable NRT per file (`#nullable enable`) or per assembly (`csc.rsp` with `-nullable:enable`).
- If NRT is not enabled, assume every reference can be null — add guards.
- Optional serialized ref: `[SerializeField] private GameObject? somePrefab;`
- Required inspector ref: `[SerializeField, Required] private GameObject requiredPrefab = null!;`
- Use `null!` sparingly — only for inspector-required fields.

## C# 10

- Enable via `csc.rsp` next to `.asmdef`: `-langversion:10`.
- Add `IsExternalInit` shim if records need it on the current toolchain:

```csharp
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices {
    internal class IsExternalInit { }
}
```

## Testing

- Test models, controllers, services. Views: visual testing unless they contain logic.
- Unit tests (granular) + PlayMode tests (integration, optional).
- Use **NSubstitute** (if present). Never mock Unity engine types.

## LINQ

- Use **ZLinq** by default (if present). Standard LINQ only if explicitly justified.

# QUICK REFERENCE — COMPLETE EXAMPLE

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Player.Combat {
    public sealed class CombatController : ValidatedMonoBehaviour {
        // 1. Constants + static readonly
        public const int MAX_COMBO = 10;
        private static readonly float s_globalCooldown = 0.5f;

        // 3. Serialized Inspector state
        [SerializeField, Required] private Transform attackOrigin = null!;
        [SerializeField] private AudioClip? hitSfx;

        // 4. Instance fields
        private readonly List<int> _hitIds = new();
        private int _currentCombo;
        private IDamageService _damageService = null!;

        // 6. Properties
        public int CurrentCombo => _currentCombo;

        // 8. Unity lifecycle
        private void Awake() {
            _currentCombo = 0;
        }

        private void OnDestroy() {
            _hitIds.Clear();
        }

        // 9. Public methods
        public void ExecuteAttack(float multiplier) {
            if (_currentCombo >= MAX_COMBO)
                return;

            var damage = CalculateDamage(multiplier);
            _damageService.Apply(damage);
            _currentCombo++;
        }

        // 11. Private methods
        private int CalculateDamage(float multiplier) {
            var baseDamage = GetBaseDamage();
            return Mathf.RoundToInt(baseDamage * multiplier);

            int GetBaseDamage() {
                return 10 + _currentCombo;
            }
        }
    }
}
```

---
