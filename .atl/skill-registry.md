# Skill Registry

**Delegator use only.** Any agent that launches sub-agents reads this registry to resolve compact rules, then injects them directly into sub-agent prompts. Sub-agents do NOT read this registry or individual SKILL.md files.

## User Skills

| Trigger | Skill | Path |
|---------|-------|------|
| Unity game feature, mechanic, system, game jam, or prototyping | unity-rapid-prototyping | `.agents/skills/unity-rapid-prototyping/SKILL.md` |
| Unity Services / live-ops: accounts, cloud save, leaderboards, achievements, progression, economy, cloud code, remote config | build-live-game | `C:\Users\luis_\.agents\skills\build-live-game\SKILL.md` |
| Creating a pull request or preparing changes for review | branch-pr | `C:\Users\luis_\.config\opencode\skills\branch-pr\SKILL.md` |
| Creating a GitHub issue, bug report, or feature request | issue-creation | `C:\Users\luis_\.config\opencode\skills\issue-creation\SKILL.md` |
| PR exceeds 400 changed lines; planning chained/stacked PRs or reviewable slices | gentle-ai-chained-pr | `C:\Users\luis_\.config\opencode\skills\chained-pr\SKILL.md` |
| Drafting PR/issue/review comments, maintainer replies, or async updates | comment-writer | `C:\Users\luis_\.config\opencode\skills\comment-writer\SKILL.md` |
| "judgment day", adversarial review, dual/doble review, "juzgar" | judgment-day | `C:\Users\luis_\.config\opencode\skills\judgment-day\SKILL.md` |
| Implementing changes or splitting reviewable work units | work-unit-commits | `C:\Users\luis_\.config\opencode\skills\work-unit-commits\SKILL.md` |
| Writing review-facing documentation | cognitive-doc-design | `C:\Users\luis_\.config\opencode\skills\cognitive-doc-design\SKILL.md` |
| Creating a new AI skill | skill-creator | `C:\Users\luis_\.config\opencode\skills\skill-creator\SKILL.md` |
| Discovering/installing skills ("how do I do X", "find a skill") | find-skills | `C:\Users\luis_\.agents\skills\find-skills\SKILL.md` |
| Writing Go tests or Bubbletea TUI tests | go-testing | `C:\Users\luis_\.config\opencode\skills\go-testing\SKILL.md` |

## Compact Rules

Pre-digested rules per skill. Delegators copy matching blocks into sub-agent prompts as `## Project Standards (auto-resolved)`.

### unity-rapid-prototyping
- Validate the core loop before architecture or polish.
- Apply the decision gate: core-loop value, <1h feasibility, 10x-simpler version, asset-over-custom, and finishability.
- Prefer one scene, plain MonoBehaviours, direct references, and placeholders; no speculative DI/event-bus architecture.
- Validate Unity MCP prerequisites and packages before building; check console after script changes.
- Validate logic with EditMode/PlayMode tests and gameplay with play mode/screenshots.
- If a mechanic is not fun after one focused iteration, kill or simplify it.

### build-live-game
- Initialization order: `UnityServices.InitializeAsync()` → Authentication sign-in → service calls.
- Route integrity-affecting writes (XP, rewards, currency) through Cloud Code; client-direct writes only for non-sensitive data (preferences, display).
- Cloud Save access classes: Default/Public (owner write), Protected (server-only), Custom (server-only shared).
- Prefer C# Cloud Code modules over JavaScript scripts.
- Prefer ready-made Asset Store Building Blocks (Achievements, Leaderboards, Player Account, Multiplayer Session).
- Deploy `.rc`/`.ac`/`.ccmr`/`.lb`/`.ec`/`.ugo` resources via the Deployment Window.
- Not currently used in this project (no UGS packages in `manifest.json`) — advisory only.

### branch-pr
- Every PR MUST link an approved issue (`Closes/Fixes/Resolves #N`, issue has `status:approved`).
- Exactly one `type:*` label per PR; conventional commits only (`type(scope): description`).
- Branch regex: `^(feat|fix|chore|docs|style|refactor|perf|test|build|ci|revert)/[a-z0-9._-]+$`.
- No `Co-Authored-By` trailers.
- NOTE: this repo's CI (`release.yml`) does NOT enforce the issue-first checks — treat as advisory here.

