import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth.tsx';
import { AccessDeniedView } from './AccessDeniedView.tsx';

interface ProtectedRouteProps {
  requiredRoles?: string[];
}

/**
 * Enforces auth and optional role gates at the routing layer so protected UI never renders before checks pass.
 */
export function ProtectedRoute({ requiredRoles = [] }: ProtectedRouteProps) {
  const location = useLocation();
  const { isAuthenticated, user } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  const hasRequiredRole =
    requiredRoles.length === 0 ||
    requiredRoles.some((role) => user?.roles.includes(role));

  if (!hasRequiredRole) {
    return <AccessDeniedView />;
  }

  return <Outlet />;
}
