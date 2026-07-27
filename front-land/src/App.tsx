import React, { Suspense, lazy } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate, useLocation } from 'react-router-dom';
import { AuthProvider } from './shared/context/AuthContext';
import { ThemeProvider } from './shared/context/ThemeContext';
import { NotificationProvider } from './shared/context/NotificationContext';
import Layout from './components/Layout/Layout';
import ErrorBoundary from './components/ErrorBoundary';
import ScrollToTop from './components/ScrollToTop';
import PageLoader from './components/PageLoader';
import { AdminRoute } from './shared/components/AdminRoute';
import { ProtectedRoute } from './shared/components/ProtectedRoute';
import CookieConsentBanner from './components/CookieConsent/CookieConsentBanner';

// HomePage stays eager — it is the landing route, so lazy-loading it would only
// add a round trip before first paint. Every other page is split into its own
// chunk and fetched on navigation.
import HomePage from './pages/HomePage';

const ApartmentListPage = lazy(() => import('./pages/ApartmentListPage'));
const ApartmentDetailPage = lazy(() => import('./pages/ApartmentDetailPage'));
const LoginPage = lazy(() => import('./pages/LoginPage'));
const RegisterPage = lazy(() => import('./pages/RegisterPage'));
const ForgotPasswordPage = lazy(() => import('./pages/ForgotPasswordPage'));
const ResetPasswordPage = lazy(() => import('./pages/ResetPasswordPage'));
const ChatPage = lazy(() => import('./pages/ChatPage'));
const RoommateListPage = lazy(() => import('./pages/RoommateListPage'));
const RoommateDetailPage = lazy(() => import('./pages/RoommateDetailPage'));
const CreateRoommatePage = lazy(() => import('./pages/CreateRoommatePage'));
const ProfilePage = lazy(() => import('./pages/ProfilePage'));
const MyApartmentsPage = lazy(() => import('./pages/MyApartmentsPage'));
const CreateApartmentPage = lazy(() => import('./pages/CreateApartmentPage'));
const EditApartmentPage = lazy(() => import('./pages/EditApartmentPage'));
const AnalyticsDashboardPage = lazy(() => import('./pages/AnalyticsDashboardPage'));
const SupportPage = lazy(() => import('./pages/SupportPage'));
const PricingPage = lazy(() => import('./pages/PricingPage'));
const SubscriptionPage = lazy(() => import('./pages/SubscriptionPage'));
const UserRoommateAnalyticsPage = lazy(() => import('./pages/UserRoommateAnalyticsPage'));
const TenantApplicationsPage = lazy(() => import('./pages/TenantApplicationsPage'));
const LandlordApplicationsPage = lazy(() => import('./pages/LandlordApplicationsPage'));
const PaymentSuccessPage = lazy(() => import('./pages/PaymentSuccessPage'));
const PaymentFailurePage = lazy(() => import('./pages/PaymentFailurePage'));
const MyAppointmentsPage = lazy(() => import('./pages/MyAppointmentsPage'));
const LandlordAppointmentsPage = lazy(() => import('./pages/LandlordAppointmentsPage'));
const LandlordAvailabilityPage = lazy(() => import('./pages/LandlordAvailabilityPage'));
const MessagesPage = lazy(() => import('./pages/MessagesPage'));
const ReportsPage = lazy(() => import('./pages/ReportsPage'));
const RoommateMatchesPage = lazy(() => import('./pages/RoommateMatchesPage'));

// These modules expose named exports only, so map them onto `default` for lazy().
const SavedSearchesPage = lazy(() =>
  import('./pages/SavedSearches').then(m => ({ default: m.SavedSearchesPage })));
const SearchRequestsPage = lazy(() =>
  import('./pages/SearchRequests').then(m => ({ default: m.SearchRequestsPage })));
const PricePredictorPage = lazy(() =>
  import('./pages/PricePredictor').then(m => ({ default: m.PricePredictorPage })));

// Legal pages
const PrivacyPolicyPage = lazy(() => import('./pages/legal/PrivacyPolicyPage'));
const TermsOfServicePage = lazy(() => import('./pages/legal/TermsOfServicePage'));
const CookiePolicyPage = lazy(() => import('./pages/legal/CookiePolicyPage'));
const RefundPolicyPage = lazy(() => import('./pages/legal/RefundPolicyPage'));

// Payment management pages
const MySubscriptionsPage = lazy(() => import('./pages/MySubscriptionsPage'));
const PaymentHistoryPage = lazy(() => import('./pages/PaymentHistoryPage'));

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
              <Suspense fallback={<PageLoader />}>
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
                {/* Legal pages — required by ZET and ZZPL */}
                <Route path="/politika-privatnosti" element={<PrivacyPolicyPage />} />
                <Route path="/uslovi-koriscenja" element={<TermsOfServicePage />} />
                <Route path="/politika-kolacica" element={<CookiePolicyPage />} />
                <Route path="/politika-povracaja" element={<RefundPolicyPage />} />
                {/* Payment management */}
                <Route path="/moje-pretplate" element={<ProtectedRoute><MySubscriptionsPage /></ProtectedRoute>} />
                <Route path="/istorija-placanja" element={<ProtectedRoute><PaymentHistoryPage /></ProtectedRoute>} />
                <Route path="/unauthorized" element={
                  <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: '60vh', gap: 16 }}>
                    <h2>Access Denied</h2>
                    <p>You don't have permission to view this page.</p>
                    <button onClick={() => window.history.back()}>Go back</button>
                  </div>
                } />
                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
              </Suspense>
              </PageErrorBoundary>
              </Layout>
              <CookieConsentBanner />
            </Router>
          </NotificationProvider>
        </AuthProvider>
      </ThemeProvider>
    </ErrorBoundary>
  );
}

export default App;
