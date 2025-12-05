# Repository Guidelines

## Project Structure & Modules
- `agents_vivian.py`: main multi-agent pipeline that orchestrates manager/sub-agent calls to generate Vivian functional specs.
- `openai_agents.py`, `openai_agents_stream.py`: minimal examples for sync and streamed agent runs.
- `main.py`: sample entrypoint calling `agents_vivian`.
- `constants/agent_instructions.py`: loads prompt instructions from `docs/`.
- `model/`: Pydantic models describing JSON schemas for Interaction/Visualization elements, States, and Transitions.
- `docs/`: Vivian domain documentation; keep it source of truth for schema and semantics.

## Setup, Build, and Run
- Use Python 3.10+ and a virtualenv: `python -m venv .venv && .\.venv\Scripts\activate`.
- Install runtime deps (none are pinned here): `pip install -U openai pydantic`.
- Set `OPENAI_API_KEY` (and any Azure keys if applicable) in your environment before running agents.
- Run the Vivian demo: `python main.py` (streams manager + sub-agent calls).
- Run story demo: `python -c "from openai_agents import my_agent_fun; my_agent_fun()"`.
- Streamed demo: `python -c "import asyncio; from openai_agents_stream import agent_stream; asyncio.run(agent_stream())"`.

## Coding Style & Naming
- Python: 4-space indentation, snake_case for functions/vars, PascalCase for classes, UPPER_SNAKE for constants.
- Prefer type hints and Pydantic models for structured outputs; keep JSON-facing fields stable and documented.
- Keep prompts/instructions in `constants/` or `docs/`; avoid hardcoding large instruction strings inline.

## Testing Guidelines
- No automated tests exist yet; add `pytest` tests under `tests/` when changing behavior or schemas.
- Name tests `test_<module>.py` and cover both happy-path and validation errors for models and agent wiring.

## Commit & Pull Request Practices
- Use imperative, concise commit subjects (e.g., `Add streamed agent demo logging`); wrap body at ~72 chars when needed.
- PRs should summarize changes, list key commands/run results, and link issues/tasks.
- Include before/after notes or sample outputs for agent-facing changes, and mention any new env vars or secrets required.

## Agent-Specific Tips
- Manager and specialist agents rely on `docs/*LLMFriendly` for schema rules—update docs first, then prompts.
- Keep tool names descriptive and stable (`*_JSON_generator`) so downstream orchestration remains compatible.
