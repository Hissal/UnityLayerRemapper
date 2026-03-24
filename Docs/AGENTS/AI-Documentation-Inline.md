# Inline XML Documentation Rules (Agent Reference)

**Last Updated:** March 03, 2026

---

## Coverage Requirements

The following MUST have XML documentation:

- Public classes, structs, interfaces, enums, delegates
- Public methods, properties, fields (if intentionally exposed), events
- Internal members when: behavior is non-obvious, logic is non-trivial, or member participates in a larger internal contract

---

## Required Tags

Use all applicable tags — do not omit relevant ones:

| Tag | When |
|-----|------|
| `<summary>` | Always |
| `<param>` | When the parameter meaning is non-obvious (see below) |
| `<typeparam>` | All generic type parameters |
| `<returns>` | All non-void methods |
| `<remarks>` | When additional context is needed |
| `<exception>` | All intentionally thrown exceptions |
| `<see cref=""/>` | When referencing types, methods, properties, fields, interfaces |
| `<seealso>` | For related members or types |

---

## `<param>` Rules

**Do NOT write** a `<param>` that merely restates the parameter name:

```xml
<!-- BAD: adds no information -->
<param name="targetPosition">The position of the target.</param>
```

**DO write** `<param>` when it conveys:

- Constraints or preconditions
- Units (e.g. meters, seconds)
- Coordinate space (e.g. world, local)
- Behavioral expectations
- Side effects

If none of these apply, omit the `<param>` tag.

---

## `<exception>` Rules

MUST document all intentionally thrown exceptions:

```xml
<exception cref="ArgumentNullException">
Thrown when <paramref name="key"/> is null.
</exception>
```

- Specify the exact condition under which it is thrown.
- Only document exceptions intentionally thrown by the method.
- Do not document framework-internal exceptions unless they are part of the public contract.

---

## `<see cref=""/>` Rules

Use when referencing:

- Types: `<see cref="SceneGroup"/>`
- Methods: `<see cref="IRngService.Range{TKey}(TKey, int, int)"/>`
- Properties, fields, interfaces, generic parameters

Do not overuse — only link when it genuinely improves clarity.

---

## Summary Length

| Member | Expected depth |
|--------|---------------|
| Simple method | One-line summary |
| Complex behavior | Longer summary acceptable |
| Class / interface | Longer acceptable; explain purpose, lifecycle, relationships |
| Internal member | Single-line preferred |

- XML documentation must never be longer than the implementation file itself.
- If deeper explanation is required, reference Markdown documentation instead.

---

## Class-Level `<summary>`

MUST explain:

- Purpose
- Intended usage
- Lifecycle (if relevant)
- Relationship to other systems (use `<see cref=""/>`)

Do NOT restate the class name in sentence form.

---

## Internal Members

Preferred format — single-line summary written as a plain comment:

```xml
/// <summary>Cached hash of the serialized type for fast lookup.</summary>
```

- Keep it concise.
- Avoid extended XML docs unless absolutely required for maintainability.
- If logic is complex, prefer refactoring for clarity over documentation.

---

## Referencing Markdown Docs

When deeper explanation exceeds what is appropriate inline:

```xml
<remarks>
For a detailed explanation of the lifecycle, see the Scene Loading documentation.
</remarks>
```

- Do not duplicate Markdown documentation inline.
- Do not add Markdown references to every method — only when it genuinely helps.

---

## Style

- Tight and precise wording.
- Do not restate obvious information from the member name.
- Use the same terminology as the Markdown system documentation.
- Do not contradict external architectural documentation.

---

## Maintenance

Update XML documentation whenever:

- Public API changes
- Method behavior changes
- Exceptions change
- Architectural decisions that affect the public contract change

Inline documentation is part of the public contract. It is mandatory.
