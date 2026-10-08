import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

interface AdminRouteProps {
    children: React.ReactNode;
}

/**
 * AdminRoute component - wraps routes that require Admin role
 *
 * Redirects unauthenticated users to /login and non-admin users to /.
 */
export const AdminRoute: React.FC<AdminRouteProps> = ({ children }) => {
    const { user, loading } = useAuth();

    if (loading) return null;
    if (!user) return <Navigate to="/login" replace />;
    // By role NAME, never by numeric id: ids differ between databases (a database built from
    // migrations has Tenant = 1), so `userRoleId === 1` would admit ordinary tenants there.
    if (user.roleName !== 'Admin') return <Navigate to="/" replace />;

    return <>{children}</>;
};
