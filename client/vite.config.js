import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'
import { fileURLToPath } from 'url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      // Force VTU ESM build to ensure BaseWrapper.find exists in tests
      '@vue/test-utils': path.resolve(
        __dirname,
        './node_modules/@vue/test-utils/dist/vue-test-utils.esm-bundler.mjs'
      )
    }
  },
  test: {
    globals: true,
    environment: 'happy-dom',
    setupFiles: ['./test/setup.ts'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'html'],
      reportsDirectory: './coverage',
      all: true,
      include: ['src/**/*.ts', 'src/**/*.vue'],
    },
  },
})