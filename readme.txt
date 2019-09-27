- Fix out of bounds error when whole world needs to be drawn
- Add ability to open existing GPS tracks
 - If GPS tracks are merged, either ignore the points that are within a few seconds from GPS track points,
   or ignore them if they are more than a few hundred meters away
- Draw different days with different colors
- Display distance (km) from 0.
- [P2] Display time (approx, interpolated) (when 'use fixed time interval' is implemented)
+ Draw route on map with chosen interval (e.g. 10 seconds per day)
 - Use fixed distance interval (fixed distance), instead of per-point interval
 - [P2] Use fixed time interval.
- Single setting describing the project & maybe generate empty project file.
- 