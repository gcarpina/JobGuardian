#!/usr/bin/env python3
"""Enforce per-package line and branch coverage from Coverlet Cobertura reports."""

import argparse
from collections import defaultdict
from pathlib import Path
import sys
import xml.etree.ElementTree as ET


PACKAGES = (
    "JobGuardian.Abstractions",
    "JobGuardian.Core",
    "JobGuardian.PostgreSql",
)


def parse_coverage(report_paths):
    lines = defaultdict(int)
    branches = {}

    for report_path in report_paths:
        root = ET.parse(report_path).getroot()
        for source_class in root.findall(".//class"):
            filename = source_class.get("filename", "")
            package = next(
                (name for name in PACKAGES if filename.startswith(f"{name}/")),
                None,
            )
            if package is None:
                continue

            for line in source_class.findall("./lines/line"):
                line_key = (package, filename, line.get("number"))
                lines[line_key] = max(
                    lines[line_key],
                    int(line.get("hits", "0")),
                )

                if line.get("branch", "").lower() != "true":
                    continue

                coverage = line.get("condition-coverage", "")
                if "(" not in coverage or "/" not in coverage:
                    raise ValueError(
                        f"Malformed branch coverage in {report_path}: "
                        f"{filename}:{line.get('number')}"
                    )
                covered, total = map(
                    int,
                    coverage.split("(", 1)[1].split(")", 1)[0].split("/", 1),
                )
                branch_key = (package, filename, line.get("number"))
                previous = branches.get(branch_key)
                if previous is None or total > previous[1]:
                    branches[branch_key] = (covered, total)
                elif total == previous[1] and covered > previous[0]:
                    branches[branch_key] = (covered, total)

    return lines, branches


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("coverage_directory", type=Path)
    parser.add_argument(
        "--threshold",
        type=float,
        default=80.0,
        help="Required coverage percentage; actual coverage must be strictly greater",
    )
    args = parser.parse_args()

    reports = sorted(args.coverage_directory.rglob("coverage.cobertura.xml"))
    if not reports:
        parser.error(f"No coverage.cobertura.xml reports found under {args.coverage_directory}")

    lines, branches = parse_coverage(reports)
    failed = False

    for package in PACKAGES:
        package_lines = [
            hit > 0
            for (source_package, _, _), hit in lines.items()
            if source_package == package
        ]
        if not package_lines:
            print(f"{package}: no production source lines found", file=sys.stderr)
            failed = True
            continue

        line_coverage = 100.0 * sum(package_lines) / len(package_lines)
        print(
            f"{package}: line coverage {sum(package_lines)}/{len(package_lines)} "
            f"({line_coverage:.2f}%)"
        )
        if line_coverage <= args.threshold:
            print(
                f"  FAIL: line coverage must be greater than {args.threshold:.2f}%",
                file=sys.stderr,
            )
            failed = True

        package_branches = [
            coverage
            for (source_package, _, _), coverage in branches.items()
            if source_package == package
        ]

        if package_branches:
            covered = sum(value[0] for value in package_branches)
            total = sum(value[1] for value in package_branches)
            branch_coverage = 100.0 * covered / total
            print(
                f"{package}: branch coverage {covered}/{total} "
                f"({branch_coverage:.2f}%)"
            )
            if branch_coverage <= args.threshold:
                print(
                    f"  FAIL: branch coverage must be greater than "
                    f"{args.threshold:.2f}%",
                    file=sys.stderr,
                )
                failed = True
        else:
            print(f"{package}: no conditional branches; branch threshold not applicable")

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
