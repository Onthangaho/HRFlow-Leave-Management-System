import { isAxiosError } from 'axios';
import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';
import { problemMessage } from '../leave-configuration/problems';

/** One owner-scoped server snapshot; balance values never originate from client calculations. */
export interface PersonalDashboardData {
 asOfUtc: string;
 basis: { timeZone: string; unit: string; balanceBasis: string; today: string; upcomingStart: string; upcomingEnd: string;
  inclusive: boolean; pendingPreviewLimit: number; upcomingLimit: number; recentLimit: number };
 balances: { leaveTypeId: string; leaveTypeName: string; entitledDays: number; usedDays: number; remainingDays: number }[];
 pendingCount: number; pending: DashboardRequest[]; upcoming: DashboardRequest[]; recent: DashboardRequest[];
}
/** Summary deliberately excludes descriptions, evidence, personnel reasons and decision notes. */
export interface DashboardRequest { id: string; leaveTypeName: string; startDate: string; endDate: string; status: string; lastActivityUtc: string | null }
const refreshInterval = 60_000;
const calendarDate = (date: string) => new Intl.DateTimeFormat('en-ZA',{dateStyle:'medium',timeZone:'UTC'}).format(new Date(`${date}T00:00:00Z`));
const utcTimestamp = (date: string) => `${new Intl.DateTimeFormat('en-ZA',{dateStyle:'medium',timeStyle:'short',timeZone:'UTC'}).format(new Date(date))} UTC`;

/** Refreshes the visible owner session without placeholder data from another account or login epoch. */
function usePersonalDashboard() {
 const { user,sessionVersion } = useAuth();
 const [visible,setVisible] = useState(()=>document.visibilityState==='visible');
 useEffect(()=>{const update=()=>setVisible(document.visibilityState==='visible');document.addEventListener('visibilitychange',update);return()=>document.removeEventListener('visibilitychange',update);},[]);
 return useQuery({queryKey:['personal-leave-dashboard',user?.id,sessionVersion],
  enabled:Boolean(user?.id && user.roles.some(role=>role==='Employee'||role==='Manager')),
  queryFn:async({signal})=>(await authHttpClient.get<PersonalDashboardData>('/leave-requests/dashboard',{signal})).data,
  retry:false,staleTime:0,refetchOnMount:'always',refetchOnWindowFocus:true,
  refetchInterval:query=>visible && !(isAxiosError(query.state.error) && query.state.error.response?.status===403) ? refreshInterval : false,refetchIntervalInBackground:false});
}

/** Reuses timeline authorization for every action; cancellation stays in the existing confirmed history workflow. */
function RequestList({rows,today,recent=false}:{rows:DashboardRequest[];today:string;recent?:boolean}) {
 return <ul className="divide-y divide-slate-200">{rows.map(row=><li key={row.id} className="flex flex-wrap items-start justify-between gap-3 py-4">
  <div className="min-w-0 flex-1"><p className="break-words font-semibold">{row.leaveTypeName}</p><p className="text-sm">{calendarDate(row.startDate)} - {calendarDate(row.endDate)}</p>
   <p className="mt-1 text-sm font-medium">{row.status}{row.status==='Approved'&&row.startDate<=today&&row.endDate>=today?' - Ongoing today':''}</p>
   {recent&&<p className="mt-1 text-xs">{row.lastActivityUtc?`Last recorded activity: ${utcTimestamp(row.lastActivityUtc)}`:'Activity time not recorded for this legacy request.'}</p>}</div>
  <Link className="ui-secondary shrink-0" to={`/leave-requests/${row.id}/history`} aria-label={`View ${row.leaveTypeName} history, ${calendarDate(row.startDate)}`}>View history</Link>
 </li>)}</ul>;
}

