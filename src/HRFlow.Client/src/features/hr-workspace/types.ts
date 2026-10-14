import type { DepartmentLeaveReport } from '../leave-reports/api';
/** Applied-period and operational data are separate; no personnel payloads appear here. */
export interface HrWorkspaceData {
 asOfUtc: string; businessToday: string; dateBasis: string; dayUnit: string;
 activeEmployees: number; inactiveEmployees: number; departmentCount: number;
 organisationDepartments: { id: string; name: string; activeEmployees: number; inactiveEmployees: number }[];
 pendingCount: number; pendingPreviewLimit: number; pending: {requestId:string;employeeName:string;leaveTypeName:string;startDate:string;endDate:string}[];
 attention: {unknownEmploymentFacts:number;missingCurrentSchedules:number;futureScheduleOnly:number;pendingActivation:number;deliveryFailed:number;deliveryPending:number;
 imports:{openBatches:number;uncreatedRows:number;previewLimit:number;batches:{id:string;expiresAtUtc:string;readyRows:number;otherUncreatedRows:number}[]}};
 report: DepartmentLeaveReport;
 trends: {start:string;end:string;pendingRequests:number;approvedRequests:number;approvedRequestDays:number;inactiveApprovedRequestDays:number}[];
}
