namespace HinataProject.Domain.Core;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int? TotalCount { get; set; }
}