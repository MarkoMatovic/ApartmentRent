import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate, useLocation } from 'react-router-dom';
import { AuthProvider } from './shared/context/AuthContext';
import { ThemeProvider } from './shared/context/ThemeContext';
import { NotificationProvider } from './shared/context/NotificationContext';
import Layout from './components/Layout/Layout';
import ErrorBoundary from './components/ErrorBoundary';
import ScrollToTop from './components/ScrollToTop';
import HomePage from './pages/HomePage';
import ApartmentListPage from './pages/ApartmentListPage';
import ApartmentDetailPage from './pages/ApartmentDetailPage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import ForgotPasswordPage from './pages/ForgotPasswordPage';
import ResetPasswordPage from './pages/ResetPasswordPage';
import ChatPage from './pages/ChatPage';
import RoommateListPage from './pages/RoommateListPage';
import RoommateDetailPage from './pages/RoommateDetailPage';
import CreateRoommatePage from './pages/CreateRoommatePage';
import ProfilePage from './pages/ProfilePage';
import MyApartmentsPage from './pages/MyApartmentsPage';
import CreateApartmentPage from './pages/CreateApartmentPage';
import EditApartmentPage from './pages/EditApartmentPage';
import AnalyticsDashboardPage from './pages/AnalyticsDashboardPage';
import SupportPage from './pages/SupportPage';
import PricingPage from './pages/PricingPage';
import SubscriptionPage from './pages/SubscriptionPage';
import UserRoommateAnalyticsPage from './pages/UserRoommateAnalyticsPage';
import TenantApplicationsPage from './pages/TenantApplicationsPage';
import LandlordApplicationsPage from './pages/LandlordApplicationsPage';
import PaymentSuccessPage from './pages/PaymentSuccessPage';
import PaymentFailurePage from './pages/PaymentFailurePage';
import MyAppointmentsPage from './pages/MyAppointmentsPage';
import LandlordAppointmentsPage from './pages/LandlordAppointmentsPage';
import LandlordAvailabilityPage from './pages/LandlordAvailabilityPage';
import MessagesPage from './pages/MessagesPage';
import ReportsPage from './pages/ReportsPage';
import { SavedSearchesPage } from './pages/SavedSearches';
import { SearchRequestsPage } from './pages/SearchRequests';
import { PricePredictorPage } from './pages/PricePredictor';
import RoommateMatchesPage from './pages/RoommateMatchesPage';
import { AdminRoute } from './shared/components/AdminRoute';
import { ProtectedRoute } from './shared/components/ProtectedRoute';

/**
 * Wraps the route tree in an ErrorBoundary that automatically resets whenever
 * the user navigates to a different path.  This means a crash on page A is
 * isolated — navigating to page B clears the error state without a full reload.
 * Must be rendered inside <Router> so that useLocation() is available.
 */
const PageErrorBoundary: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const location = useLocation();
  return <ErrorBoundary key={location.pathname}>{children}</ErrorBoundary>;
};

function App() {
  return (
    <ErrorBoundary>
      <ThemeProvider>
        <AuthProvider>
          <NotificationProvider>
            <Router>
              <ScrollToTop />
              <Layout>
              <PageErrorBoundary>
              <Routes>
                <Route path="/" element={<HomePage />} />
                <Route path="/apartments" element={<ApartmentListPage />} />
                <Route path="/apartments/:id" element={<ApartmentDetailPage />} />
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />
                <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                <Route path="/reset-password/:token" element={<ResetPasswordPage />} />
                <Route path="/chat" element={<ProtectedRoute><ChatPage /></ProtectedRoute>} />
                <Route path="/messages" element={<ProtectedRoute><MessagesPage /></ProtectedRoute>} />
                <Route path="/roommates" element={<RoommateListPage />} />
                <Route path="/roommates/create" element={<ProtectedRoute><CreateRoommatePage /></ProtectedRoute>} />
                <Route path="/roommates/matches" element={<ProtectedRoute><RoommateMatchesPage /></ProtectedRoute>} />
                <Route path="/roommates/:id" element={<RoommateDetailPage />} />
                <Route path="/profile" element={<ProtectedRoute><ProfilePage /></ProtectedRoute>} />
                <Route path="/my-apartments" element={<ProtectedRoute><MyApartmentsPage /></ProtectedRoute>} />
                <Route path="/apartments/create" element={<ProtectedRoute><CreateApartmentPage /></ProtectedRoute>} />
                <Route path="/apartments/edit/:id" element={<ProtectedRoute><EditApartmentPage /></ProtectedRoute>} />
                <Route path="/admin/analytics" element={<AdminRoute><AnalyticsDashboardPage /></AdminRoute>} />
                <Route path="/admin/reports" element={<AdminRoute><ReportsPage /></AdminRoute>} />
                <Route path="/support" element={<SupportPage />} />
                <Route path="/pricing" element={<PricingPage />} />
                <Route path="/subscription" element={<ProtectedRoute><SubscriptionPage /></ProtectedRoute>} />
                <Route path="/analytics/roommate" element={<ProtectedRoute><UserRoommateAnalyticsPage /></ProtectedRoute>} />
                <Route path="/applications/sent" element={<ProtectedRoute><TenantApplicationsPage /></ProtectedRoute>} />
                <Route path="/applications/received" element={<ProtectedRoute><LandlordApplicationsPage /></ProtectedRoute>} />
                <Route path="/appointments/my" element={<ProtectedRoute><MyAppointmentsPage /></ProtectedRoute>} />
                <Route path="/appointments/manage" element={<ProtectedRoute><LandlordAppointmentsPage /></ProtectedRoute>} />
                <Route path="/appointments/availability" element={<ProtectedRoute><LandlordAvailabilityPage /></ProtectedRoute>} />
                <Route path="/payment-success" element={<PaymentSuccessPage />} />
                <Route path="/payment-failure" element={<PaymentFailurePage />} />
                <Route path="/saved-searches" element={<ProtectedRoute><SavedSearchesPage /></ProtectedRoute>} />
                <Route path="/search-requests" element={<ProtectedRoute><SearchRequestsPage /></ProtectedRoute>} />
                <Route path="/price-predictor" element={<PricePredictorPage />} />
                <Route path="/unauthorized" element={
                  <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: '60vh', gap: 16 }}>
                    <h2>Access Denied</h2>
                    <p>You don't have permission to view this page.</p>
                    <button onClick={() => window.history.back()}>Go back</button>
                  </div>
                } />
                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
              </PageErrorBoundary>
              </Layout>
            </Router>
          </NotificationProvider>
        </AuthProvider>
      </ThemeProvider>
    </ErrorBoundary>
  );
}

export default App;

