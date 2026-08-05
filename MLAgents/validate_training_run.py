#!/usr/bin/env python3
"""Reject ML-Agents runs that never produce trustworthy episode summaries."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path


BEHAVIORS = (
    "TD3DAgent",
    "TD3DBalanceAgent",
    "TD3DEnemyLevelAgent",
)
STEP_PATTERN = re.compile(
    r"(?P<behavior>TD3D(?:Balance|EnemyLevel)?Agent)\.\s+"
    r"Step:\s+(?P<step>\d+)\."
)
SUMMARY_PATTERN = re.compile(r"Mean Reward:\s+(?P<reward>[-+]?\d+(?:\.\d+)?)")
NO_EPISODE_MARKER = "No episode was completed since last summary"


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Validate that an ML-Agents log contains completed episode summaries."
    )
    parser.add_argument("--stdout-log", required=True, type=Path)
    parser.add_argument("--status", type=Path)
    parser.add_argument("--report", type=Path)
    parser.add_argument("--min-completed-summaries", type=int, default=1)
    parser.add_argument("--max-no-episode-ratio", type=float, default=0.75)
    parser.add_argument("--require-final-completed-summary", action="store_true")
    return parser.parse_args()


def empty_behavior() -> dict[str, object]:
    return {
        "summary_count": 0,
        "completed_summary_count": 0,
        "no_episode_summary_count": 0,
        "last_step": None,
        "last_summary": None,
        "last_reward": None,
    }


def read_status(status_path: Path | None) -> dict[str, object] | None:
    if status_path is None:
        return None
    try:
        return json.loads(status_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        return {"error": str(error)}


def validate(arguments: argparse.Namespace) -> dict[str, object]:
    result: dict[str, object] = {
        "valid": False,
        "stdout_log": str(arguments.stdout_log),
        "behaviors": {behavior: empty_behavior() for behavior in BEHAVIORS},
        "worker_restart_count": 0,
        "reasons": [],
    }
    reasons = result["reasons"]
    assert isinstance(reasons, list)

    try:
        lines = arguments.stdout_log.read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError as error:
        reasons.append(f"cannot read stdout log: {error}")
        return result

    behaviors = result["behaviors"]
    assert isinstance(behaviors, dict)
    for line in lines:
        match = STEP_PATTERN.search(line)
        if match is None:
            if "Communicator has exited" in line:
                result["worker_restart_count"] = int(result["worker_restart_count"]) + 1
            continue

        behavior = match.group("behavior")
        if behavior not in behaviors:
            continue
        details = behaviors[behavior]
        assert isinstance(details, dict)
        details["summary_count"] = int(details["summary_count"]) + 1
        details["last_step"] = int(match.group("step"))

        reward_match = SUMMARY_PATTERN.search(line)
        if reward_match is not None:
            details["completed_summary_count"] = int(details["completed_summary_count"]) + 1
            details["last_summary"] = "completed"
            details["last_reward"] = float(reward_match.group("reward"))
        elif NO_EPISODE_MARKER in line:
            details["no_episode_summary_count"] = int(details["no_episode_summary_count"]) + 1
            details["last_summary"] = "no_episode"

    for behavior, details in behaviors.items():
        assert isinstance(details, dict)
        summary_count = int(details["summary_count"])
        completed_count = int(details["completed_summary_count"])
        no_episode_count = int(details["no_episode_summary_count"])
        if completed_count < arguments.min_completed_summaries:
            reasons.append(
                f"{behavior}: only {completed_count} completed summary(s); "
                f"required {arguments.min_completed_summaries}"
            )
        if summary_count > 0 and no_episode_count / summary_count > arguments.max_no_episode_ratio:
            reasons.append(
                f"{behavior}: no-episode ratio {no_episode_count / summary_count:.2f} "
                f"exceeds {arguments.max_no_episode_ratio:.2f}"
            )
        if arguments.require_final_completed_summary and details["last_summary"] != "completed":
            reasons.append(f"{behavior}: last summary is not a completed episode summary")

    status = read_status(arguments.status)
    if status is not None:
        result["status_read"] = "error" if "error" in status else "ok"

    result["valid"] = len(reasons) == 0
    return result


def main() -> int:
    arguments = parse_arguments()
    if arguments.min_completed_summaries < 1:
        raise SystemExit("--min-completed-summaries must be positive")
    if not 0.0 <= arguments.max_no_episode_ratio <= 1.0:
        raise SystemExit("--max-no-episode-ratio must be between 0 and 1")

    result = validate(arguments)
    rendered = json.dumps(result, indent=2, sort_keys=True)
    print(rendered)
    if arguments.report is not None:
        arguments.report.write_text(rendered + "\n", encoding="utf-8")
    return 0 if result["valid"] else 1


if __name__ == "__main__":
    sys.exit(main())
