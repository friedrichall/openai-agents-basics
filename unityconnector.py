#!/usr/bin/env python3
"""Unity entrypoint that reuses the existing Vivian agent pipeline."""

import asyncio
import os
import sys
from pathlib import Path
from typing import Dict, Tuple

DEFAULT_OUTPUT_ROOT = Path("generated_specs")


def _prepare_console() -> None:
    try:
        sys.stdout.reconfigure(errors="replace")
        sys.stderr.reconfigure(errors="replace")
    except Exception:
        pass


def _ensure_sys_path() -> None:
    """Add project root and local venv site-packages so Unity can import dependencies."""
    here = Path(__file__).resolve().parent
    candidates = [
        here,
        here.parent,
        Path(os.getenv("VIVIAN_VENV", "")).expanduser() / "Lib" / "site-packages",
        here / ".venv" / "Lib" / "site-packages",
        here / "venv" / "Lib" / "site-packages",
        here / "env" / "Lib" / "site-packages",
    ]
    for path in candidates:
        if path and path.exists():
            sys.path.insert(0, str(path))


def _parse_argv(argv: list[str]) -> Tuple[str, str, Dict[str, str]]:
    """Parse CLI with group name support. Interaction types from CLI are ignored; the agent infers them."""
    if not argv:
        return "GeneratedGroup", "", {}

    if len(argv) >= 2:
        group = argv[0] if len(argv) > 2 else "GeneratedGroup"
        description = argv[1]
        remainder = argv[2:]
    else:
        group = "GeneratedGroup"
        description = argv[0]
        remainder = argv[1:]

    # Support legacy name/type pairs but ignore the type part; keep only object names.
    names: list[str] = []
    if remainder:
        if len(remainder) % 2 == 0 and len(remainder) >= 2:
            names = remainder[::2]
        else:
            names = remainder

    objects = {name: "" for name in names}
    return group, description, objects

def _output_dirs(group: str) -> Tuple[Path, Path]:
    env_root = os.getenv("VIVIAN_OUTPUT_ROOT")
    if env_root:
        root = Path(env_root)
    else:
        unity_root = Path.cwd() / "Packages" / "vivian-example-prototypes" / "Resources"
        root = unity_root if unity_root.exists() else DEFAULT_OUTPUT_ROOT

    group_dir = root / (group or "GeneratedGroup")
    fs_dir = group_dir / "FunctionalSpecification"
    fs_dir.mkdir(parents=True, exist_ok=True)
    return group_dir, fs_dir


def main() -> None:
    _prepare_console()
    _ensure_sys_path()

    try:
        from agents_vivian import build_vivian_prompt, run_vivian  # imported late so sys.path is patched
    except ModuleNotFoundError as exc:
        print(
            "Could not import project modules (missing 'agents' dependency). "
            "Ensure Unity uses this repo's virtualenv or set VIVIAN_VENV to the venv path.",
            file=sys.stderr,
        )
        raise exc

    if not os.getenv("OPENAI_API_KEY"):
        print("Please set the OPENAI_API_KEY environment variable before running.")
        sys.exit(1)

    group, description, object_interactions = _parse_argv(sys.argv[1:])
    if not description:
        print("No description provided. Please pass at least a short scene description.")
        sys.exit(1)

    print("Unity -> Vivian Agent Connector")
    print("______________________________")
    print("Group:", group or "(empty)")
    print("Description:", description or "(empty)")
    for name in object_interactions.keys():
        print(f"{name} (type inferred by agent)")

    user_prompt = build_vivian_prompt(description, object_interactions)
    group_dir, fs_dir = _output_dirs(group)

    try:
        spec = asyncio.run(run_vivian(user_prompt, fs_dir))
    except Exception as exc:  # pragma: no cover - defensive logging
        print(f"Failed to run Vivian pipeline: {exc}", file=sys.stderr)
        sys.exit(1)

    if spec is None:
        print("No output received from Vivian agents.", file=sys.stderr)
        sys.exit(1)

    print("")
    print("OK: files generated in:", fs_dir)


if __name__ == "__main__":
    main()
