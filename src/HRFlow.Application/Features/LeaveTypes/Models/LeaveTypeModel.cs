namespace HRFlow.Application.Features.LeaveTypes.Models;

public class LeaveTypeModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public int DefaultBalance { get; set; }
}