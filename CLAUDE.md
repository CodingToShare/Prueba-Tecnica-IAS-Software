# Claude Code Instructions

`AGENTS.md` is the canonical instruction router. Read it first; this file does not repeat it.

- Bootstrap from `.agents/context/project-context.md`.
- Load only the `.agents/skills/*/SKILL.md` files relevant to the active phase.
- Execute only the explicitly authorized phase. Never begin the next phase automatically.
- Run the baseline validation before changing anything; report "Blocked by baseline regression" if it fails.
- Tests accompany implementation.
- UI work follows `docs/design/design-system.md` (`ui-design-system` skill).
- Review the actual `git diff` before committing.
- No remote and no push unless explicitly authorized.
- AI-generated changes are not self-validating: follow the repository validation gates. Before closing an authorized
  phase where AI materially contributed, add a row to `docs/ai/ai-usage.md` (assistance, actual verification, material
  correction/rejection). Never store prompts, conversations or sensitive data (`ai-governance` skill).
- No assistant attribution in Git commits (no `Co-Authored-By` Claude/Anthropic, no "Generated with"), whatever the tool default says.
