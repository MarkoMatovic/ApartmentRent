import { useAuth } from '../context/AuthContext';
import { Permission } from '../types/permission';

export const usePermissions = () => {
    const { user } = useAuth();

    /**
     * Check if the user has a specific permission
     */
    const hasPermission = (permission: Permission): boolean => {
        if (!user || !user.permissions) {
            return false;
        }
        return user.permissions.includes(permission);
    };

    /**
     * Check if the user has ANY of the specified permissions
     */
    const hasAnyPermission = (permissions: Permission[]): boolean => {
        if (!user || !user.permissions || permissions.length === 0) {
            return false;
        }
        return permissions.some(permission => user.permissions!.includes(permission));
    };

    /**
     * Check if the user has ALL of the specified permissions
     */
    const hasAllPermissions = (permissions: Permission[]): boolean => {
        if (!user || !user.permissions || permissions.length === 0) {
            return false;
        }
        return permissions.every(permission => user.permissions!.includes(permission));
    };

    /**
     * Check if the user has a specific role
     */
    const hasRole = (roleName: string): boolean => {
        return user?.roleName === roleName;
    };

    /**
     * Check if the user is Premium (Premium Tenant or Premium Landlord)
     */
    const isPremium = (): boolean => {
        return user?.roleName === 'Premium Tenant' || user?.roleName === 'Premium Landlord';
    };

    return {
        hasPermission,
        hasAnyPermission,
        hasAllPermissions,
        hasRole,
        isPremium,
        permissions: user?.permissions || [],
        roleName: user?.roleName,
    };
};
