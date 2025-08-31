
<p align="center"><img width="446" height="77" alt="WatchNow" src="https://github.com/user-attachments/assets/837c2f5c-44d3-4b86-8ebe-e71562dacf97" /></p>

WatchNow is a simple always-on-top app for watching videos while multitasking

![](https://i.imgur.com/tI06m8a.png)

### Features:
- Supported sources:
  - YouTube playlists (by id)
  - Reddit's /r/videos
  - Plex (embeds the plex.tv/web UI)
  - Hulu (embeds hulu.com)
  - Raindrop.io (a read-later service)
- Always-on-top by default
- Snap to bottom-right of a screen (right-click the ▼ button and choose a screen to snap to)
- Mini player (click the ▼ button to switch to the mini player to take up a minimal amount of the screen)
- No YouTube ads (WatchNow doesn't implement any AdBlock, but rather takes advantage of a quirk of YouTube that ads aren't shown on `youtube.com/embed/*` URLs)
- SponsorBlock (WatchNow will query the SponsorBlock API and skip segments. Currently, ALL types of SponsorBlock segments are skipped)
- "Cross-platform" (though a lot of internal logic is Windows-specific right now)
  - This app is built with [AvaloniaUI](https://avaloniaui.net/), a modern cross-platform UI framework. Currently, I'm only publishing Windows versions, but if there is a demand for another platform, please let me know
 
### To add a source:
1. Open WatchNow
2. Type a source string in the textbox. Supported source strings are:
    - /r/videos
    - hulu
    - plex
    - raindrop (or "raindrop.io")
    - _Any public YouTube playlist id, such as UUX6OQ3DkcsbYNE6H8uQQuVA_
3. Press the "+" button to add the source
_Sources are saved to the settings when the app is closed via the "X" button_
_There isn't currently a way to re-order sources via the UI. To reorder sources, open `%AppData%/WatchNow/Settings.ini` and re-order them there_

### To use the app:
1. Opening the app should asynchronously load all the latest items for all the sources
2. Click through the source tabs to select a source
3. Scroll through the list of recent videos for that source
4. Double-click anywhere on the video image, title, or surrounding area to load that video into the player
5. _Some sources, such as /r/videos or Raindrop, have extra buttons such as "Archive" or "Comments" that will let you perform an extra action for that video_

### Notes:

This project was made as a personal project and has a number of quirks (you might notice comments regarding this in the code). For me, it works "well enough". But you're welcome to suggest contributions
