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

