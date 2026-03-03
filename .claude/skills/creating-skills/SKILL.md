---
name: creating-skills
description: Guidance for authoring Agent Skills — the folder structure, SKILL.md format, front-matter fields, body content, and quality checks needed to create well-formed skills.
---

# Creating Skills

Agent Skills are folders of instructions, scripts, and resources that Copilot can load when relevant to perform specialised tasks. Skills follow an [open standard](https://agentskills.io/) and work across multiple agents including GitHub Copilot in VS Code, GitHub Copilot CLI, and GitHub Copilot coding agent.

## Skill Locations

| Type | Paths searched |
|------|---------------|
| **Project skills** (stored in the repo) | `.github/skills/`, `.claude/skills/`, `.agents/skills/` |
| **Personal skills** (stored in user profile) | `~/.copilot/skills/`, `~/.claude/skills/`, `~/.agents/skills/` |

Additional search locations can be configured via the `chat.agentSkillsLocations` setting.

## Folder Structure

- Each skill lives in its own directory containing a `SKILL.md` file
- The directory name **must match** the `name` field in the SKILL.md front matter
- Use lowercase kebab-case for directory names (e.g., `webapp-testing`, `csharp`, `api-design`)
- Optionally include scripts, examples, templates, or other resources alongside `SKILL.md`

Example layout:

```
.github/skills/
└── webapp-testing/
    ├── SKILL.md
    ├── test-template.js
    └── examples/
        └── login-test.js
```

## SKILL.md Front Matter (required)

Every `SKILL.md` must start with YAML front matter. The following fields are available:

| Field | Required | Description |
|-------|----------|-------------|
| `name` | **Yes** | Unique identifier, lowercase with hyphens. Must match the parent directory name. Max 64 characters. |
| `description` | **Yes** | What the skill does and when to use it. Be specific about capabilities and use cases so Copilot can decide when to load it. Max 1024 characters. |
| `argument-hint` | No | Hint text shown in the chat input when the skill is invoked as a slash command (e.g., `[test file] [options]`). |
| `user-invokable` | No | Whether the skill appears as a `/` slash command. Defaults to `true`. Set to `false` to hide it from the menu while still allowing automatic loading. |
| `disable-model-invocation` | No | Whether to prevent the agent from loading the skill automatically. Defaults to `false`. Set to `true` to require manual `/` invocation only. |

Example:

```yaml
---
name: webapp-testing
description: Guidelines and scripts for testing web applications using Playwright, including component and end-to-end test patterns.
argument-hint: "[test file] [options]"
---
```

### Invocation matrix

| `user-invokable` | `disable-model-invocation` | Via `/` menu | Auto-loaded | Use case |
|---|---|---|---|---|
| _(default)_ | _(default)_ | Yes | Yes | General-purpose skills |
| `false` | _(default)_ | No | Yes | Background knowledge the model loads when relevant |
| _(default)_ | `true` | Yes | No | On-demand only |
| `false` | `true` | No | No | Effectively disabled |

## SKILL.md Body

The body contains the instructions, guidelines, and examples Copilot follows when using the skill. Write clear, specific content that describes:

- What the skill helps accomplish
- When to use the skill
- Step-by-step procedures to follow
- Examples of expected input and output
- References to included scripts or resources (use relative paths, e.g., `[test script](./test-template.js)`)

### Content guidelines

- Start with a top-level `#` heading that names the skill clearly
- Organise guidance into logical `##` sections
- Use bullet points for individual rules or preferences — keep each point concise
- Bold key terms for scannability (e.g., **PascalCase**, **readonly**)
- Use inline code for syntax, keywords, types, and short examples
- Use fenced code blocks only for multi-line examples that genuinely aid understanding
- Avoid lengthy prose — skills should be scannable reference material, not tutorials

### Tone & style

- Write rules as direct imperatives: "Use X", "Prefer Y over Z", "Avoid W"
- State what **to do**, not just what to avoid
- Where a rule has nuance, add a brief rationale after an em dash (e.g., "Never use `.Result` — it risks deadlocks")
- Keep each bullet to one or two sentences maximum

## How Copilot Uses Skills (progressive disclosure)

Skills use a three-level loading system so many skills can be installed without consuming context:

1. **Discovery** — Copilot reads the `name` and `description` from front matter to decide relevance (always loaded, lightweight).
2. **Instructions loading** — When a request matches, Copilot loads the `SKILL.md` body into context. Direct `/` invocation also triggers this.
3. **Resource access** — Additional files in the skill directory (scripts, examples, docs) are accessed only when Copilot references them.

## Scope

- A skill should cover **one cohesive topic** (a language, a framework, a workflow)
- If a skill grows too large, split it into separate skills (e.g., `csharp` and `csharp-testing`)

## Quality Checklist

Before finalising a skill:

- [ ] `SKILL.md` file exists in a dedicated directory
- [ ] Front matter has both `name` and `description`
- [ ] `name` matches the parent directory name (lowercase, hyphenated)
- [ ] `description` clearly states capabilities **and** when to use the skill
- [ ] Body starts with a `#` heading
- [ ] Rules are actionable and specific, not vague ("write clean code")
- [ ] No contradictions between bullets
- [ ] No duplication of guidance already in another skill
- [ ] Relative paths to any companion resources are correct
