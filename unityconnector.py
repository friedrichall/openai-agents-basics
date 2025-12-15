#!/usr/bin/env python3
"""Unity entrypoint that reuses the existing Vivian agent pipeline."""

import asyncio
import json
import os
import sys
from pathlib import Path
from typing import Dict, Tuple, Optional, Any, List

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


def _parse_argv(argv: list[str]) -> Tuple[str, str, Dict[str, str], Optional[str]]:
    """
    Parse CLI args as:
        argv[0]: group name
        argv[1]: description
        argv[2]: scene JSON path (required)
        argv[3:]: selected object names (types inferred by agent; flat list)
    """
    group = argv[0] if len(argv) >= 1 else "GeneratedGroup"
    description = argv[1] if len(argv) >= 2 else ""
    scene_json = argv[2] if len(argv) >= 3 else None
    names: list[str] = argv[3:] if len(argv) > 3 else []

    objects = {name: "" for name in names}
    return group, description, objects, scene_json


def _safe_vec(value: Any, length: int = 3) -> List[float]:
    """Normalize vector-like values (list/dict) into a fixed-length list."""
    if isinstance(value, dict):
        # Preserve order x, y, z (or padding)
        ordered = [value.get(k, 0.0) for k in ("x", "y", "z")][:length]
        return ordered + [0.0] * (length - len(ordered))
    if isinstance(value, (list, tuple)):
        vals = list(value)[:length]
        return vals + [0.0] * (length - len(vals))
    return [0.0] * length


def _map_exported_object(obj: Dict[str, Any]) -> Dict[str, Any]:
    """Convert a raw exported object dict into a normalized internal shape."""
    transform = obj.get("transform", {}) or {}
    mesh = obj.get("mesh") or None
    materials = obj.get("materials") or None
    children_raw = obj.get("children") or []

    mapped = {
        "name": obj.get("name") or obj.get("Name") or "UnnamedObject",
        "transform": {
            "position": _safe_vec(transform.get("position")),
            # Rotations may include w; keep up to 4 components.
            "rotation": _safe_vec(transform.get("rotation"), length=4),
            "scale": _safe_vec(transform.get("scale"), length=3),
        },
        # Mesh triangles index into vertices; normals/uvs may be empty.
        "mesh": {
            "vertices": mesh.get("vertices", []) if mesh else [],
            "triangles": mesh.get("triangles", []) if mesh else [],
            "uvs": mesh.get("uvs", []) if mesh else [],
            "normals": mesh.get("normals", []) if mesh else [],
        } if mesh else None,
        "materials": materials or [],
        "children": [],
    }

    # Recursively map children using provided world transforms (already world-space per export).
    mapped["children"] = [_map_exported_object(child) for child in children_raw if isinstance(child, dict)]
    return mapped


def _load_scene_json(scene_path: Path) -> Dict[str, Any]:
    """Load and validate the scene JSON export."""
    if not scene_path.exists():
        raise FileNotFoundError(f"Scene JSON not found: {scene_path}")
    try:
        data = json.loads(scene_path.read_text(encoding="utf-8"))
    except Exception as exc:
        raise ValueError(f"Failed to parse scene JSON: {scene_path} ({exc})") from exc

    if not isinstance(data, dict):
        raise ValueError("Malformed scene JSON: root must be an object.")

    exported = data.get("objects")
    if not isinstance(exported, list):
        raise ValueError("Malformed scene JSON: missing 'objects' array.")

    mapped_objects = [_map_exported_object(obj) for obj in exported if isinstance(obj, dict)]
    return {
        "source": str(scene_path),
        "groupName": data.get("groupName") or "",
        "description": data.get("description") or "",
        "objects": mapped_objects,
    }


def _summarize_scene(scene: Dict[str, Any]) -> str:
    """Generate a brief textual summary for agent context."""
    objects = scene.get("objects", [])
    lines = [f"Scene JSON: {scene.get('source', '(unknown)')}"]
    lines.append(f"Exported objects: {len(objects)}")
    for obj in objects[:5]:
        mesh = obj.get("mesh")
        tri_count = len(mesh.get("triangles", [])) // 3 if mesh else 0
        lines.append(
            f"- {obj.get('name')} (tris: {tri_count}, children: {len(obj.get('children', []))})"
        )
    if len(objects) > 5:
        lines.append(f"...and {len(objects) - 5} more")
    return "\n".join(lines)

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

    group, description, object_interactions, scene_json = _parse_argv(sys.argv[1:])
    if not description:
        print("No description provided. Please pass at least a short scene description.")
        sys.exit(1)
    if not scene_json:
        print("No scene JSON path provided. Please pass a path to the exported scene JSON.", file=sys.stderr)
        sys.exit(1)

    print("Unity -> Vivian Agent Connector")
    print("______________________________")
    print("Group:", group or "(empty)")
    print("Description:", description or "(empty)")
    print("Scene JSON path:", scene_json or "(none)")
    scene_path = Path(scene_json).expanduser() if scene_json else None
    try:
        scene_data = _load_scene_json(scene_path)
    except Exception as exc:
        print(f"Failed to load scene JSON: {exc}", file=sys.stderr)
        sys.exit(1)

    for name in object_interactions.keys():
        print(f"{name} (type inferred by agent)")

    description_with_scene = f"{description}\n\n{_summarize_scene(scene_data)}"

    user_prompt = build_vivian_prompt(description_with_scene, object_interactions)
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
