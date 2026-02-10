import { PermissionDto } from './permission';

export type RoleName =
    | 'Admin'
    | 'Tenant'
    | 'Landlord'
    | 'TenantLandlord'
    | 'Premium Tenant'
    | 'Premium Landlord'
    | 'Moderator'
    | 'Guest';

export interface Role {
    roleId: number;
    roleName: RoleName;
    description?: string;
    permissions?: PermissionDto[];
}
