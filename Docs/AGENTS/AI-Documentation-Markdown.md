# System Documentation Rules (Agent Reference)

**Last Updated:** March 03, 2026

---

## File Structure

- Every system MUST have a `README.md` at its root.
- If docs exceed ~500–750 lines, cover multiple conceptual areas, or mix API with architecture: create a `Docs/` folder.
- When `Docs/` exists, the root `README.md` becomes an index — it must link to all pages in `Docs/`.
- Never create a root repository `README.md` if one does not already exist.

### Docs/ Layout

```
Docs/
├── Overview.md
├── Usage.md
├── API.md
├── Technical/
│   ├── Technical.md
│   └── DesignDecisions.md
```

- Each page must have a single responsibility.
- Do not mix public API docs with architectural internals.

---

## README.md Requirements

Every system `README.md` MUST:

1. Start with: `Last Updated: YYYY-MM-DD`
2. Include a short system overview, purpose, and high-level usage summary.
3. Link to all `Docs/` pages if `Docs/` exists.
4. Include navigation links to the repository root `README.md` and root docs index.

---

## Page Requirements (All Pages)

Every documentation page MUST:

1. Start with: `Last Updated: YYYY-MM-DD`
2. Use clear headings and structured sections.
3. Use consistent terminology throughout.
4. Include a navigation footer linking back to the system `README.md` and relevant sibling pages.

Minimum nav footer:

```markdown
---
## Navigation
- [← Back to System Index](../README.md)
- [Next: API →](API.md)
```

---

## Content Rules

**Public API pages** (`API.md`, `Usage.md`):
- Document purpose, when to use, basic usage example, and constraints.
- Do not document trivial implementation details.

**Technical pages** (`Docs/Technical/`):
- Explain design decisions, tradeoffs, performance considerations, known limitations.
- Include internal flow diagrams where applicable.
- Target: contributors, not general users.

---

## Cross-Linking

- Use relative paths only — never absolute.
- Link related systems and sibling pages where relevant.
- Link between Usage ↔ API ↔ Technical pages when it improves navigation.
- Do not duplicate explanations — link instead.

---

## Style

- Direct, technical phrasing only.
- Consistent terminology — no mixing of naming styles across pages.
- No ambiguous, emotional, or casual language.

---

## Maintenance

Update documentation whenever:
- Public API changes
- Behavior changes
- Architectural decisions change
- Major refactors occur

Documentation is mandatory, not optional. Treat it as part of the system.
