/** A working destination shared by the shell and capability-based overview. */
export interface NavigationItem {
  path: string;
  label: string;
  description: string;
  icon: string;
}

/** Additive navigation groups are presentation only; route guards and the API remain authoritative. */
export interface NavigationGroup { label: string; items: NavigationItem[] }

/** Returns only existing workflows, with one Overview even for combined-role accounts. */
export function navigationFor(roles: readonly string[]): NavigationGroup[] {
  const personal = roles.some(role => role === 'Employee' || role === 'Manager');
  const groups: NavigationGroup[] = [{ label: personal ? 'Personal' : 'Workspace', items: [
    { path: '/', label: 'Overview', description: 'Find the tools available to your account.', icon: 'home' },
    ...(personal ? [
      { path: '/leave-requests/new', label: 'Request leave', description: 'Choose your leave type and dates.', icon: 'plus' },
      { path: '/leave-requests/history', label: 'My leave and balances', description: 'Check balances, requests and recorded decisions.', icon: 'history' },
    ] : []),
  ] }];
  if (roles.includes('Manager')) groups.push({ label: 'Team', items: [
    { path: '/leave-requests/approvals', label: 'Approval queue', description: 'Decide eligible direct-report requests.', icon: 'check' },
    { path: '/team-leave', label: 'Team leave', description: 'Plan coverage with approved team leave.', icon: 'team' },
  ] });
  if (roles.includes('HR Administrator')) groups.push({ label: 'Administration', items: [
    { path: '/admin/employees', label: 'Employee management', description: 'Manage profiles, access and reporting relationships.', icon: 'team' },
    { path: '/admin/leave-policies', label: 'Leave policies/types', description: 'Manage leave types and their shared rules.', icon: 'policy' },
    { path: '/admin/leave-monitoring', label: 'Pending leave monitoring', description: 'Follow organisation-wide requests, read-only.', icon: 'history' },
    { path: '/admin/leave-reports', label: 'Department leave reports', description: 'Compare leave activity for a selected period.', icon: 'chart' },
  ] });
  groups.push({ label: 'Account', items: [
    { path: '/notifications', label: 'Notifications', description: 'Read your saved leave updates.', icon: 'history' },
    { path: '/account/password', label: 'Change password', description: 'Secure your account and end previous sessions.', icon: 'policy' },
  ] });
  return groups;
}

/** Gives detail and denied routes readable titles without adding timeline IDs to navigation. */
export function titleFor(path: string): string {
  if (/^\/leave-requests\/[^/]+\/history$/.test(path)) return 'Request history';
  return navigationFor(['Employee', 'Manager', 'HR Administrator'])
    .flatMap(group => group.items).find(item => item.path === path)?.label ?? 'Workspace';
}
