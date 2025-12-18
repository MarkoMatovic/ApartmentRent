# Landlord Frontend

React + TypeScript frontend application integrated with the backend API.

## Tech Stack

- React 18
- TypeScript
- Vite
- Material UI (MUI v5)
- React Router
- Axios
- TanStack Query (React Query)
- Zustand (State Management)

## Setup

1. Install dependencies:
```bash
npm install
```

2. Create a `.env` file in the root directory:
```
VITE_API_BASE_URL=http://localhost:5197
```

3. Start the development server:
```bash
npm run dev
```

The application will be available at `http://localhost:5173`

## Backend Integration

- All API calls are made through centralized services in `/src/features/*/api/`
- Authentication uses JWT tokens stored in localStorage
- No mock data - all data comes from the backend API
- Proper loading, error, and empty states implemented

## Project Structure

```
src/
├── app/              # App-level configuration (router, providers)
├── features/         # Feature modules
│   ├── apartments/   # Apartment listings
│   ├── auth/         # Authentication
│   ├── notifications/# Notifications
│   └── reviews/      # Reviews and favorites
├── shared/           # Shared utilities and components
│   ├── api/          # API client configuration
│   ├── components/   # Reusable UI components
│   └── types/        # Shared TypeScript types
└── main.tsx          # Application entry point
```

## Environment Variables

- `VITE_API_BASE_URL`: Backend API base URL (default: http://localhost:5197)

