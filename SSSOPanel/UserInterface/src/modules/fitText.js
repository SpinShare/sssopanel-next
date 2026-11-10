// Font size (vw, or em) that keeps a long name on one line: base size up to `chars` characters, smaller after that down to
// `min` of it. Past that the element's own ellipsis takes over
export const fitFontSize = (text, base, chars, min = 0.55, unit = 'vw') => {
    const length = String(text ?? '').length;
    return `${base * Math.max(min, Math.min(1, chars / Math.max(1, length)))}${unit}`;
};
