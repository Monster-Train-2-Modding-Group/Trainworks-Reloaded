import argparse
import json
import re
from enum import Enum
from pathlib import Path
from typing import Dict, Optional, Set


class AnimationType(str, Enum):
    NONE = "none"
    IDLE = "idle"
    ATTACK = "attack"
    HIT_REACT = "hit_react"
    IDLE_RELENTLESS = "idle_relentless"
    SPELL = "spell"
    DEATH = "death"
    TALK = "talk"
    HOVER = "hover"


# Optional overrides for specific animations; unlisted animations use default_framerate
DEFAULT_FRAMERATE_OVERRIDES: Dict[str, float] = {

}

SUPPORTED_EXTENSIONS: Set[str] = {".png", ".jpg", ".jpeg", ".webp"}


def natural_sort_key(file_path: Path):
    """Sorts strings containing numbers in human order (e.g., frame_2 before frame_10)."""
    return [
        int(text) if text.isdigit() else text.lower()
        for text in re.split(r"(\d+)", file_path.stem)
    ]


def scan_sprite_directory(
    root_dir: Path,
    framerate_overrides: Optional[Dict[str, float]] = None,
    default_framerate: float = 12.0,
) -> dict:
    """
    Scans subfolders in root_dir matching AnimationType names and builds the JSON structure.
    """
    root = root_dir.resolve()
    if not root.is_dir():
        raise ValueError(f"Target directory does not exist: {root}")

    overrides = framerate_overrides or {}
    valid_animation_names = {item.value for item in AnimationType}

    animations_output = []
    sprites_output = []
    seen_sprite_ids = set()

    # Scan only direct subdirectories whose names match a valid animation enum
    for folder in sorted(root.iterdir()):
        if not folder.is_dir():
            continue

        folder_name = folder.name.lower().strip()
        if folder_name not in valid_animation_names:
            continue

        # Gather and sort frames
        image_files = [
            f for f in folder.iterdir()
            if f.is_file() and f.suffix.lower() in SUPPORTED_EXTENSIONS
        ]
        image_files.sort(key=natural_sort_key)

        if not image_files:
            continue

        frame_ids = []
        for img in image_files:
            sprite_id = img.stem
            frame_ids.append("@"+sprite_id)

            # Store unique sprite definition with relative path
            if sprite_id not in seen_sprite_ids:
                sprites_output.append({
                    "id": sprite_id,
                    "path": img.relative_to(root).as_posix()
                })
                seen_sprite_ids.add(sprite_id)

        framerate = overrides.get(folder_name, default_framerate)

        animations_output.append({
            "animation": folder_name,
            "frames": frame_ids,
            "framerate": float(framerate)
        })

    return {
        "game_objects": [
            {
                "id": str(root_dir),
                "type": "character_art",
                "extensions": {
                    "character_art": {
                        "sprite": "@<sprite-id-here>",
                        "animations": animations_output,
                    }
                }
            }
        ],
        "sprites": sprites_output
    }


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Generate sprite animation JSON from matching subdirectories."
    )
    parser.add_argument(
        "directory",
        type=str,
        nargs="?",
        default="./character_sprites",
        help="Path to the main folder containing animation subfolders"
    )
    parser.add_argument(
        "-o", "--output",
        type=str,
        default="sprite_config.json",
        help="Output JSON file name (default: sprite_config.json)"
    )
    parser.add_argument(
        "--fps",
        type=float,
        default=12.0,
        help="Default framerate for animations without explicit overrides"
    )

    args = parser.parse_args()
    target_path = Path(args.directory)

    data = scan_sprite_directory(
        root_dir=target_path,
        framerate_overrides=DEFAULT_FRAMERATE_OVERRIDES,
        default_framerate=args.fps
    )

    out_file = target_path / args.output if not Path(args.output).is_absolute() else Path(args.output)
    with open(out_file, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)

    print(f"Generated {out_file}.")