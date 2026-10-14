import { TeamCalendar } from '../manager-workspace/ManagerWorkspace';
import { useAuth } from '../auth/hooks/useAuth';
/** Shares the read-only calendar/list while retaining the existing Manager route guard. */
export function TeamLeavePage() {
  const { user, sessionVersion } = useAuth();
  return <TeamCalendar key={`${user?.id}:${sessionVersion}`} />;
}