export type Permission =
    // Apartment permissions
    | 'apartments.create'
    | 'apartments.edit.own'
    | 'apartments.edit.any'
    | 'apartments.delete.own'
    | 'apartments.delete.any'
    | 'apartments.view'
    | 'apartments.publish'
    | 'apartments.unpublish'

    // Application permissions
    | 'applications.view.own'
    | 'applications.view.received'
    | 'applications.submit'
    | 'applications.review'
    | 'applications.approve'
    | 'applications.reject'

    // Review permissions
    | 'reviews.create'
    | 'reviews.edit.own'
    | 'reviews.delete.own'
    | 'reviews.delete.any'
    | 'reviews.moderate'

    // Analytics permissions
    | 'analytics.view.personal'
    | 'analytics.view.landlord'
    | 'analytics.view.system'

    // Machine Learning permissions
    | 'analytics.ml.manage'
    | 'analytics.ml.train'

    // Message permissions
    | 'messages.send'
    | 'messages.view.own'
    | 'messages.view.all'
    | 'messages.moderate'

    // User permissions
    | 'users.view.all'
    | 'users.edit.own'
    | 'users.edit.any'
    | 'users.delete.any'
    | 'users.manage.roles'

    // Admin permissions
    | 'admin.access'
    | 'admin.manage.users'
    | 'admin.manage.content'
    | 'admin.manage.settings';

export interface PermissionDto {
    permissionId: number;
    permissionName: Permission;
    description: string;
}
