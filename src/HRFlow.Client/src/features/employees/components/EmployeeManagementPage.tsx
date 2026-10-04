import { EmployeeTable } from './EmployeeTable';

/** Presents the read-only employee directory until the matching HR write API is exposed. */
export const EmployeeManagementPage = () => {
  return (
    <main className="mx-auto mt-16 w-full max-w-5xl space-y-8 rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
      <header>
        <h1 className="text-3xl font-bold text-slate-900">Employee Directory</h1>
        <p className="mt-2 text-sm text-slate-600">
          Review employee records. HR write actions will be enabled when their corresponding API endpoints are available.
        </p>
      </header>
      <EmployeeTable />
    </main>
  );
};