import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './shared/context/AuthContext';
import { ThemeProvider } from './shared/context/ThemeContext';
import { NotificationProvider } from './shared/context/NotificationContext';
import Layout from './components/Layout/Layout';
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

function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <NotificationProvider>
          <Router>
            <Layout>
              <Routes>
                <Route path="/" element={<HomePage />} />
                <Route path="/apartments" element={<ApartmentListPage />} />
                <Route path="/apartments/create" element={<CreateApartmentPage />} />
                <Route path="/apartments/edit/:id" element={<EditApartmentPage />} />
                <Route path="/apartments/:id" element={<ApartmentDetailPage />} />
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />
                <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                <Route path="/reset-password/:token" element={<ResetPasswordPage />} />
                <Route path="/messages" element={<ChatPage />} />
                <Route path="/roommates" element={<RoommateListPage />} />
                <Route path="/roommates/:id" element={<RoommateDetailPage />} />
                <Route path="/roommates/create" element={<CreateRoommatePage />} />
                <Route path="/profile" element={<ProfilePage />} />
                <Route path="/my-apartments" element={<MyApartmentsPage />} />
                <Route path="/admin/analytics" element={<AnalyticsDashboardPage />} />
                <Route path="/support" element={<SupportPage />} />
                <Route path="/pricing" element={<PricingPage />} />
                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
            </Layout>
          </Router>
        </NotificationProvider>
      </AuthProvider>
    </ThemeProvider>
  );
}

export default App;

