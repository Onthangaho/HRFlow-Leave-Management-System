import { ApplicationShell } from './components/layout/ApplicationShell';
import { LeaveRequestTimelinePage } from './features/leave-timeline/LeaveRequestTimelinePage';
import { LeaveConfigurationPage } from './features/leave-configuration/LeaveConfigurationPage';
import { lazy, Suspense } from 'react';
import { TeamLeavePage } from './features/team-leave/TeamLeavePage';
import { RequestLeavePage } from './features/leave-requests/components/RequestLeavePage';
import { Navigate, Route, Routes } from 'react-router-dom'
import { HomePage } from './features/auth/components/HomePage.tsx'
import { LoginPage } from './features/auth/components/LoginPage.tsx'
import { ProtectedRoute } from './features/auth/components/ProtectedRoute.tsx'
import { EmployeeManagementPage } from './features/employees/components/EmployeeManagementPage';
import { ManagerApprovalQueuePage } from './features/leave-requests/components/ManagerApprovalQueuePage.tsx';
import { EmployeeLeaveHistoryPage } from './features/leave-requests/components/EmployeeLeaveHistoryPage.tsx';
import { HrPendingLeaveMonitoringPage } from './features/leave-requests/components/HrPendingLeaveMonitoringPage.tsx';

const LeaveReportsPage = lazy(() => import('./features/leave-reports/LeaveReportsPage').then(module => ({ default: module.LeaveReportsPage })));

/** Keeps one stable shell beneath authentication and preserves each existing capability guard. */
function App() {
  return <Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route element={<ProtectedRoute />}>
      <Route element={<ApplicationShell />}>
        <Route path="/" element={<HomePage />} />
        <Route element={<ProtectedRoute requiredRoles={['Employee', 'Manager', 'HR Administrator']} />}>
          <Route path="/leave-requests/:id/history" element={<LeaveRequestTimelinePage />} />
        </Route>
        <Route element={<ProtectedRoute requiredRoles={['Employee', 'Manager']} />}>
          <Route path="/leave-requests/new" element={<RequestLeavePage />} />
          <Route path="/leave-requests/history" element={<EmployeeLeaveHistoryPage />} />
        </Route>
        <Route element={<ProtectedRoute requiredRoles={['HR Administrator']} />}>
          <Route path="/admin/leave-reports" element={<Suspense fallback={<p role="status" className="ui-panel">Loading leave reports…</p>}><LeaveReportsPage /></Suspense>} />
          <Route path="/admin/leave-policies" element={<LeaveConfigurationPage />} />
          <Route path="/admin/employees" element={<EmployeeManagementPage />} />
          <Route path="/admin/leave-monitoring" element={<HrPendingLeaveMonitoringPage />} />
        </Route>
        <Route element={<ProtectedRoute requiredRoles={['Manager']} />}>
          <Route path="/team-leave" element={<TeamLeavePage />} />
          <Route path="/leave-requests/approvals" element={<ManagerApprovalQueuePage />} />
        </Route>
      </Route>
    </Route>
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes>;
}

export default App;
