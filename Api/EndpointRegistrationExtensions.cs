using System.Reflection;

namespace HinataProject.Api;

/// <summary>
/// Contains extension methods for registering and mapping endpoints.
/// </summary>
public static class EndpointRegistrationExtensions
{
    public static void AddEndpoints(this WebApplicationBuilder builder, Assembly assembly)
    {
        var endpointTypes = assembly.GetTypes()
            .Where(t => typeof(IDiscoverableGroup).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        
        foreach (var endpointType in endpointTypes)
            builder.Services.AddSingleton(typeof(IDiscoverableGroup), endpointType);
        
        endpointTypes = assembly.GetTypes()
            .Where(t => typeof(IDiscoverableEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        
        foreach (var endpointType in endpointTypes)
            builder.Services.AddSingleton(typeof(IDiscoverableEndpoint), endpointType);
    }

    
    /// <summary>
    /// Maps all endpoints registered as services that implement the <see cref="IEndpoint"/> interface to the provided <see cref="WebApplication"/>.
    /// </summary>
    /// <param name="webApplication">The <see cref="WebApplication"/> to map the endpoints to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task MapAllEndpoints(this WebApplication webApplication)
    {
        var groups = webApplication.Services.GetServices<IDiscoverableGroup>();
        var discoverableEndpoints = webApplication.Services.GetServices<IDiscoverableEndpoint>().ToList();

        foreach (var discoverableGroup in groups)
        {
            var type = discoverableGroup.GetType();
            var groupBuilder = await discoverableGroup.MapGroup(webApplication);

            List<IDiscoverableEndpoint> toRemove = new List<IDiscoverableEndpoint>();
            foreach (var discoverableEndpoint in discoverableEndpoints)
            {
                var endpointInterface = discoverableEndpoint.GetType()
                    .GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDiscoverableEndpoint<>));

                if (endpointInterface is null) 
                    continue;
                
                var groupType = endpointInterface.GetGenericArguments()[0];
                
                if (discoverableGroup.GetType() != groupType) 
                    continue;
                
                await discoverableEndpoint.MapEndpoint(groupBuilder);
                toRemove.Add(discoverableEndpoint);
            }

            foreach (var discoverableEndpoint in toRemove)
                discoverableEndpoints.Remove(discoverableEndpoint);
        }
        
        foreach (var endpointType in discoverableEndpoints)
            await endpointType.MapEndpoint(webApplication);
    }
}