import { highlight } from '@/modules/useHighlighter';

// Discord message markup for the ref den: lines of styled parts (ScreenRefDenMessage renders them) and plain text previews

// Links show their text, timestamps the time. Mentions the bot couldn't name (or an older bot) stay generic
const cleanText = (text) =>
    text
        .replace(/\[([^\]]+)\]\(<?https?:\/\/[^\s)>]+>?\)/g, '$1')
        .replace(/<@!?\d+>/g, '@user')
        .replace(/<@&\d+>/g, '@role')
        .replace(/<#\d+>/g, '#channel')
        .replace(/<t:(\d+)(:\w)?>/g, (_, seconds) => new Date(seconds * 1000).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }));

// Custom emoji (<:name:id>, <a:name:id> when animated) come from discord's cdn, which serves any server's emoji by id
// Without nitro, an emoji from another server is sent as its cdn link instead, sometimes as a [name](<link>) masked link
const CUSTOM_EMOJI = /<(a?):(\w+):(\d+)>|(?:\[([^\]]*)\]\(<?)?https:\/\/(?:cdn|media)\.discordapp\.(?:com|net)\/emojis\/(\d+)\.(\w+)(\?[^\s)>]*)?(?:>?\))?/g;
const emojiPart = (match) => {
    if (match[3]) return { text: match[2], emoji: `https://cdn.discordapp.com/emojis/${match[3]}.${match[1] ? 'gif' : 'webp'}?size=96` };

    const query = new URLSearchParams(match[7] ?? '');
    const animated = match[6] === 'gif' || query.get('animated') === 'true';
    const name = query.get('name') || (match[4] ?? '').replace(/:/g, '') || 'emoji';
    return { text: name, emoji: `https://cdn.discordapp.com/emojis/${match[5]}.${animated ? 'gif' : 'webp'}?size=96` };
};

// Names come from the bot, role mentions keep the role color
const MENTION = /<@!?(\d+)>|<@&(\d+)>|<#(\d+)>/g;
const mentionPart = (match, mentions) => {
    if (match[1] && mentions?.users?.[match[1]]) return { mention: true, text: `@${mentions.users[match[1]]}` };
    const role = match[2] && mentions?.roles?.[match[2]];
    if (role) return { mention: true, text: `@${role.name}`, color: role.color && role.color !== '#000000' ? role.color : null };
    if (match[3] && mentions?.channels?.[match[3]]) return { mention: true, text: `#${mentions.channels[match[3]]}` };
    return null;
};

// Emoji and mentions become one private use character each, so the markdown below can't split them up
const TOKEN_BASE = 0xe000;
const TOKEN = /[-]/;
const tokenize = (content, mentions) => {
    const tokens = [];
    const add = (part) => String.fromCharCode(TOKEN_BASE + tokens.push(part) - 1);
    const text = content
        .replace(CUSTOM_EMOJI, (...match) => add(emojiPart(match)))
        .replace(MENTION, (whole, ...match) => {
            const part = mentionPart([whole, ...match], mentions);
            return part ? add(part) : whole;
        });
    return { text: cleanText(text), tokens };
};

