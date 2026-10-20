import { deferredFeature } from './components/DeferredFeature';
import { ApplicationShell } from './components/layout/ApplicationShell';
import { Navigate, Route, Routes } from 'react-router-dom'
import { HomePage } from './features/auth/components/HomePage.tsx'
import { LoginPage } from './features/auth/components/LoginPage.tsx'
import { ProtectedRoute } from './features/auth/components/ProtectedRoute.tsx'


const CsvOnboardingPage = deferredFeature(() => import('./features/onboarding/CsvOnboardingPage'), 'CsvOnboardingPage', 'CSV onboarding');
const ProfilePage = deferredFeature(() => import('./features/account/AccountPages'), 'ProfilePage', 'Profile');
const SettingsPage = deferredFeature(() => import('./features/account/AccountPages'), 'SettingsPage', 'Settings');
const NotificationsPage = deferredFeature(() => import('./features/notifications/NotificationsPage'), 'NotificationsPage', 'Notifications');
const ChangePasswordPage = deferredFeature(() => import('./features/auth/components/ChangePasswordPage'), 'ChangePasswordPage', 'Change password');
const ActivationPage = deferredFeature(() => import('./features/auth/components/ActivationPage'), 'ActivationPage', 'Account activation');
const LeaveRequestTimelinePage = deferredFeature(() => import('./features/leave-timeline/LeaveRequestTimelinePage'), 'LeaveRequestTimelinePage', 'Request history');
const LeaveConfigurationPage = deferredFeature(() => import('./features/leave-configuration/LeaveConfigurationPage'), 'LeaveConfigurationPage', 'Leave policies');
const TeamLeavePage = deferredFeature(() => import('./features/team-leave/TeamLeavePage'), 'TeamLeavePage', 'Team leave');
const RequestLeavePage = deferredFeature(() => import('./features/leave-requests/components/RequestLeavePage'), 'RequestLeavePage', 'Request leave');
const EmployeeManagementPage = deferredFeature(() => import('./features/employees/components/EmployeeManagementPage'), 'EmployeeManagementPage', 'Employee management');
const ManagerApprovalQueuePage = deferredFeature(() => import('./features/leave-requests/components/ManagerApprovalQueuePage'), 'ManagerApprovalQueuePage', 'Approval queue');
const EmployeeLeaveHistoryPage = deferredFeature(() => import('./features/leave-requests/components/EmployeeLeaveHistoryPage'), 'EmployeeLeaveHistoryPage', 'My leave');
const HrPendingLeaveMonitoringPage = deferredFeature(() => import('./features/leave-requests/components/HrPendingLeaveMonitoringPage'), 'HrPendingLeaveMonitoringPage', 'Pending monitoring');
const LeaveReportsPage = deferredFeature(() => import('./features/leave-reports/LeaveReportsPage'), 'LeaveReportsPage', 'Leave reports');

/** Keeps one stable shell beneath authentication and preserves each existing capability guard. */
function App() {
  return <Routes>
    <Route path="/activate" element={<ActivationPage />} />
    <Route path="/login" element={<LoginPage />} />
    <Route element={<ProtectedRoute />}>
      <Route element={<ApplicationShell />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/notifications" element={<NotificationsPage />} />
        <Route path="/account/profile" element={<ProfilePage />} />
        <Route path="/account/settings" element={<SettingsPage />} />
        <Route path="/account/password" element={<ChangePasswordPage />} />
        <Route element={<ProtectedRoute requiredRoles={['Employee', 'Manager', 'HR Administrator']} />}>
          <Route path="/leave-requests/:id/history" element={<LeaveRequestTimelinePage />} />
        </Route>
        <Route element={<ProtectedRoute requiredRoles={['Employee', 'Manager']} />}>
          <Route path="/leave-requests/new" element={<RequestLeavePage />} />
          <Route path="/leave-requests/history" element={<EmployeeLeaveHistoryPage />} />
        </Route>
        <Route element={<ProtectedRoute requiredRoles={['HR Administrator']} />}>
          <Route path="/admin/leave-reports" element={<LeaveReportsPage />} />
          <Route path="/admin/leave-policies" element={<LeaveConfigurationPage />} />
          <Route path="/admin/onboarding" element={<CsvOnboardingPage />} />
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
