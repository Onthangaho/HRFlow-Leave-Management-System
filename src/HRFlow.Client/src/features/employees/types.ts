export interface Employee {
  id: string;
  fullName: string;
  email: string;
  departmentName: string;
  roleName: string;
}

export interface EmployeeFormValues {
  fullName: string;
  email: string;
  password?: string;
  departmentId: string;
  roleName: string;
}