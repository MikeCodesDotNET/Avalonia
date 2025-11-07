using System;
using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia
{
    /// <summary>
    /// Provides a lightweight service locator for Avalonia's internal dependency injection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a legacy dependency injection mechanism used internally by Avalonia. Modern application
    /// code should use standard .NET dependency injection patterns instead.
    /// </para>
    /// <para>
    /// The locator supports hierarchical scopes, singleton and transient lifetimes, and factory functions.
    /// Services can be registered via the fluent <see cref="Bind{T}"/> API.
    /// </para>
    /// </remarks>
    [PrivateApi]
    public class AvaloniaLocator : IAvaloniaDependencyResolver
    {
        private readonly IAvaloniaDependencyResolver? _parentScope;

        /// <summary>
        /// Gets or sets the current global service resolver.
        /// </summary>
        /// <value>
        /// The active resolver used to locate services. Defaults to the <see cref="CurrentMutable"/> instance.
        /// </value>
        public static IAvaloniaDependencyResolver Current { get; set; }

        /// <summary>
        /// Gets or sets the current mutable locator instance.
        /// </summary>
        /// <value>
        /// The mutable <see cref="AvaloniaLocator"/> that can be used to register new services.
        /// </value>
        public static AvaloniaLocator CurrentMutable { get; set; }

        private readonly Dictionary<Type, Func<object?>> _registry = new Dictionary<Type, Func<object?>>();

        static AvaloniaLocator()
        {
            Current = CurrentMutable = new AvaloniaLocator();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaLocator"/> class with no parent scope.
        /// </summary>
        public AvaloniaLocator()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaLocator"/> class with the specified parent scope.
        /// </summary>
        /// <param name="parentScope">
        /// The parent resolver to fall back to when a service is not found in this locator.
        /// </param>
        public AvaloniaLocator(IAvaloniaDependencyResolver parentScope)
        {
            _parentScope = parentScope;
        }

        /// <summary>
        /// Retrieves a service of the specified type.
        /// </summary>
        /// <param name="t">The service type to locate.</param>
        /// <returns>
        /// The service instance, or <see langword="null"/> if the service is not registered in this
        /// locator or any parent scope.
        /// </returns>
        public object? GetService(Type t)
        {
            return _registry.TryGetValue(t, out var rv) ? rv() : _parentScope?.GetService(t);
        }

        /// <summary>
        /// Provides a fluent API for registering service implementations with various lifetimes.
        /// </summary>
        /// <typeparam name="TService">The service type being registered.</typeparam>
        public class RegistrationHelper<TService>
        {
            private readonly AvaloniaLocator _locator;

            /// <summary>
            /// Initializes a new instance of the <see cref="RegistrationHelper{TService}"/> class.
            /// </summary>
            /// <param name="locator">The locator to register services with.</param>
            public RegistrationHelper(AvaloniaLocator locator)
            {
                _locator = locator;
            }

            /// <summary>
            /// Registers a constant instance as the service implementation.
            /// </summary>
            /// <typeparam name="TImpl">The implementation type.</typeparam>
            /// <param name="constant">The constant instance to return for all service requests.</param>
            /// <returns>The locator, for fluent chaining.</returns>
            public AvaloniaLocator ToConstant<TImpl>(TImpl constant) where TImpl : TService
            {
                _locator._registry[typeof(TService)] = () => constant;
                return _locator;
            }

            /// <summary>
            /// Registers a factory function that is invoked on each service request.
            /// </summary>
            /// <typeparam name="TImlp">The implementation type.</typeparam>
            /// <param name="func">The factory function that creates new instances.</param>
            /// <returns>The locator, for fluent chaining.</returns>
            public AvaloniaLocator ToFunc<TImlp>(Func<TImlp> func) where TImlp : TService
            {
                _locator._registry[typeof(TService)] = () => func();
                return _locator;
            }

            /// <summary>
            /// Registers a factory function that is invoked once on the first service request,
            /// then returns the same instance thereafter.
            /// </summary>
            /// <typeparam name="TImlp">The implementation type.</typeparam>
            /// <param name="func">The factory function that creates the instance.</param>
            /// <returns>The locator, for fluent chaining.</returns>
            /// <remarks>
            /// This provides lazy singleton behavior - the instance is not created until first requested.
            /// Not thread-safe; concurrent first requests may result in multiple instances.
            /// </remarks>
            public AvaloniaLocator ToLazy<TImlp>(Func<TImlp> func) where TImlp : TService
            {
                var constructed = false;
                TImlp? instance = default;
                _locator._registry[typeof(TService)] = () =>
                {
                    if (!constructed)
                    {
                        instance = func();
                        constructed = true;
                    }

                    return instance;
                };
                return _locator;
            }

            /// <summary>
            /// Registers a type that will be lazily instantiated as a singleton using its parameterless constructor.
            /// </summary>
            /// <typeparam name="TImpl">The implementation type with a parameterless constructor.</typeparam>
            /// <returns>The locator, for fluent chaining.</returns>
            public AvaloniaLocator ToSingleton<TImpl>() where TImpl : class, TService, new()
            {
                TImpl? instance = null;
                return ToFunc(() => instance ?? (instance = new TImpl()));
            }

            /// <summary>
            /// Registers a type that will be instantiated using its parameterless constructor on each request.
            /// </summary>
            /// <typeparam name="TImpl">The implementation type with a parameterless constructor.</typeparam>
            /// <returns>The locator, for fluent chaining.</returns>
            public AvaloniaLocator ToTransient<TImpl>() where TImpl : class, TService, new() => ToFunc(() => new TImpl());
        }

        /// <summary>
        /// Begins registering a service of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The service type to register.</typeparam>
        /// <returns>
        /// A <see cref="RegistrationHelper{TService}"/> that provides methods to specify the implementation.
        /// </returns>
        public RegistrationHelper<T> Bind<T>() => new RegistrationHelper<T>(this);

        /// <summary>
        /// Registers a constant instance as both the service type and implementation type.
        /// </summary>
        /// <typeparam name="T">The service and implementation type.</typeparam>
        /// <param name="constant">The instance to register.</param>
        /// <returns>The locator, for fluent chaining.</returns>
        public AvaloniaLocator BindToSelf<T>(T constant)
            => Bind<T>().ToConstant(constant);

        /// <summary>
        /// Registers a type as a lazy singleton, serving as both the service and implementation type.
        /// </summary>
        /// <typeparam name="T">The service and implementation type with a parameterless constructor.</typeparam>
        /// <returns>The locator, for fluent chaining.</returns>
        public AvaloniaLocator BindToSelfSingleton<T>() where T : class, new() => Bind<T>().ToSingleton<T>();

        private class ResolverDisposable : IDisposable
        {
            private readonly IAvaloniaDependencyResolver _resolver;
            private readonly AvaloniaLocator _mutable;

            public ResolverDisposable(IAvaloniaDependencyResolver resolver, AvaloniaLocator mutable)
            {
                _resolver = resolver;
                _mutable = mutable;
            }

            public void Dispose()
            {
                Current = _resolver;
                CurrentMutable = _mutable;
            }
        }

        /// <summary>
        /// Creates a new service locator scope that inherits from the current scope.
        /// </summary>
        /// <returns>
        /// An <see cref="IDisposable"/> that, when disposed, restores the previous scope.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Creates a child locator with the current locator as its parent. Services registered in the
        /// new scope shadow those in parent scopes. Disposing the returned handle restores the previous
        /// <see cref="Current"/> and <see cref="CurrentMutable"/> values.
        /// </para>
        /// <para>
        /// This enables temporary service overrides or test isolation.
        /// </para>
        /// </remarks>
        public static IDisposable EnterScope()
        {
            var d = new ResolverDisposable(Current, CurrentMutable);
            Current = CurrentMutable = new AvaloniaLocator(Current);
            return d;
        }
    }

    /// <summary>
    /// Provides a service location abstraction for resolving service instances by type.
    /// </summary>
    [PrivateApi]
    public interface IAvaloniaDependencyResolver
    {
        /// <summary>
        /// Retrieves a service instance of the specified type.
        /// </summary>
        /// <param name="t">The service type to locate.</param>
        /// <returns>
        /// The service instance, or <see langword="null"/> if no service of the specified type is registered.
        /// </returns>
        object? GetService(Type t);
    }

    /// <summary>
    /// Provides extension methods for <see cref="IAvaloniaDependencyResolver"/>.
    /// </summary>
    [PrivateApi]
    public static class LocatorExtensions
    {
        /// <summary>
        /// Retrieves a service instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The service type to locate.</typeparam>
        /// <param name="resolver">The resolver to query.</param>
        /// <returns>
        /// The service instance cast to <typeparamref name="T"/>, or <see langword="null"/> if no
        /// service of the specified type is registered.
        /// </returns>
        public static T? GetService<T>(this IAvaloniaDependencyResolver resolver)
        {
            return (T?)resolver.GetService(typeof(T));
        }

        /// <summary>
        /// Retrieves a required service instance of the specified type.
        /// </summary>
        /// <param name="resolver">The resolver to query.</param>
        /// <param name="t">The service type to locate.</param>
        /// <returns>The service instance. Never <see langword="null"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no service of the specified type is registered.
        /// </exception>
        public static object GetRequiredService(this IAvaloniaDependencyResolver resolver, Type t)
        {
            return resolver.GetService(t) ?? throw new InvalidOperationException($"Unable to locate '{t}'.");
        }

        /// <summary>
        /// Retrieves a required service instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The service type to locate.</typeparam>
        /// <param name="resolver">The resolver to query.</param>
        /// <returns>The service instance cast to <typeparamref name="T"/>. Never <see langword="null"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no service of the specified type is registered.
        /// </exception>
        public static T GetRequiredService<T>(this IAvaloniaDependencyResolver resolver)
        {
            return (T?)resolver.GetService(typeof(T)) ?? throw new InvalidOperationException($"Unable to locate '{typeof(T)}'.");
        }
    }
}

