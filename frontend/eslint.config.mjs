/* eslint-env node */
import js from '@eslint/js'
import tseslint from 'typescript-eslint'
import pluginVue from 'eslint-plugin-vue'
import prettier from 'eslint-config-prettier'
import vueParser from 'vue-eslint-parser'

export default [
  // 忽略
  { ignores: ['dist', 'node_modules'] },

  // JS 基礎
  js.configs.recommended,

  // TS 推薦
  ...tseslint.configs.recommended,

  // Vue 3 推薦（flat）
  ...pluginVue.configs['flat/recommended'],

  // 與 Prettier 衝突的規則關閉
  prettier,

  // 針對 .vue：用 vue-eslint-parser，並把 <script lang="ts"> 交給 TS parser
  {
    files: ['**/*.vue'],
    languageOptions: {
      parser: vueParser,
      parserOptions: {
        parser: tseslint.parser, // 內層 <script> 用 TS parser
        ecmaVersion: 'latest',
        sourceType: 'module',
        extraFileExtensions: ['.vue']
      }
    },
    rules: {
      'vue/multi-word-component-names': 'off',
      '@typescript-eslint/no-explicit-any': 'off'
    }
  },

  // 針對 .ts：補上通用語法設定（可選，但清楚）
  {
    files: ['**/*.ts'],
    languageOptions: {
      parser: tseslint.parser,
      parserOptions: { ecmaVersion: 'latest', sourceType: 'module' }
    }
  }
]
