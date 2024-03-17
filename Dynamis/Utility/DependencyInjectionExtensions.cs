using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Dynamis.Utility;

internal static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSingletonAlias<TAlias, TImplementation>(this IServiceCollection collection) where TAlias : class where TImplementation : TAlias
        => collection.AddSingleton<TAlias>(s => s.GetRequiredService<TImplementation>());

    public static IServiceCollection AddLazySingletonAlias<TAlias, TImplementation>(this IServiceCollection collection) where TAlias : class where TImplementation : TAlias
        => collection.AddSingleton<Lazy<TAlias>>(s => new(() => s.GetRequiredService<TImplementation>()));

    public static void AddImplementationAliases<T>(this IServiceCollection collection) where T : class
    {
        var aliases = (
            from descriptor in collection
            let type = descriptor.ServiceType
            where descriptor.ImplementationType == type && typeof(T).IsAssignableFrom(type)
            select new ServiceDescriptor(typeof(T), MakeServiceFactory<T>(type), descriptor.Lifetime)
        ).ToList();

        foreach (var alias in aliases) {
            collection.Add(alias);
        }
    }

    public static void AddLazyImplementationAliases<T>(this IServiceCollection collection) where T : class
    {
        var aliases = (
            from descriptor in collection
            let type = descriptor.ServiceType
            where descriptor.ImplementationType == type && typeof(T).IsAssignableFrom(type)
            select new ServiceDescriptor(typeof(Lazy<T>), MakeLazyServiceFactory<T>(type), descriptor.Lifetime)
        ).ToList();

        foreach (var alias in aliases) {
            collection.Add(alias);
        }
    }

    private static Func<IServiceProvider, T> MakeServiceFactory<T>(Type t) where T : notnull
        => (Func<IServiceProvider, T>)typeof(DependencyInjectionExtensions).GetMethod(nameof(ServiceFactory), BindingFlags.NonPublic | BindingFlags.Static)!
           .MakeGenericMethod(t)
           .CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IServiceProvider), t));

    private static T ServiceFactory<T>(IServiceProvider provider) where T : notnull
        => provider.GetRequiredService<T>();

    private static Func<IServiceProvider, Lazy<T>> MakeLazyServiceFactory<T>(Type t) where T : notnull
        => typeof(DependencyInjectionExtensions).GetMethod(nameof(LazyServiceFactory), BindingFlags.NonPublic | BindingFlags.Static)!
           .MakeGenericMethod(typeof(T), t)
           .CreateDelegate<Func<IServiceProvider, Lazy<T>>>();

    private static Lazy<TService> LazyServiceFactory<TService, TImplementation>(IServiceProvider provider) where TService : notnull where TImplementation : TService
        => new(() => provider.GetRequiredService<TImplementation>());
}