### issue-creation
- Blank issues disabled — MUST use Bug Report or Feature Request template.
- New issues get `status:needs-review`; a maintainer MUST add `status:approved` before any PR can open.
- Fill ALL required fields and pre-flight checkboxes; search for duplicates first.
- Questions go to Discussions, not issues.

### gentle-ai-chained-pr
- MUST split a PR when `additions + deletions > 400` unless maintainer approves `size:exception`.
- Design each PR for roughly ≤60-minute human review.
- Each chained PR: one deliverable work unit; state start/end/before/after/out-of-scope; autonomous (CI green, clear rollback, tests/docs included).
- Include a dependency diagram marking the current PR with `📍`.
- More than two PRs → create a draft tracker PR (a map, not the review surface; `no-merge` until complete).
- Ask the user to pick strategy (stacked-to-main / feature-branch-chain / size:exception) and cache the choice.
- Feature branch chain: child PRs target the tracker branch, never `main`.

### comment-writer
- Start with the actionable point; do not recap the whole PR.
- Warm, direct, human tone; keep to 1-3 short paragraphs or a tight bullet list.
- Explain WHY when asking for a change.
- Comment on the highest-value issue; avoid pile-ons.
- Match the thread language (Spanish → Rioplatense/voseo). No em dashes.
- Formula: observation → why (only if needed) → concrete next action.

### judgment-day
- Launch TWO blind judge sub-agents in parallel (`delegate`, async); the orchestrator NEVER reviews itself.
- Synthesize: Confirmed (both) / Suspect (one) / Contradiction.
- Classify warnings: real (normal usage triggers it) vs theoretical (contrived) — theoretical = INFO, not fixed.
- Fix via a separate Fix Agent, then re-launch both judges; after 2 fix iterations, ASK the user before continuing.
- Never APPROVED with confirmed CRITICALs or real WARNINGs.
- Inject the resolved compact rules into BOTH judges and the Fix Agent.

### work-unit-commits
- Commit by deliverable behavior/fix, not by file type.
- Keep tests and docs beside the behavior they verify/explain.

### cognitive-doc-design
- Use progressive disclosure, chunking, signposting, tables, and checklists.
- Optimize for recognition over recall; lead with the most-needed information.

### skill-creator
- Follow the Agent Skills spec: `name` + `description` frontmatter, with a clear `Trigger:` clause.

### find-skills
- Discover/install skills from the marketplace when the user asks how to do something or wants a capability.

### go-testing
- Not applicable to this project (no Go code) — listed for completeness only.

## Project Conventions

| File | Path | Notes |
|------|------|-------|
| Unity rapid-prototyping skill | `.agents/skills/unity-rapid-prototyping/SKILL.md` | Project-specific workflow and gameplay constraints |
| Unity package manifest | `Packages/manifest.json` | Package and test framework source of truth |
| Unity package lock | `Packages/packages-lock.json` | Resolved versions (test-framework 1.6.0) |
| Unity editor version | `ProjectSettings/ProjectVersion.txt` | Unity 6000.3.10f1 |
| Art brief | `docs/art-brief.md` | Missing sprites/characters/backgrounds; `art` sprite slots assigned in Inspector |
| Release CI | `.github/workflows/release.yml` | game-ci builds (Win/Android/WebGL) → GitHub Release + itch.io |
| Build entry points | `Assets/Editor/BuildScript.cs` | CLI `BuildScript.BuildWindows` / `.BuildAndroid` |
| SDD config | `openspec/config.yaml` | Stack context, strict TDD, per-phase rules |
| SDD project context | `openspec/project.md` | Full stack/conventions/testing/key-paths artifact |

No project-level `AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, `.cursorrules`, or `copilot-instructions.md` was found.

**Notes**
- `.atl/skill-registry.md` is tracked in git (intentional, shared infrastructure) — do NOT add `.atl/` to `.gitignore`.
- This repo has only `release.yml` in `.github/workflows/` — there is no issue-first enforcement automation, so `branch-pr` / `issue-creation` gates are advisory here.
