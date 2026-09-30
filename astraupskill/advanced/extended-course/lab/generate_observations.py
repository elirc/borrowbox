"""Generate an isolated current-behavior lab using the existing safe copy recipe."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, help="New absolute directory outside BorrowBox")
    parser.add_argument("--suite", choices=["foundation", "inspection"], default="foundation")
    args = parser.parse_args()
    here = Path(__file__).resolve().parent
    template_name = "Program.cs.txt" if args.suite == "foundation" else "InspectionProgram.cs.txt"
    template = (here / template_name).read_text(encoding="utf-8")
    inspection_relative = "src/BorrowBox.Infrastructure/Inspections/InMemoryInspectionStore.cs"
    project = here.parents[3]
    inspection_raw = (project / inspection_relative).read_bytes() if args.suite == "inspection" else None
    generator = here.parent.parent / "lab" / "generate_lab.py"
    # The existing generator enforces new-output and outside-project constraints.
    subprocess.run([sys.executable, str(generator), "--mode", "reference",
                    "--output", args.output], check=True)
    output = Path(args.output).expanduser().resolve()
    if inspection_raw is not None:
        destination = output / inspection_relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(inspection_raw)
        source_map_path = output / "SOURCE-MAP.json"
        source_map = json.loads(source_map_path.read_text(encoding="utf-8"))
        digest = hashlib.sha256(inspection_raw).hexdigest()
        source_map["files"].append({"path": inspection_relative, "original_sha256": digest,
                                    "lab_sha256": digest, "deliberate_defects": []})
        source_map_path.write_text(json.dumps(source_map, indent=2) + "\n", encoding="utf-8")
    (output / "Program.cs").write_text(template, encoding="utf-8")
    (output / "README.txt").write_text(
        "Extended-course current-behavior observations.\n"
        "Run: dotnet run --project Lab.csproj\n"
        "Suite: " + args.suite + ". Passing observations include documented limitations.\n"
        "No application files, API server, database, or external packages are used.\n",
        encoding="utf-8")


if __name__ == "__main__":
    main()
