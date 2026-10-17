// Every assets/eventlogo-<season>-<year>.svg|png, so a new event only needs its logo dropped in.
// Logos come from https://github.com/SpinShare/media (SSSO/V3/<Season><Year>/Logo)
const files = import.meta.glob('../assets/eventlogo-*.{svg,png}', { eager: true, import: 'default' });

// { winter: { 2025: url, ... }, ... }
export const eventLogos = {};
for (const [path, url] of Object.entries(files)) {
    const match = path.match(/eventlogo-([a-z]+)-(\d{4})\.\w+$/);
    if (!match) continue;
    (eventLogos[match[1]] ??= {})[match[2]] = url;
}

// Newest first
export const logoYears = (theme) => Object.keys(eventLogos[theme] ?? {}).sort((a, b) => b - a);

// Falls back to the newest logo of the theme when there is none for the year
export const getEventLogo = (theme, year) => {
    const logos = eventLogos[theme] ?? {};
    return logos[year] ?? logos[logoYears(theme)[0]] ?? null;
};
