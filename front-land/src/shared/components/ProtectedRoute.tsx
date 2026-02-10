import React from 'react';
import { Navigate } from 'react-router-dom';
import { usePermissions } from '../hooks/usePermissions';
import { Permission } from '../types/permission';

interface ProtectedRouteProps {
    permission?: Permission;
    permissions?: Permission[];
    requireAll?: boolean;
    fallbackPath?: string;
    children: React.ReactNode;
}

/**
 * ProtectedRoute component - wraps routes that require specific permissions
 * 
 * Usage:
 * <ProtectedRoute permission="apartments.create">
 *   <CreateApartmentPage />
 * </ProtectedRoute>
 */
export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
    permission,
    permissions,
    requireAll = false,
    fallbackPath = '/unauthorized',
    children,
}) => {
    const { hasPermission, hasAnyPermission, hasAllPermissions } = usePermissions();

    let authorized = false;

    if (permission) {
        authorized = hasPermission(permission);
    } else if (permissions) {
        authorized = requireAll
            ? hasAllPermissions(permissions)
            : hasAnyPermission(permissions);
    } else {
        // No permission required, always authorized
        authorized = true;
    }

    if (!authorized) {
        return <Navigate to={fallbackPath} replace />;
    }

    return <>{children}</>;
};
