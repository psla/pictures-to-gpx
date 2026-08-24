# GPS Data Sources & Ingestion

**PicturesToGpx** supports ingesting and merging geospatial data from multiple heterogenous formats.

---

## Supported Input Sources

### 1. Photo EXIF Metadata ([`ImageUtility.cs`](../PicturesToGpx/ImageUtility.cs))
* Extracts GPS coordinates, UTC timestamps, and Dilution of Precision (DOP) from JPEG EXIF tags via `MetadataExtractor`.
* Coordinates in DMS notation and fractional DOP values are parsed by [`LatLongParser`](../PicturesToGpx/Geometry/LatLongParser.cs) and [`ExifParser`](../PicturesToGpx/Geometry/ExifParser.cs).
* Automatically filters out low-accuracy points with high dilution of precision.

### 2. Google Timeline / Location History JSON ([`GoogleTimelineJsonReader.cs`](../PicturesToGpx/Gps/GoogleTimelineJsonReader.cs))
* Parses Google Takeout Location History exports (`Records.json`).
* Filters out points where the reported accuracy exceeds `GoogleTimelineMinimumAccuracyMeters`.

### 3. Google Timeline KML ([`GoogleTimelineKmlReader.cs`](../PicturesToGpx/Gps/GoogleTimelineKmlReader.cs))
* Parses standard Google Timeline KML tracks containing timestamped coordinate lists.

### 4. Garmin / Strava FIT Files ([`FitReader.cs`](../PicturesToGpx/Gps/FitReader.cs))
* Reads raw or gzipped FIT files (`.fit`, `.fit.gz`) using the Dynastream FIT SDK (`Fit.dll`).
* Converts FIT semicircles and epoch timestamps into standard coordinates and UTC times.

### 5. Endomondo JSON Files ([`EndomondoJsonReader.cs`](../PicturesToGpx/Gps/EndomondoJsonReader.cs))
* Parses workout logs exported from Endomondo in JSON format.

---

## Position Caching

To avoid costly re-parsing across runs, extracted position data is serialized as JSON in `WorkingDirectory`:
* Photo directory positions are cached per folder slug.
* GPS track directory positions are cached in `endomondo-positions.json`.
* On subsequent executions, cached positions are reloaded directly from disk if available.

---

## Merging & Deduplication

* **Chronological Merging** ([`EnumerableUtils.Merge`](../PicturesToGpx/EnumerableUtils.cs#L9-L40)): Merges sorted location streams into a single time-ordered sequence in linear time.
* **Timeframe Bounding**: Truncates points outside `[StartTime, EndTime)`.
* **Track Segmentation** ([`Program.WritePointsAsGpx`](../PicturesToGpx/Program.cs#L347-L373)): Splits points into distinct GPX tracks when time gaps between consecutive points exceed 5 hours.
