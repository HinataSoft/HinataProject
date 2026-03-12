namespace HinataProject.Domain.Core;

public interface IReadOnlyId<TValue>
{
    public TValue Id { get; }
}