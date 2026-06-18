# itch.io WebGL Release Guide

This guide publishes **Splat Fighters** as an HTML5 game that runs in the browser on itch.io. The automated path uses the enabled scenes in `ProjectSettings/EditorBuildSettings.asset`, with `MainMenu` as the first scene.

Published project page: [https://tiempo206.itch.io/splat-fighters](https://tiempo206.itch.io/splat-fighters)

## Prerequisites

- Unity Editor `2022.3.62f3c1`.
- The matching **WebGL Build Support** module in Unity Hub.
- An itch.io account and a project page slug.
- `butler` for command-line uploads, or a browser for manual ZIP uploads.

Install WebGL support from **Unity Hub > Installs > 2022.3.62f3c1 > Add modules > WebGL Build Support**.

## Build

Close the Unity Editor before running the command-line build, then run from the repository root:

```bash
"Splat Fighters/Tools/build_webgl.sh"
```

The build script:

- builds every enabled scene in Build Settings;
- uses Brotli compression supported by itch.io;
- keeps decompression fallback and WebGL threads disabled;
- writes the release to `Splat Fighters/Builds/WebGL`;
- writes the Unity log to `Splat Fighters/Logs/webgl-build.log`;
- fails if the build does not produce a root `index.html`.

The same action is available inside Unity at **Splat Fighters > Build > WebGL for itch.io**.

## Package and validate

```bash
"Splat Fighters/Tools/package_webgl.sh"
```

This checks itch.io's HTML5 limits and creates:

```text
Splat Fighters/Builds/Splat-Fighters-WebGL.zip
```

The archive contains `index.html` at its root. Do not ZIP the parent `WebGL` folder itself, because that would place `index.html` one directory too deep.

## Test locally

Do not double-click `index.html`; browser security rules can block Unity data files when opened with a `file://` URL. Serve the build over HTTP:

```bash
python3 "Splat Fighters/Tools/serve_webgl.py"
```

Open `http://localhost:8000` and verify:

- the main menu loads without browser console errors;
- Turf War and Tower Control both start;
- keyboard, mouse, camera lock, shooting, roller, pause, and restart work;
- the training scene opens and returns to the menu;
- audio starts after user interaction;
- a complete match reaches the results screen;
- the game remains readable at common 16:9 desktop sizes and fullscreen.

## Create the itch.io project page

1. Sign in to itch.io and choose **Upload new project**.
2. Set **Title** to `Splat Fighters`.
3. Choose a lowercase URL slug, for example `splat-fighters`.
4. Set **Kind of project** to **HTML**.
5. Keep **Release status** as **In development** until final testing is complete.
6. Keep the page private or restricted while testing.
7. Add a short description, controls, screenshots, credits, and the documented public-asset acknowledgements.
8. Save the page before uploading.

Recommended embed settings:

- **Click to launch in fullscreen** for the most reliable third-person mouse/camera experience.
- Keep **Click to Play** enabled so audio can begin after a user gesture.
- Enable itch.io's fullscreen button if the selected embed mode exposes it.
- Do not mark the build **Mobile Friendly** unless touch controls and mobile performance have been tested.

## Upload in the browser

1. Open the project's edit page.
2. Upload `Splat Fighters/Builds/Splat-Fighters-WebGL.zip`.
3. Mark the upload as **This file will be played in the browser** if itch.io does not detect it automatically.
4. Save, wait for archive processing, and open the private project page.
5. Test the published build in Chrome or Edge and Safari. Check the browser console if loading stalls.

## Upload with butler

Authenticate once:

```bash
butler login
```

Then provide the itch.io username and project slug:

```bash
ITCH_USER=your-username \
ITCH_GAME=splat-fighters \
"Splat Fighters/Tools/publish_itch.sh"
```

The script pushes `Builds/WebGL` to the `html5` channel and uses the Unity bundle version as itch.io's user version. After the first push, open the itch.io edit page, set the project kind to **HTML**, and tag the channel **HTML5 / Playable in browser**. These two settings cannot be completed by butler.

For later releases, rebuild and rerun the same publish command. Butler uploads only the changed data.

## Final release checklist

- The hosted build loads from the itch.io page, not only localhost.
- No `403`, missing-file, mixed-content, out-of-memory, or WebAssembly errors appear in the browser console.
- The ZIP has no more than 1,000 files, no file larger than 200 MB, and no more than 500 MB extracted content.
- All asset paths use the exact filename case and all external requests use HTTPS.
- Controls, objective rules, game modes, credits, asset notices, and known limitations are visible on the page.
- The page has a cover image and at least three representative screenshots.
- Visibility is changed from private/restricted only after the hosted build passes the smoke test.

## Troubleshooting

- **WebGL is missing in Unity:** add the WebGL Build Support module for the exact editor version.
- **Build fails immediately:** inspect `Splat Fighters/Logs/webgl-build.log` and search for `error CS` or `BuildFailedException`.
- **Build works locally but itch.io returns 403:** check filename case and confirm `index.html` is at the ZIP root.
- **`.br` files fail to load:** confirm the upload was processed as an HTML5 game. itch.io supports Unity Brotli files whose names end in `.br`.
- **Audio is silent:** click inside the game first; browsers commonly require a user gesture before playing audio.
- **Mouse camera does not move:** click the game canvas to give it focus and enter pointer lock; fullscreen launch is recommended.
- **Loading or memory failure:** reduce texture sizes and unused assets, then rebuild. Browser builds have tighter memory and download constraints than desktop builds.

## Official references

- [itch.io: Uploading HTML5 games](https://itch.io/docs/creators/html5)
- [itch.io: Installing butler](https://itch.io/docs/butler/installing.html)
- [itch.io: Pushing builds](https://itch.io/docs/butler/pushing.html)
- [Unity 2022.3: Getting started with WebGL](https://docs.unity3d.com/2022.3/Documentation/Manual/webgl-gettingstarted.html)
- [Unity 2022.3: Build a WebGL application](https://docs.unity3d.com/2022.3/Documentation/Manual/webgl-building.html)
