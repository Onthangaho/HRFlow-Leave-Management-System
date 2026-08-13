namespace HRFlow.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
}

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}