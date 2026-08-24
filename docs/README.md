# PicturesToGpx Documentation

Welcome to the **PicturesToGpx** documentation. This directory provides an in-depth reference for the architecture, configuration formats, GPS data sources, coordinate math, and rendering pipelines used in this repository.

> [!IMPORTANT]
> **Agent & Contributor Rule**: Before proposing changes, designing plans, or modifying code in this repository, you must read the relevant documentation files in this directory. Any change to the codebase must also update the corresponding documentation files to keep documentation strictly in sync with the code.

---

## Documentation Index

1. **[Architecture & Design](architecture.md)**
   * High-level system overview and execution flow.
   * Project layers: Ingestion, Geometry / Projection, Rendering, Video Encoding.
   * External dependencies and target runtime (.NET Framework 4.8 / C# 8.0).

2. **[Configuration Reference](configuration.md)**
   * Complete JSON configuration schema and property reference.
   * Single project map/video generation setup.
   * Batch GPS activity preview generation (`generatePreviews` mode).

3. **[GPS Data Sources & Ingestion](gps-sources.md)**
   * EXIF metadata extraction from JPEG photos.
   * Google Timeline Location History (`Records.json` and KML).
   * Garmin / Strava FIT (`.fit`, `.fit.gz`) binary tracks.
   * Endomondo JSON tracks.
   * Chronological merging and filtering rules.

4. **[Rendering Pipeline & Video Generation](rendering-pipeline.md)**
   * Geographic to Spherical Mercator and pixel coordinate transformations.
   * Google Maps tile fetching, bounding box calculation, and disk caching.
   * Route smoothing via Chaikin's algorithm and pixel proximity decimation.
   * MJPEG AVI video generation, time/distance overlays, and per-day color cycling.

---

## Quick Usage

### 1. Generating a Single Map / Video
```bash
PicturesToGpx.exe <path-to-config.json>
```

### 2. Generating Activity Previews
```bash
PicturesToGpx.exe generatePreviews <path-to-config.json>
```
