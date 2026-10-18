# Themes

Every folder in here is one theme. The folder name is the theme id, written as `<season>-<year>` (e.g. `autumn-2026`). That id is what gets saved as the `theme` setting. The newest theme is the default, and the settings page lists them.

```tree
themes/
  autumn-2026/
    theme.json
    background.png
    logo.png
```

## theme.json

```json
{
    "name": "Autumn 2026",
    "season": "autumn",
    "year": 2026
}
```

| field    | what                                                                                                               |
| -------- | ------------------------------------------------------------------------------------------------------------------ |
| `name`   | Shown in the theme dropdown and on the End (Stream) screen as "SSSO &lt;name&gt;"                                  |
| `season` | `winter`, `spring`, `summer` or `autumn`. Picks the dropdown icon                                                  |
| `year`   | Sorts the themes, newest first                                                                                     |
| `icon`   | Optional, a [Material Design Icons](https://pictogrammers.com/library/mdi/) name to use instead of the season icon |

## background.png

The gradient behind every tinted part of the overlay: the full screen backgrounds, the sets box, the tug of war bar, the results tabs and the health rings. It's set to cover each element (`background-size: cover`), I recommend keeping it 1920x1080.

It should be **just the gradient**:

- no logo and no dot columns
- no grain, the overlay puts its own `noise.png` on top, so grain in the image would show up twice

`png`, `jpg`, `webp` and `svg` all work. Without a background, the theme borrows the one from the newest theme of the same season (that's what the 2023 themes do).

### Getting one from the event wallpaper

The event graphics live in the [SpinShare/media](https://github.com/SpinShare/media) repo under `SSSO/V3/<Season><Year>/`.

- **There's a `Wallpaper.psd`:** layer 1 is the clean gradient (layer 2 is the grain). It's usually a bit bigger than the canvas and offset, so crop it to the canvas:

```sh
magick identify Wallpaper.psd            # layer 1 shows e.g. 1984x1140 -32-30
magick 'Wallpaper.psd[1]' +repage -crop 1920x1080+32+30 +repage background.png
```

The crop offset is the layer offset without the minus signs.

- **There's only a flattened `Wallpaper.png`:** ask the designer for a clean export of the gradient -w-. But if that's not possible, most events reuse an older gradient with new colors (summer 2025 and autumn 2026 are recolored summer 2024, spring 2026 is recolored winter 2024). You can learn that color change from the pixels the logo and dots don't cover and apply it to the older clean gradient. That's how those backgrounds here were made: a cubic RGB polynomial, fit on 20px block averages of both wallpapers with the logo and dot columns masked out.

## logo

`logo.svg`, `logo.png` or `logo.webp`, the event logo shown on the Countdown and End (Stream) screens. It's the `Logo` file from the media repo. Leave it out and those screens just don't show one.

## New event checklist

1. Make `themes/<season>-<year>/` with a `theme.json`, a `background.png` and a `logo.png` (or svg, or webp, or whatever)
2. Done for the overlay. It's the newest theme, so it's the default unless you picked another one in the settings
3. Optional, but i recommend it, the app icon: copy the event's `CircleLogo.png` to `SSSOPanel/electron/icon.png` and regenerate the favicon from it:

```sh
magick icon.png -define icon:auto-resize=64,48,32,16 SSSOPanel/UserInterface/public/favicon.ico
```
