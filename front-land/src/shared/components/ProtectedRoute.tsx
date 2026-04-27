import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { usePermissions } from '../hooks/usePermissions';
import { useAuth } from '../context/AuthContext';
import { Permission } from '../types/permission';

interface ProtectedRouteProps {
    permission?: Permission;
    permissions?: Permission[];
    requireAll?: boolean;
    fallbackPath?: string;
    children: React.ReactNode;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
    permission,
    permissions,
    requireAll = false,
    fallbackPath = '/login',
    children,
}) => {
    const { isAuthenticated, loading } = useAuth();
    const { hasPermission, hasAnyPermission, hasAllPermissions } = usePermissions();
    const location = useLocation();

    // Wait for auth to initialize before redirecting
    if (loading) return null;

    if (!isAuthenticated) {
        return <Navigate to="/login" state={{ from: location }} replace />;
    }

    if (permission) {
        if (!hasPermission(permission))
            return <Navigate to={fallbackPath} replace />;
    } else if (permissions) {
        const ok = requireAll ? hasAllPermissions(permissions) : hasAnyPermission(permissions);
        if (!ok) return <Navigate to={fallbackPath} replace />;
    }

    return <>{children}</>;
};
