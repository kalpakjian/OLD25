from pathlib import Path
from collections import Counter


def get_latest_combat_log():
    project_root = Path(__file__).resolve().parent
    logs_dir = project_root / "Logs" / "combat_logs"

    if not logs_dir.exists():
        raise FileNotFoundError(f"logs dir not found: {logs_dir}")

    log_files = list(logs_dir.glob("combat_log_*.txt"))
    if not log_files:
        raise FileNotFoundError(f"no combat_log_*.txt in {logs_dir}")

    return max(log_files, key=lambda p: p.stat().st_mtime)


def parse_key_value_line(line: str):
    if "[WeaponHitbox]" not in line:
        return None

    if "event=" not in line:
        return None

    data = {}
    parts = line.strip().split()

    for part in parts:
        if "=" not in part:
            continue
        key, value = part.split("=", 1)
        data[key.strip()] = value.strip()

    if "event" not in data:
        return None

    return data


def load_weapon_events(path: str):
    events = []

    with open(path, "r", encoding="utf-8") as f:
        for raw_line in f:
            data = parse_key_value_line(raw_line)
            if data is not None:
                events.append(data)

    return events


def to_int(value, default=0):
    try:
        return int(float(value))
    except (TypeError, ValueError):
        return default


def get_last_cycle_id(events):
    cycle_ids = []

    for e in events:
        cycle = e.get("cycle")
        if cycle is not None:
            try:
                cycle_ids.append(int(float(cycle)))
            except ValueError:
                pass

    if not cycle_ids:
        return None

    return max(cycle_ids)


def read_preview_lines(path: str, limit: int = 20):
    preview = []
    total_lines = 0

    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            total_lines += 1
            if "[WeaponHitbox]" in line or "[CombatAttack]" in line:
                if len(preview) < limit:
                    preview.append(line.rstrip())

    return total_lines, preview


def analyze_last_cycle(path: str):
    events = load_weapon_events(path)
    last_cycle = get_last_cycle_id(events)

    lines = []
    lines.append(f"Analyzing log: {path}")
    lines.append(f"WeaponHitbox structured events found: {len(events)}")
    lines.append("")

    if last_cycle is None:
        lines.append("No WeaponHitbox cycle data found.")
        return "\n".join(lines)

    cycle_events = [e for e in events if to_int(e.get("cycle")) == last_cycle]

    phase_counts = Counter()
    phase_sequence = []
    treasure_hits = Counter()
    target_hits = Counter()
    repeat_blocks = Counter()
    repeat_block_treasures = Counter()
    invalid_hits = Counter()

    begin_count = 0
    end_count = 0
    owner_name = None
    weapon_name = None

    for e in cycle_events:
        event = e.get("event", "")
        owner_name = owner_name or e.get("owner")
        weapon_name = weapon_name or e.get("weapon")

        if event == "begin":
            begin_count += 1

        elif event == "end":
            end_count += 1

        elif event == "phase":
            phase = e.get("phase", "?")
            atk = e.get("atk", "?")
            phase_counts[(phase, atk)] += 1
            phase_sequence.append(f"phase={phase},atk={atk}")

        elif event == "hit_treasure":
            phase = e.get("phase", "?")
            atk = e.get("atk", "?")
            target = e.get("target", "?")
            treasure_hits[(phase, atk, target)] += 1

        elif event == "hit":
            phase = e.get("phase", "?")
            atk = e.get("atk", "?")
            target = e.get("target", "?")
            deal = e.get("deal", "?")
            target_hits[(phase, atk, target, deal)] += 1

        elif event == "repeat_block":
            phase = e.get("phase", "?")
            atk = e.get("atk", "?")
            target = e.get("target", "?")
            repeat_blocks[(phase, atk, target)] += 1

        elif event == "repeat_block_treasure":
            phase = e.get("phase", "?")
            atk = e.get("atk", "?")
            target = e.get("target", "?")
            repeat_block_treasures[(phase, atk, target)] += 1

        elif event == "invalid":
            phase = e.get("phase", "?")
            atk = e.get("atk", "?")
            collider = e.get("collider", "?")
            invalid_hits[(phase, atk, collider)] += 1

    lines.append("=== Last Cycle Summary ===")
    lines.append(f"cycle={last_cycle} owner={owner_name} weapon={weapon_name}")
    lines.append(f"begin={begin_count} end={end_count}")

    lines.append("")
    lines.append("=== Phase Counts ===")
    if phase_counts:
        for (phase, atk), count in sorted(phase_counts.items(), key=lambda x: to_int(x[0][0])):
            lines.append(f"phase={phase} atk={atk}: {count}")
    else:
        lines.append("(none)")

    lines.append("")
    lines.append("=== Phase Sequence ===")
    if phase_sequence:
        lines.append(" -> ".join(phase_sequence))
    else:
        lines.append("(none)")

    lines.append("")
    lines.append("=== Treasure Hits ===")
    if treasure_hits:
        for (phase, atk, target), count in sorted(
            treasure_hits.items(),
            key=lambda x: (to_int(x[0][0]), x[0][2])
        ):
            lines.append(f"phase={phase} atk={atk} target={target}: {count}")
    else:
        lines.append("(none)")

    lines.append("")
    lines.append("=== Target Hits ===")
    if target_hits:
        for (phase, atk, target, deal), count in sorted(
            target_hits.items(),
            key=lambda x: (to_int(x[0][0]), x[0][2])
        ):
            lines.append(f"phase={phase} atk={atk} target={target} deal={deal}: {count}")
    else:
        lines.append("(none)")

    lines.append("")
    lines.append("=== Repeat Blocked ===")
    if repeat_blocks:
        for (phase, atk, target), count in sorted(
            repeat_blocks.items(),
            key=lambda x: (to_int(x[0][0]), x[0][2])
        ):
            lines.append(f"phase={phase} atk={atk} target={target}: {count}")
    else:
        lines.append("(none)")

    lines.append("")
    lines.append("=== Treasure Repeat Blocked ===")
    if repeat_block_treasures:
        for (phase, atk, target), count in sorted(
            repeat_block_treasures.items(),
            key=lambda x: (to_int(x[0][0]), x[0][2])
        ):
            lines.append(f"phase={phase} atk={atk} target={target}: {count}")
    else:
        lines.append("(none)")

    lines.append("")
    lines.append("=== Invalid Hits ===")
    if invalid_hits:
        for (phase, atk, collider), count in sorted(
            invalid_hits.items(),
            key=lambda x: (to_int(x[0][0]), x[0][2])
        ):
            lines.append(f"phase={phase} atk={atk} collider={collider}: {count}")
    else:
        lines.append("(none)")

    return "\n".join(lines)


def main():
    latest_log = get_latest_combat_log()
    print(f"Using log: {latest_log}")

    if not latest_log.exists():
        print("ERROR: latest log file does not exist")
        return

    print(f"Log size: {latest_log.stat().st_size} bytes")

    total_lines, preview = read_preview_lines(str(latest_log), limit=20)
    print(f"Total lines in file: {total_lines}")
    print("=== Preview (first matching lines) ===")
    if preview:
        for line in preview:
            print(line)
    else:
        print("(no [WeaponHitbox] or [CombatAttack] lines found)")
    print("")

    summary = analyze_last_cycle(str(latest_log))
    print(summary)

    summary_path = latest_log.parent / "combat_summary.txt"
    summary_path.write_text(summary, encoding="utf-8")
    print(f"\nSummary written to: {summary_path}")


if __name__ == "__main__":
    main()