/** Current-data personal capability only; no annual/accrual/payment or statutory interpretation is added. */
export function PersonalLeaveDashboard() {
 const query=usePersonalDashboard();
 const permissionDenied=isAxiosError(query.error) && [401,403].includes(query.error.response?.status ?? 0);
 const data=permissionDenied?undefined:query.data;
 return <section aria-label="Personal leave dashboard" className="space-y-6">
  <div className="ui-panel flex flex-wrap items-center justify-between gap-4"><div><h2 className="text-2xl font-bold">Your leave at a glance</h2><p className="mt-2 text-sm">Plan your time away and follow your requests.</p>{data&&<p className="mt-2 font-semibold">{data.pendingCount} Pending requests across all dates{query.isFetching||query.isError?' (previous snapshot)':''}.</p>}</div>
   <div className="flex flex-wrap gap-3"><Link className="ui-primary" to="/leave-requests/new">Request Leave</Link><Link className="ui-secondary" to="/leave-requests/history">My leave and balances</Link><button className="ui-secondary" disabled={query.isFetching} onClick={()=>void query.refetch()}>Refresh dashboard</button></div></div>
  {query.isPending&&<div className="ui-panel space-y-4" role="status"><p>Loading your leave dashboard...</p><div className="grid gap-3 sm:grid-cols-3" aria-hidden="true">{[1,2,3].map(n=><div key={n} className="h-24 rounded-xl border border-slate-200 bg-slate-100" />)}</div></div>}
  {query.isError&&<div role="alert" className="ui-error"><p>{problemMessage(query.error)}</p><p>{permissionDenied?'Personal leave access is unavailable. Sign in again to refresh your permissions.':data?'Previously loaded results below may be out of date.':'Your data is unavailable; no zero balances or counts are assumed.'}</p><button className="ui-secondary mt-3" disabled={query.isFetching} onClick={()=>void query.refetch()}>Retry dashboard</button></div>}
  {data&&<>
   <div className="ui-panel text-sm" role="status"><p>{query.isFetching?'Refreshing previously loaded results...':query.isError?'Refresh failed; showing the previous snapshot.':'Snapshot loaded.'} Captured {utcTimestamp(data.asOfUtc)}.</p><p>Business date: {calendarDate(data.basis.today)}. {data.basis.timeZone}. Refreshes every 60 seconds while this page is visible.</p></div>
   <section aria-labelledby="personal-balances-title" className="space-y-3"><h3 id="personal-balances-title" className="text-xl font-semibold">Current configured balances</h3><p className="text-sm">Configured entitlement minus all Approved calendar days. Dates include both endpoints. Pending requests do not reserve balance. These are current-model values, not annual allowances, accrual or paid/statutory entitlements. Overlapping requests are counted separately in approved request-days.</p>
    {!data.balances.length?<p className="ui-panel">No leave types are configured. Contact HR before requesting leave.</p>:<div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{data.balances.map(balance=><article className="ui-panel min-w-0" key={balance.leaveTypeId}><h4 className="break-words font-semibold">{balance.leaveTypeName}</h4><p className="mt-3 text-3xl font-bold">{balance.remainingDays}<span className="ml-2 text-sm font-normal">calendar days remaining</span></p><dl className="mt-4 space-y-2 text-sm"><div className="flex flex-wrap justify-between gap-2"><dt>Configured entitlement</dt><dd>{balance.entitledDays} days</dd></div><div className="flex flex-wrap justify-between gap-2"><dt>Approved request-days</dt><dd>{balance.usedDays} days</dd></div></dl></article>)}</div>}
   </section>
   <div className="grid items-start gap-6 xl:grid-cols-2">
    <section className="ui-panel" aria-labelledby="personal-pending-title"><h3 id="personal-pending-title" className="text-xl font-semibold">Pending requests <span className="ml-1 text-base">({data.pendingCount})</span></h3><p className="mt-2 text-sm">All Pending requests, regardless of dates. Showing up to {data.basis.pendingPreviewLimit}, earliest leave first. This is a request count, not a leave-day total.</p>{data.pending.length?<RequestList rows={data.pending} today={data.basis.today}/>:<p className="py-5">You have no Pending requests.</p>}<Link className="font-semibold text-indigo-700 underline" to="/leave-requests/history">View all requests or cancel in My leave</Link></section>
    <section className="ui-panel" aria-labelledby="personal-upcoming-title"><h3 id="personal-upcoming-title" className="text-xl font-semibold">Ongoing and upcoming Approved leave</h3><p className="mt-2 text-sm">{calendarDate(data.basis.upcomingStart)} to {calendarDate(data.basis.upcomingEnd)}, inclusive (today through today + 90 days). Includes leave already in progress. Up to {data.basis.upcomingLimit} requests, earliest start first; overlapping records remain separate.</p>{data.upcoming.length?<RequestList rows={data.upcoming} today={data.basis.today}/>:<p className="py-5">No Approved leave intersects this period.</p>}</section>
   </div>
   <section className="ui-panel" aria-labelledby="personal-recent-title"><h3 id="personal-recent-title" className="text-xl font-semibold">Recent request activity</h3><p className="mt-2 text-sm">Up to {data.basis.recentLimit} requests, newest recorded activity first. One row per request; open its history for the full timeline. Legacy missing event times remain unknown.</p>{data.recent.length?<RequestList rows={data.recent} today={data.basis.today} recent/>:<p className="py-5">No requests yet. Start with Request Leave.</p>}</section>
  </>}
  <section className="ui-panel"><h3 className="font-semibold">Your notifications</h3><p className="my-2 text-sm">Follow updates in your private inbox. Request links always check your current access.</p><Link className="ui-secondary inline-block" to="/notifications">Open your notifications</Link></section>
 </section>;
}
