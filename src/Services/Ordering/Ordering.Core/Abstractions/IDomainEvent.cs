namespace Ordering.Core.Abstractions
{
    public interface IDomainEvent
    {
        Guid EventId => Guid.NewGuid();
        public DateTime OcurredOn => DateTime.UtcNow;
        public string EventType => GetType().AssemblyQualifiedName;
    }
}
