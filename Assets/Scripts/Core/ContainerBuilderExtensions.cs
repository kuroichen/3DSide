using System;
using VContainer;

namespace Rail
{
    public static class ContainerBuilderExtensions
    {
        public static RegistrationBuilder RegisterPlainEntryPoint<T>(this IContainerBuilder builder, Lifetime lifetime = Lifetime.Singleton)
        {
            builder.RegisterBuildCallback(resolver => resolver.Resolve<T>());
            return builder.Register<T>(lifetime);
        }

        public static RegistrationBuilder RegisterPlainEntryPoint<T>(this IContainerBuilder builder, Func<IObjectResolver, T> implementationConfiguration, Lifetime lifetime = Lifetime.Singleton)
        {
            builder.RegisterBuildCallback(resolver => resolver.Resolve<T>());
            return builder.Register<T>(implementationConfiguration, lifetime);
        }
    }
}