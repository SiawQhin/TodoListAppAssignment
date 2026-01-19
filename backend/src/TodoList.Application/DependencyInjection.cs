using Microsoft.Extensions.DependencyInjection;
using TodoList.Application.Mappings;
using TodoList.Application.Services;

namespace TodoList.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register AutoMapper with profiles from this assembly
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<TodoMappingProfile>();
        });

        // Register application services
        services.AddScoped<ITodoService, TodoService>();

        return services;
    }
}
