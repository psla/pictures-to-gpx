# Configuration Reference

**PicturesToGpx** uses JSON configuration files to control data inputs, time filters, rendering parameters, output paths, and video formats.

Configurations are strongly typed and modeled by the [`Settings`](../PicturesToGpx/Settings.cs) class.

---

## Schema Reference

### Top-Level Properties

| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `ProjectName` | `string` | `"project_name"` | **Required**. Alphanumeric identifier used for naming and default directory paths. |
| `PicturesInputDirectory` | `string` | `null` | Single directory containing JPEG photos with GPS EXIF metadata. |
| `PicturesInputDirectories` | `string[]` | `[]` | List of directories containing JPEG photos. Can be combined with `PicturesInputDirectory`. |
| `GpsInputDirectory` | `string` | `null` | Directory to search for standalone GPS files (`.json` for Endomondo, `.fit` / `.fit.gz` for Garmin). |
| `GoogleTimelineKmlFile` | `string` | `null` | Path to a Google Timeline KML export. |
| `GoogleTimelineJsonFile` | `string` | `null` | Path to a single Google Timeline `Records.json` export. |
| `GoogleTimelineJsonFiles` | `string[]` | `[]` | Array of paths to multiple Google Timeline `Records.json` files (e.g., from multiple accounts). |
| `GoogleTimelineMinimumAccuracyMeters` | `int` | `500` | Minimum accuracy threshold in meters. Google Timeline points with accuracy values greater than this are discarded. |
| `OutputDirectory` | `string` | Required | Target directory where `map.avi`, `track.gpx`, and `track.json` are written. |
| `WorkingDirectory` | `string` | `Path.GetTempPath()/<ProjectName>` | Intermediate working directory used for caching extracted position JSON files. |
| `StartTime` | `string` (DateTimeOffset) | `DateTime.MinValue` | Minimum timestamp filter. Points before this timestamp are excluded. |
| `EndTime` | `string` (DateTimeOffset) | `2400-01-01` | Maximum timestamp filter. Points after this timestamp are excluded. |
| `TileCacheDirectory` | `string` | `"<temp_dir>/tile-cache"` | Local directory where Google Maps raster tiles are cached. Can be shared across projects. |
| `MinPixelProximity` | `int` | `8` | Minimum pixel distance required between adjacent points before filtering. |
| `DisplayDistance` | `bool` | `true` | When `true`, displays cumulative distance traveled (km) on the video / still frame. |
| `DisplayDateTime` | `bool` | `true` | When `true`, displays the current local date & time on the video frame. |
| `DayColors` | `string[]` | `["#ff0000"]` | Array of hex color strings (e.g. `"#ff0000"`) cycled per day of the trip. |
| `TilerConfig` | `object` | `{}` | Tile rendering settings. |
| `VideoConfig` | `object` | `{}` | Video generation parameters. |
| `StillConfig` | `object` | `{}` | Paths for saving static map images. |
| `SofteningSettings` | `object` | `{}` | Chaikin line smoothing settings. |

---

### Nested Configuration Objects

#### `TilerConfig`
* `DrawTilesBoundingBox` (`bool`, default: `false`): When `true`, draws red bounding boxes around individual map tiles for debugging.

#### `VideoConfig`
* `ProduceVideo` (`bool`, default: `true`): Whether to generate an animated `map.avi` file.
* `VideoDuration` (`TimeSpan` string, default: `"00:00:04.500000"`): Target duration of the video.
* `Width` (`int`, default: `1920`): Video frame width in pixels.
* `Height` (`int`, default: `1080`): Video frame height in pixels.
* `Framerate` (`int`, default: `30`): Output frames per second.
* `RepeatLastFrameCount` (`int`, default: `1`): Number of additional frames to append with the completed route.

#### `StillConfig`
* `EmptyMapPath` (`string`): File path where an empty base map should be saved as PNG.
* `PopulatedMapPath` (`string`): File path where the full rendered route map should be saved as PNG.

#### `SofteningSettings` (Chaikin Smoothing)
* `WhereToRound` (`double`, default: `0.75`): Chaikin ratio (typically between `0.6` and `0.95`) determining how close to vertices the smoothing curve begins.
* `MaxIterationCount` (`int`, default: `3`): Number of smoothing iterations.

---

## Example Configurations

### 1. Full Multi-Day Road Trip Video
```json
{
  "ProjectName": "portugal-2024",
  "PicturesInputDirectories": [
    "U:\\photos\\2024\\2024-07_Pixel7",
    "U:\\photos\\2024\\2024-07_Pixel7Pro_Madeira",
    "U:\\photos\\2024\\2024-09_Pixel7",
    "U:\\photos\\2024\\2024-11_Pixel6a"
  ],
  "GpsInputDirectory": "U:\\projects\\2024-07_Portugal\\WorkDir",
  "OutputDirectory": "U:\\projects\\2024-07_Portugal\\Map",
  "StartTime": "2024-07-19 14:00:00",
  "EndTime": "2024-07-23 00:00:00",
  "WorkingDirectory": "U:\\projects\\2024-07_Portugal\\WorkDir",
  "GoogleTimelineJsonFiles": [
    "U:\\gps\\Takeout\\Location History (Timeline)\\Records.json"
  ],
  "GoogleTimelineMinimumAccuracyMeters": 50,
  "TileCacheDirectory": "C:\\tmp\\tile-cache",
  "TilerConfig": {
    "DrawTilesBoundingBox": false
  },
  "VideoConfig": {
    "ProduceVideo": true,
    "VideoDuration": "00:00:10.000000",
    "Width": 1920,
    "Height": 1080,
    "Framerate": 30
  },
  "SofteningSettings": {
    "WhereToRound": 0.9
  },
  "DayColors": [ "#ff0000", "#0000ff", "#00ffff", "#ff00ff", "#257452", "#ff7e00" ]
}
```

### 2. Batch Activity Preview Generation
```json
{
  "ProjectName": "gps-previews-2022",
  "GpsInputDirectory": "U:\\gps\\strava_export\\activities",
  "OutputDirectory": "U:\\gps\\strava_export\\activities\\previews",
  "WorkingDirectory": "C:\\tmp\\WorkDir",
  "TileCacheDirectory": "C:\\tmp\\tile-cache",
  "VideoConfig": {
    "ProduceVideo": false,
    "Width": 1920,
    "Height": 1080
  },
  "SofteningSettings": {
    "WhereToRound": 0.9
  }
}
```
