import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { AdminRoute } from './AdminRoute';

const auth = vi.hoisted(() => ({
  value: { isAuthenticated: false, loading: false, user: null as null | Record<string, unknown> },
}));
const perms = vi.hoisted(() => ({
  hasPermission: vi.fn(() => true),
  hasAnyPermission: vi.fn(() => true),
  hasAllPermissions: vi.fn(() => true),
}));

vi.mock('../context/AuthContext', () => ({ useAuth: () => auth.value }));
vi.mock('../hooks/usePermissions', () => ({ usePermissions: () => perms }));

const renderAt = (element: React.ReactElement) =>
  render(
    <MemoryRouter initialEntries={['/secret']}>
      <Routes>
        <Route path="/secret" element={element} />
        <Route path="/login" element={<div>LOGIN PAGE</div>} />
        <Route path="/" element={<div>HOME PAGE</div>} />
      </Routes>
    </MemoryRouter>,
  );

beforeEach(() => {
  auth.value = { isAuthenticated: false, loading: false, user: null };
  perms.hasPermission.mockReturnValue(true);
  perms.hasAnyPermission.mockReturnValue(true);
  perms.hasAllPermissions.mockReturnValue(true);
});

describe('ProtectedRoute', () => {
  it('renders nothing while auth is still initialising (no premature redirect)', () => {
    auth.value = { isAuthenticated: false, loading: true, user: null };
    const { container } = renderAt(<ProtectedRoute><div>SECRET</div></ProtectedRoute>);
    expect(container).toBeEmptyDOMElement();
  });

  it('sends anonymous visitors to /login', () => {
    renderAt(<ProtectedRoute><div>SECRET</div></ProtectedRoute>);
    expect(screen.getByText('LOGIN PAGE')).toBeInTheDocument();
    expect(screen.queryByText('SECRET')).not.toBeInTheDocument();
  });

  it('shows the page to an authenticated user', () => {
    auth.value = { isAuthenticated: true, loading: false, user: { userId: 1 } };
    renderAt(<ProtectedRoute><div>SECRET</div></ProtectedRoute>);
    expect(screen.getByText('SECRET')).toBeInTheDocument();
  });

  it('redirects an authenticated user who lacks the required permission', () => {
    auth.value = { isAuthenticated: true, loading: false, user: { userId: 1 } };
    perms.hasPermission.mockReturnValue(false);
    renderAt(
      <ProtectedRoute permission={'manage_listings' as never}>
        <div>SECRET</div>
      </ProtectedRoute>,
    );
    expect(screen.getByText('LOGIN PAGE')).toBeInTheDocument();
    expect(screen.queryByText('SECRET')).not.toBeInTheDocument();
  });
});

describe('AdminRoute', () => {
  it('sends anonymous visitors to /login', () => {
    renderAt(<AdminRoute><div>ADMIN</div></AdminRoute>);
    expect(screen.getByText('LOGIN PAGE')).toBeInTheDocument();
  });

  it('keeps ordinary users out (redirects home)', () => {
    auth.value = { isAuthenticated: true, loading: false, user: { userRoleId: 3, roleName: 'Tenant' } };
    renderAt(<AdminRoute><div>ADMIN</div></AdminRoute>);
    expect(screen.getByText('HOME PAGE')).toBeInTheDocument();
    expect(screen.queryByText('ADMIN')).not.toBeInTheDocument();
  });

  it('lets an admin in, identified by role NAME whatever their numeric role id is', () => {
    auth.value = { isAuthenticated: true, loading: false, user: { userRoleId: 99, roleName: 'Admin' } };
    renderAt(<AdminRoute><div>ADMIN</div></AdminRoute>);
    expect(screen.getByText('ADMIN')).toBeInTheDocument();
  });

  it('does NOT treat role id 1 as admin — ids differ between databases (a database built from migrations has Tenant = 1)', () => {
    auth.value = { isAuthenticated: true, loading: false, user: { userRoleId: 1, roleName: 'Tenant' } };
    renderAt(<AdminRoute><div>ADMIN</div></AdminRoute>);
    expect(screen.getByText('HOME PAGE')).toBeInTheDocument();
    expect(screen.queryByText('ADMIN')).not.toBeInTheDocument();
  });
});
