# Landlander Frontend

Modern React frontend application for apartment rental platform, similar to WG-Gesucht.de.

## Features

- 🏠 Apartment listings with search and filters
- 🗺️ Interactive maps (Leaflet) showing apartment locations
- 👥 Roommate finder functionality
- 💬 Real-time chat (SignalR)
- 🔔 Notifications system
- 🌙 Dark mode support
- 🌍 Multi-language support (Serbian, Russian, English, German)
- 📱 Fully responsive design
- ⭐ Review and rating system
- 🔐 Authentication (Login, Register, Forgot/Reset Password)

## Tech Stack

- **React 18** with TypeScript
- **Vite** for build tooling
- **Material-UI (MUI)** for UI components
- **React Router** for routing
- **React Query** for data fetching
- **i18next** for internationalization
- **Leaflet** for maps
- **SignalR** for real-time communication
- **Axios** for API calls

## Getting Started

### Prerequisites

- Node.js 18+ and npm/yarn
- Backend API running on `http://localhost:5197` (or configure in `.env`)

### Installation

1. Install dependencies:
```bash
npm install
```

2. Create `.env` file (or use the existing one):
```
VITE_API_URL=http://localhost:5197
VITE_SIGNALR_URL=http://localhost:5197/notificationsHub
```

3. Start development server:
```bash
npm run dev
```

The application will be available at `http://localhost:5173`

### Build for Production

```bash
npm run build
```

The built files will be in the `dist` directory.

## Project Structure

```
front-land/
├── src/
│   ├── components/       # Reusable components
│   │   ├── Layout/       # Header, Footer, Layout
│   │   ├── Map/          # Map components
│   │   ├── Notification/ # Notification components
│   │   └── ...
│   ├── pages/            # Page components
│   ├── shared/
│   │   ├── api/          # API client and endpoints
│   │   ├── context/      # React contexts (Auth, Theme, Notifications)
│   │   ├── types/        # TypeScript types
│   │   └── i18n/         # i18n configuration
│   ├── theme/            # Material-UI themes
│   └── locales/          # Translation files
├── public/               # Static assets
└── package.json
```

## Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint

## Features Overview

### Authentication
- User registration and login
- JWT token-based authentication
- Forgot/Reset password functionality
- Protected routes

### Apartment Listings
- Browse apartments with filters (price, rooms, location, features)
- Sort by price, date, rooms
- Detailed apartment view with:
  - Description
  - Interactive map (Leaflet)
  - Features and amenities
  - Reviews and ratings

### Roommates
- Find potential roommates
- Filter by preferences, budget, lifestyle
- Contact roommates

### Chat
- Real-time messaging via SignalR
- Conversation list
- Message notifications

### Notifications
- Real-time notifications via SignalR
- Notification panel
- Mark as read functionality

### Theme
- Light and dark mode
- Persistent theme preference
- Material-UI theming

### Internationalization
- Support for 4 languages:
  - Serbian (default)
  - English
  - Russian
  - German
- Language switcher in header
- Persistent language preference

## Backend Integration

The frontend expects the following backend endpoints:

- `POST /api/v1/auth/login` - User login
- `POST /api/v1/auth/register` - User registration
- `POST /api/v1/auth/logout` - User logout
- `POST /api/v1/auth/forgot-password` - Forgot password
- `POST /api/v1/auth/reset-password` - Reset password
- `GET /api/v1/rent/get-all-apartments` - Get all apartments
- `GET /api/v1/rent/get-apartment` - Get apartment by ID
- `POST /api/v1/rent/create-apartment` - Create apartment
- `GET /api/v1/reviews/*` - Review endpoints
- SignalR Hub: `/notificationsHub` - Real-time notifications

## Environment Variables

- `VITE_API_URL` - Backend API base URL (default: http://localhost:5197)
- `VITE_SIGNALR_URL` - SignalR hub URL (default: http://localhost:5197/notificationsHub)

## License

MIT

