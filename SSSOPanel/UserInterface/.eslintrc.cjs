/* eslint-env node */
require('@rushstack/eslint-patch/modern-module-resolution');

module.exports = {
    root: true,
    extends: [
        'plugin:vue/vue3-essential',
        'eslint:recommended',
        '@vue/eslint-config-prettier',
    ],
    parserOptions: {
        ecmaVersion: 'latest',
    },
    rules: {
        'vue/max-len': 'off',
        'max-len': 'off',
        'vue/multi-word-component-names': 'off',
        'prettier/prettier': ['error', { endOfLine: 'auto' }],
    },
};
