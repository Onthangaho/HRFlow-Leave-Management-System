namespace HRFlow.Application.Interfaces;
/// <summary>Distributes safe CSV headings together with field guidance, so offline HR preparation cannot mistake unsupported Gender for accepted data.</summary>
public static class OnboardingTemplate
{
 /// <summary>Contains headings only; no invented employees, credentials or example personal data.</summary>
 public const string Csv = "FirstName,LastName,EmailAddress,EmployeeNumber,Roles,Department,ManagerEmail,EmploymentStartDate,WorkScheduleCode,Gender\r\n";
 /// <summary>Explains the actual supported contract; extract the CSV before uploading.</summary>
 public const string Guidance = "Extract the CSV from this ZIP before uploading. UTF-8; optional BOM; 1 MiB; 100 data rows.\n" +
  "FirstName and LastName join with one space into FullName. Dates: yyyy-MM-dd. Roles: semicolon-separated Employee;Manager;HR Administrator.\n" +
  "Department must match an existing name. ManagerEmail must identify an existing active, activated same-department Manager, or be blank.\n" +
  "EmployeeNumber is required and unique, including inactive accounts. WorkScheduleCode must identify an existing template, or be blank for Unknown.\n" +
  "Gender is optional and currently UNSUPPORTED: leave every value blank. Nonblank values are Invalid and prevent creation; Gender never determines leave eligibility.\n" +
  "Never include passwords, tokens or credential columns. Future start dates do not delay activated-account access. Leave charging remains inclusive calendar days.\n";
}
