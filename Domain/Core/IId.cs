namespace HinataProject.Domain.Core;

public interface IId<TValue> : IReadOnlyId<TValue>
{
    public new TValue Id { get; set; }
}