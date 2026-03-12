namespace HinataProject.Api;

public interface IDiscoverableGroup
{
    Task<IEndpointRouteBuilder> MapGroup(IEndpointRouteBuilder routeBuilder);
}