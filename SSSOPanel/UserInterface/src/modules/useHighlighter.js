import { ref } from 'vue';

// Fine grained shiki: only these grammars, each its own chunk, loaded the first time a message uses it.
// The javascript engine skips the wasm. Names and the aliases people type after ``` in discord
const LANGS = {
    javascript: () => import('@shikijs/langs/javascript'),
    typescript: () => import('@shikijs/langs/typescript'),
    json: () => import('@shikijs/langs/json'),
    python: () => import('@shikijs/langs/python'),
    csharp: () => import('@shikijs/langs/csharp'),
    cpp: () => import('@shikijs/langs/cpp'),
    c: () => import('@shikijs/langs/c'),
    java: () => import('@shikijs/langs/java'),
    rust: () => import('@shikijs/langs/rust'),
    lua: () => import('@shikijs/langs/lua'),
    bash: () => import('@shikijs/langs/bash'),
    css: () => import('@shikijs/langs/css'),
    html: () => import('@shikijs/langs/html'),
    yaml: () => import('@shikijs/langs/yaml'),
    diff: () => import('@shikijs/langs/diff'),
    ini: () => import('@shikijs/langs/ini'),
    toml: () => import('@shikijs/langs/toml'),
    markdown: () => import('@shikijs/langs/markdown'),
    sql: () => import('@shikijs/langs/sql'),
    xml: () => import('@shikijs/langs/xml'),
    php: () => import('@shikijs/langs/php'),
    go: () => import('@shikijs/langs/go'),
    kotlin: () => import('@shikijs/langs/kotlin'),
    ruby: () => import('@shikijs/langs/ruby'),
    swift: () => import('@shikijs/langs/swift'),
    powershell: () => import('@shikijs/langs/powershell'),
    docker: () => import('@shikijs/langs/docker'),
};
const ALIASES = { js: 'javascript', ts: 'typescript', py: 'python', cs: 'csharp', 'c#': 'csharp', 'c++': 'cpp', rs: 'rust', sh: 'bash', shell: 'bash', zsh: 'bash', yml: 'yaml', md: 'markdown', golang: 'go', kt: 'kotlin', rb: 'ruby', ps1: 'powershell', pwsh: 'powershell', ps: 'powershell', dockerfile: 'docker', svg: 'xml', jsx: 'javascript', tsx: 'typescript' };

const THEME = 'github-dark-default';

// Bumped when a grammar finishes loading, so computed lists that highlight re-run
export const highlightVersion = ref(0);

let highlighterPromise = null;
// Set once it's created, highlighting itself has to be sync for the computed message lists
let highlighter = null;
const loading = new Map();
const loaded = new Set();

const getHighlighter = () => {
    // Shiki itself is a chunk too, most streams never see a code block
    highlighterPromise ??= Promise.all([import('shiki/core'), import('shiki/engine/javascript'), import('@shikijs/themes/github-dark-default')])
        .then(([{ createHighlighterCore }, { createJavaScriptRegexEngine }, theme]) => createHighlighterCore({ themes: [theme.default], langs: [], engine: createJavaScriptRegexEngine() }))
        .then((core) => {
            highlighter = core;
            return core;
        });
    return highlighterPromise;
};

const langName = (lang) => {
    const name = (lang ?? '').toLowerCase();
    return LANGS[ALIASES[name] ?? name] ? ALIASES[name] ?? name : null;
};

const load = (name) => {
    if (!loading.has(name)) {
        loading.set(
            name,
            getHighlighter()
                .then((core) => core.loadLanguage(LANGS[name]()))
                .then(() => {
                    loaded.add(name);
                    highlightVersion.value++;
                })
                // No internet for the chunk or a broken grammar, it just stays plain
                .catch(() => {}),
        );
    }
};

// Lines of { content, color, fontStyle } tokens, null while the grammar loads or for unknown languages (shown plain)
export const highlight = (code, lang) => {
    const name = langName(lang);
    if (!name) return null;
    if (!loaded.has(name)) {
        load(name);
        return null;
    }
    try {
        return highlighter.codeToTokens(code, { lang: name, theme: THEME }).tokens;
    } catch {
        return null;
    }
};
