namespace HinataProject.Api;

public interface IDiscoverableEndpoint
{
    Task MapEndpoint(IEndpointRouteBuilder routeBuilder);
}

public interface IDiscoverableEndpoint<T> : IDiscoverableEndpoint where T: IDiscoverableGroup
{
    
}