// Earliest match wins, ** before * so bold isn't read as two italics
const INLINE = [
    { style: 'code', pattern: /`([^`]+)`/ },
    { style: 'spoiler', pattern: /\|\|([\s\S]+?)\|\|/ },
    { style: 'bold', pattern: /\*\*([\s\S]+?)\*\*/ },
    { style: 'underline', pattern: /__([\s\S]+?)__/ },
    { style: 'italic', pattern: /\*([^*\s](?:[^*]*[^*\s])?)\*/ },
    // snake_case isn't italic in discord either
    { style: 'italic', pattern: /(?<![\p{L}\p{N}])_([^_]+)_(?![\p{L}\p{N}])/u },
    { style: 'strike', pattern: /~~([\s\S]+?)~~/ },
];
const inlineParts = (text, tokens, classes = []) => {
    let first = null;
    for (const rule of INLINE) {
        const match = rule.pattern.exec(text);
        if (match && (!first || match.index < first.match.index)) first = { rule, match };
    }

    if (!first) {
        // Plain text, with the emoji and mentions put back
        return text
            .split(/([-])/)
            .filter((piece) => piece !== '')
            .map((piece) => {
                const token = TOKEN.test(piece) ? tokens[piece.charCodeAt(0) - TOKEN_BASE] : null;
                return { ...(token ?? { text: piece }), classes };
            });
    }

    const { rule, match } = first;
    const inner = [...classes, rule.style];
    return [
        ...inlineParts(text.slice(0, match.index), tokens, classes),
        // Code shows as is, the rest can nest
        ...(rule.style === 'code' ? [{ text: match[1], classes: inner }] : inlineParts(match[1], tokens, inner)),
        ...inlineParts(text.slice(match.index + match[0].length), tokens, classes),
    ];
};

// Headings, -# subtext, quotes and lists per line, everything after >>> is quoted
const textLines = (content, mentions) => {
    const { text, tokens } = tokenize(content, mentions);
    let quoteRest = false;
    return text.split('\n').map((raw) => {
        let line = raw;
        let kind = quoteRest ? 'quote' : '';
        if (!quoteRest && line.startsWith('>>> ')) {
            quoteRest = true;
            kind = 'quote';
            line = line.slice(4);
        } else if (line.startsWith('> ')) {
            kind = 'quote';
            line = line.slice(2);
        }

        const heading = /^(#{1,3}) (.+)/.exec(line);
        const subtext = /^-# (.+)/.exec(line);
        if (heading) {
            kind += ` heading h${heading[1].length}`;
            line = heading[2];
        } else if (subtext) {
            kind += ' subtext';
            line = subtext[1];
        }
        line = line.replace(/^(\s*)[-*] /, '$1• ');

        // Quotes are blocks, so the bar runs down every line when it wraps
        return { kind, block: kind.includes('quote'), parts: inlineParts(line, tokens) };
    });
};

// ``` blocks show as is, no markdown, emoji or mentions inside. A word right after the fence is the language, highlighted with shiki
const CODE_BLOCK = /```(?:([\w+#-]+)\n)?\n?([\s\S]*?)\n?```/g;
// Shiki's fontStyle is bit flags
const tokenStyle = (token) => ({
    color: token.color,
    fontStyle: token.fontStyle & 1 ? 'italic' : null,
    fontWeight: token.fontStyle & 2 ? 700 : null,
    textDecoration: [token.fontStyle & 4 && 'underline', token.fontStyle & 8 && 'line-through'].filter(Boolean).join(' ') || null,
});
export const toLines = (content, mentions) => {
    const lines = [];
    let last = 0;
    const addText = (text) => {
        // The newlines next to a fence would only make empty lines
        const trimmed = text.replace(/^\n/, '').replace(/\n$/, '');
        if (trimmed !== '') lines.push(...textLines(trimmed, mentions));
    };
    for (const match of content.matchAll(CODE_BLOCK)) {
        addText(content.slice(last, match.index));
        const code = match[2].split('\n');
        const tokens = highlight(match[2], match[1]);
        code.forEach((line, index) => {
            const edges = `${index === 0 ? ' first' : ''}${index === code.length - 1 ? ' last' : ''}`;
            const parts = tokens?.[index]?.length ? tokens[index].map((token) => ({ text: token.content, classes: [], style: tokenStyle(token) })) : [{ text: line || ' ', classes: [] }];
            lines.push({ kind: `codeblock${edges}`, block: true, parts });
        });
        last = match.index + match[0].length;
    }
    addText(content.slice(last));
    return lines.length ? lines : [{ kind: '', parts: [{ text: content, classes: [] }] }];
};

// One line of the replied message, no styling
export const replyText = (content) =>
    cleanText(content.replace(CUSTOM_EMOJI, (...match) => `:${emojiPart(match).text}:`))
        .replace(/\|\|[\s\S]+?\|\|/g, '[spoiler]')
        .replace(/\*\*|__|~~|`|^>+ |^-# |^#{1,3} /gm, '')
        .replace(/\s+/g, ' ')
        .trim() || '(attachment)';

// Unicode emoji too, so a message of only emoji (custom or not) gets the big ones
const EMOJI_ONLY = /^(?:\p{Extended_Pictographic}|\p{Emoji_Component}|\u200d|\ufe0f|\s)+$/u;

export const isJumbo = (content) => {
    const rest = content.replace(CUSTOM_EMOJI, '').trim();
    const hasEmoji = rest !== content.trim() || /\p{Extended_Pictographic}/u.test(rest);
    return hasEmoji && (rest === '' || EMOJI_ONLY.test(rest));
};
