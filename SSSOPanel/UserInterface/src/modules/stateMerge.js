// state-set is merged into the panel's state (JObject.Merge), so a key the new data leaves out keeps its old value.
// Results entries leave keys out (attempts is only on replayed sets, judgements can have fewer keys), so every key
// the old value had and the new one doesn't is sent as null, which the merge does write
export const replacing = (next, previous) => {
    const isObject = (value) => value !== null && typeof value === 'object' && !Array.isArray(value);
    if (!isObject(next) || !isObject(previous)) return next;
    const out = { ...next };
    for (const key of Object.keys(previous)) out[key] = key in out ? replacing(out[key], previous[key]) : null;
    return out;
};

// For a state-set: currentMatch.results replaces the last round's instead of merging into it
export const withFreshResults = (data, state) => (data?.currentMatch?.results ? { ...data, currentMatch: { ...data.currentMatch, results: replacing(data.currentMatch.results, state?.currentMatch?.results) } } : data);
