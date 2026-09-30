"""Create a disposable lab from current project source; never edit the project."""
import argparse
import hashlib
import json
from pathlib import Path

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, help="New absolute directory outside the project")
    parser.add_argument("--mode", choices=["starter", "reference"], required=True)
    args = parser.parse_args()
    here = Path(__file__).resolve().parent
    project = here.parents[2]
    output = Path(args.output).expanduser()
    if not output.is_absolute():
        parser.error("--output must be an absolute path")
    output = output.resolve()
    if output == project or project in output.parents:
        parser.error("Output must be outside the application project")
    if output.exists():
        parser.error("Output already exists; choose a new directory (nothing overwritten)")
    recipe = json.loads((here / "recipe.json").read_text(encoding="utf-8"))
    prepared = []
    hashes = []
    for rel in recipe["sources"]:
        source = project / rel
        raw = source.read_bytes()
        text = raw.decode("utf-8-sig").replace("\r\n", "\n")
        changed = []
        if args.mode == "starter":
            for edit in recipe["edits"]:
                if edit["path"] != rel:
                    continue
                if text.count(edit["before"]) != 1:
                    parser.error("Source drift at " + rel + "; expected edit anchor exactly once")
                text = text.replace(edit["before"], edit["after"], 1)
                changed.append(edit["purpose"])
        prepared.append((rel, text))
        hashes.append({"path": rel, "original_sha256": hashlib.sha256(raw).hexdigest(),
                       "lab_sha256": hashlib.sha256(text.encode("utf-8")).hexdigest(),
                       "deliberate_defects": changed})
    # All reads and mutation anchors validated before creating any output.
    output.mkdir(parents=True, exist_ok=False)
    for rel, text in prepared:
        destination = output / rel
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_text(text, encoding="utf-8")
    (output / "Program.cs").write_text((here / "Program.cs.txt").read_text(encoding="utf-8"), encoding="utf-8")
    (output / "Lab.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
        '<TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings>'
        '<Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors>'
        '<RestorePackagesWithLockFile>false</RestorePackagesWithLockFile>'
        '</PropertyGroup></Project>', encoding="utf-8")
    (output / "Directory.Build.props").write_text("<Project />", encoding="utf-8")
    (output / "Directory.Build.targets").write_text("<Project />", encoding="utf-8")
    (output / "NuGet.Config").write_text(
        '<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
    (output / "global.json").write_text((project / "global.json").read_text(encoding="utf-8"), encoding="utf-8")
    (output / "SOURCE-MAP.json").write_text(json.dumps(
        {"mode": args.mode, "origin": str(project), "files": hashes}, indent=2), encoding="utf-8")
    (output / "README.txt").write_text(
        "Disposable learning copy. Run: dotnet run --project Lab.csproj\n"
        "No external NuGet packages. Starter failures are intentional.\n"
        "Edit only this lab's copied source. SOURCE-MAP.json records originals.\n",
        encoding="utf-8")
    print("Created " + args.mode + " lab at " + str(output))
    print("Run: dotnet run --project " + str(output / "Lab.csproj"))

if __name__ == "__main__":
    main()
