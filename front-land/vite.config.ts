import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'
import path from 'path'
import { fileURLToPath } from 'url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.ico', 'apple-touch-icon.png', 'masked-icon.svg'],
      manifest: {
        name: 'Landlander - Apartment Rental',
        short_name: 'Landlander',
        description: 'Find your perfect apartment or roommate',
        theme_color: '#ffffff',
        icons: [
          {
            src: 'pwa-192x192.png',
            sizes: '192x192',
            type: 'image/png'
          },
          {
            src: 'pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png'
          }
        ]
      }
    })
  ],
  build: {
    rollupOptions: {
      output: {
        // Route chunks are produced automatically by the React.lazy() imports in
        // App.tsx. These vendor groups exist because the libraries below are shared
        // by most routes — without an explicit split they collapse back into the
        // entry chunk. Splitting them also keeps their hashes stable across app
        // deploys, so returning users re-download only what actually changed.
        manualChunks: {
          'vendor-react': ['react', 'react-dom', 'react-router-dom'],
          'vendor-mui': ['@mui/material', '@mui/icons-material', '@emotion/react', '@emotion/styled'],
          'vendor-map': ['leaflet', 'react-leaflet'],
          'vendor-signalr': ['@microsoft/signalr'],
          'vendor-i18n': ['i18next', 'react-i18next', 'i18next-browser-languagedetector'],
        },
      },
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@/components': path.resolve(__dirname, './src/components'),
      '@/pages': path.resolve(__dirname, './src/pages'),
      '@/shared': path.resolve(__dirname, './src/shared'),
      '@/theme': path.resolve(__dirname, './src/theme'),
      '@/locales': path.resolve(__dirname, './src/locales'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'https://localhost:7092',
        changeOrigin: true,
        secure: false, // Allow self-signed certificates
      },
      '/uploads': {
        target: 'https://localhost:7092',
        changeOrigin: true,
        secure: false, // Allow self-signed certificates
      },
      '/notificationHub': {
        target: 'https://localhost:7092',
        changeOrigin: true,
        secure: false,
        ws: true
      },
      '/chatHub': {
        target: 'https://localhost:7092',
        changeOrigin: true,
        secure: false,
        ws: true
      }
    },
  },
})

