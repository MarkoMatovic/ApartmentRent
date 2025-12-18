import { createBrowserRouter, Navigate } from 'react-router-dom';
import { useAuthStore } from '@/features/auth/store/authStore';
import LoginPage from '@/features/auth/pages/LoginPage';
import RegisterPage from '@/features/auth/pages/RegisterPage';
import HomePage from '@/features/apartments/pages/HomePage';
import ApartmentsListPage from '@/features/apartments/pages/ApartmentsListPage';
import ApartmentsPage from '@/features/apartments/pages/ApartmentsPage';
import ApartmentDetailPage from '@/features/apartments/pages/ApartmentDetailPage';
import ApplicationsPage from '@/features/apartments/pages/ApplicationsPage';
import NotificationsPage from '@/features/notifications/pages/NotificationsPage';
import ReviewsPage from '@/features/reviews/pages/ReviewsPage';

// Protected Route Component
// TEMPORARY: Auth disabled for development - bypassing all auth checks
const ProtectedRoute = ({ children }: { children: React.ReactNode }) => {
  // const { isAuthenticated } = useAuthStore.getState();
  // return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
  return <>{children}</>; // TEMPORARY: Always allow access
};

// Public Route Component (redirect if authenticated)
// TEMPORARY: Auth disabled for development - bypassing all auth checks
const PublicRoute = ({ children }: { children: React.ReactNode }) => {
  // const { isAuthenticated } = useAuthStore.getState();
  // return !isAuthenticated ? <>{children}</> : <Navigate to="/" replace />;
  return <>{children}</>; // TEMPORARY: Always allow access
};

export const router = createBrowserRouter([
  {
    path: '/login',
    element: (
      <PublicRoute>
        <LoginPage />
      </PublicRoute>
    ),
  },
  {
    path: '/register',
    element: (
      <PublicRoute>
        <RegisterPage />
      </PublicRoute>
    ),
  },
  {
    path: '/',
    element: <HomePage />,
  },
  {
    path: '/apartments',
    element: <ApartmentsPage />,
  },
  {
    path: '/apartments/:id',
    element: <ApartmentDetailPage />,
  },
  {
    path: '/apartments/:id/applications',
    element: (
      <ProtectedRoute>
        <ApplicationsPage />
      </ProtectedRoute>
    ),
  },
  {
    path: '/applications',
    element: (
      <ProtectedRoute>
        <ApplicationsPage />
      </ProtectedRoute>
    ),
  },
  {
    path: '/messages',
    element: (
      <ProtectedRoute>
        <NotificationsPage />
      </ProtectedRoute>
    ),
  },
  {
    path: '/roommates',
    element: (
      <ProtectedRoute>
        <ReviewsPage />
      </ProtectedRoute>
    ),
  },
  {
    path: '/notifications',
    element: (
      <ProtectedRoute>
        <NotificationsPage />
      </ProtectedRoute>
    ),
  },
  {
    path: '/reviews',
    element: (
      <ProtectedRoute>
        <ReviewsPage />
      </ProtectedRoute>
    ),
  },
  {
    path: '*',
    element: <Navigate to="/" replace />,
  },
]);

