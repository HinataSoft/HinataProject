using NodaTime;

namespace HinataProject.Domain.Core;


public interface IHaveCreatedDate
{
    public Instant CreatedAt { get; set; }
}