import { Navigate, Route, Routes } from 'react-router-dom'
import { HomePage } from './features/auth/components/HomePage.tsx'
import { LoginPage } from './features/auth/components/LoginPage.tsx'
import { ProtectedRoute } from './features/auth/components/ProtectedRoute.tsx'
import { EmployeeManagementPage } from './features/employees/components/EmployeeManagementPage';

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/" element={<HomePage />} />
      </Route>

      <Route element={<ProtectedRoute requiredRoles={['HR Administrator']} />}>
        <Route path="/admin" element={<HomePage />} />
        <Route element={<ProtectedRoute requiredRoles={['HR Administrator']} />}>
  <Route path="/admin/employees" element={<EmployeeManagementPage />} />
</Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App