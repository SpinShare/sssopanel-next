import fallbackBackground from '@/assets/background.svg';

// Every assets/themes/<id>/ with a theme.json is a theme, see assets/themes/README.md
const metaFiles = import.meta.glob('../assets/themes/*/theme.json', { eager: true, import: 'default' });
const backgroundFiles = import.meta.glob('../assets/themes/*/background.{png,jpg,webp,svg}', { eager: true, import: 'default' });
const logoFiles = import.meta.glob('../assets/themes/*/logo.{svg,png,webp}', { eager: true, import: 'default' });

const SEASONS = ['winter', 'spring', 'summer', 'autumn'];
const SEASON_ICONS = { winter: 'snowflake', spring: 'flower', summer: 'white-balance-sunny', autumn: 'leaf-maple' };

// { 'autumn-2026': url, ... }
const byId = (files) => Object.fromEntries(Object.entries(files).map(([path, value]) => [path.split('/').at(-2), value]));
const backgrounds = byId(backgroundFiles);
const logos = byId(logoFiles);

// Newest first
export const themes = Object.entries(byId(metaFiles))
    .map(([id, meta]) => ({
        id,
        name: meta.name ?? id,
        season: meta.season ?? '',
        year: String(meta.year ?? ''),
        icon: meta.icon ?? SEASON_ICONS[meta.season] ?? 'palette',
        background: backgrounds[id] ?? null,
        logo: logos[id] ?? null,
    }))
    .sort((a, b) => b.year - a.year || SEASONS.indexOf(b.season) - SEASONS.indexOf(a.season));

// No background (the 2023 events) borrows the newest one of the same season
for (const theme of themes) {
    theme.background ??= themes.find((other) => other.background && other.season === theme.season)?.background ?? fallbackBackground;
}

// Unknown ids (old season only settings, removed themes) get the newest theme of their season, then the newest one
export const getTheme = (id) => {
    const season = id?.split('-')[0];
    return themes.find((theme) => theme.id === id) ?? themes.find((theme) => theme.season === season) ?? themes[0] ?? { id: '', name: '', season: '', year: '', icon: 'palette', background: fallbackBackground, logo: null };
};
