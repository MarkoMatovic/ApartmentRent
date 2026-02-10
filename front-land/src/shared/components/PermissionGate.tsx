import React from 'react';
import { usePermissions } from '../hooks/usePermissions';
import { Permission } from '../types/permission';

interface PermissionGateProps {
    permission?: Permission;
    permissions?: Permission[];
    requireAll?: boolean;
    fallback?: React.ReactNode;
    children: React.ReactNode;
}

/**
 * PermissionGate component - conditionally renders children based on permissions
 * 
 * Usage:
 * <PermissionGate permission="apartments.edit.own">
 *   <Button>Edit Apartment</Button>
 * </PermissionGate>
 */
export const PermissionGate: React.FC<PermissionGateProps> = ({
    permission,
    permissions,
    requireAll = false,
    fallback = null,
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
        // No permission required, always show
        authorized = true;
    }

    return authorized ? <>{children}</> : <>{fallback}</>;
};
