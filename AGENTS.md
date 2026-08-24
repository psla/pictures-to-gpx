# Agent Guidelines for PicturesToGpx

All AI agents working on this repository MUST strictly follow these rules:

---

## 1. Documentation First (Mandatory)
Before proposing any implementation plans, making architectural decisions, or modifying any code in this repository:
* You **MUST** read the project documentation located in the [`docs/`](docs/README.md) directory.
* Review the specific guide for the area you are touching:
  * [`docs/architecture.md`](docs/architecture.md): Component structure, data flow, and solution design.
  * [`docs/configuration.md`](docs/configuration.md): JSON configuration schema, options, and sample configs.
  * [`docs/gps-sources.md`](docs/gps-sources.md): GPS parsing (EXIF, FIT, Google Timeline, Endomondo) and caching.
  * [`docs/rendering-pipeline.md`](docs/rendering-pipeline.md): Projections, tile rendering, smoothing, and video encoding.

---

## 2. Keep Documentation Synchronized
* For **any** code, configuration, or architectural change, you **MUST** update the corresponding documentation files in `docs/`.
* No feature, configuration change, or refactoring is complete until the documentation reflects the updated state of the codebase.

---

## 3. Mandatory User Review Before Commits
* **NEVER** create a git commit automatically without explicit user approval.
* Always present the changes, test results, and diffs to the user and wait for their confirmation before committing.

---

## 4. Code & Quality Standards
* **Framework & Language**: Target .NET Framework 4.8 / C# 8.0.
* **Testing**: When modifying or adding parsing or geometry logic, add or update corresponding unit tests in [`PicturesToGpx.Test`](PicturesToGpx.Test).
* **Code Integrity**: Preserve existing comments, docstrings, and robust error handling.
