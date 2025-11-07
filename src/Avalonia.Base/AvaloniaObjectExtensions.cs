using System;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Reactive;

namespace Avalonia
{
    /// <summary>
    /// Provides extension methods for <see cref="AvaloniaObject"/> and related classes.
    /// </summary>
    public static class AvaloniaObjectExtensions
    {
        /// <summary>
        /// Converts an observable sequence to an <see cref="IBinding"/> that can be applied to Avalonia properties.
        /// </summary>
        /// <typeparam name="T">The type of values produced by the observable.</typeparam>
        /// <param name="source">
        /// The observable sequence that will provide values for the binding. Must not be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IBinding"/> that subscribes to <paramref name="source"/> and updates the target
        /// property with each emitted value.
        /// </returns>
        /// <remarks>
        /// This enables observable-based reactive programming patterns with Avalonia's binding system.
        /// The binding is one-way from the observable to the property.
        /// </remarks>
        public static IBinding ToBinding<T>(this IObservable<T> source)
        {
            return new BindingAdaptor(
                typeof(T).IsValueType
                    ? source.Select(x => (object?)x)
                    : (IObservable<object?>)source);
        }

        /// <summary>
        /// Creates an observable that tracks changes to an <see cref="AvaloniaProperty"/> on an object.
        /// </summary>
        /// <param name="o">
        /// The object to observe. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="property">
        /// The property to track. Must not be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// An observable that immediately emits the current effective value of <paramref name="property"/>
        /// on <paramref name="o"/>, then emits each new effective value whenever the property changes.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The subscription to <paramref name="o"/> uses a weak reference, allowing the object to be
        /// garbage collected even if the observable subscription is not disposed. The observable will
        /// complete when the target object is collected.
        /// </para>
        /// <para>
        /// For better type safety and to avoid boxing with value types, use the generic overload
        /// <see cref="GetObservable{T}(AvaloniaObject, AvaloniaProperty{T})"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="o"/> or <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        public static IObservable<object?> GetObservable(this AvaloniaObject o, AvaloniaProperty property)
        {
            return new AvaloniaPropertyObservable<object?, object?>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)));
        }

        /// <summary>
        /// Creates a strongly-typed observable that tracks changes to an <see cref="AvaloniaProperty{T}"/> on an object.
        /// </summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="o">
        /// The object to observe. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="property">
        /// The property to track. Must not be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// An observable that immediately emits the current effective value of <paramref name="property"/>
        /// on <paramref name="o"/>, then emits each new effective value whenever the property changes.
        /// </returns>
        /// <remarks>
        /// The subscription to <paramref name="o"/> uses a weak reference, allowing the object to be
        /// garbage collected even if the observable subscription is not disposed. The observable will
        /// complete when the target object is collected.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="o"/> or <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        public static IObservable<T> GetObservable<T>(this AvaloniaObject o, AvaloniaProperty<T> property)
        {
            return new AvaloniaPropertyObservable<T, T>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)));
        }

        /// <summary>
        /// Creates an observable that tracks changes to a property and converts each value with a function.
        /// </summary>
        /// <typeparam name="TSource">The type of values held by the property.</typeparam>
        /// <typeparam name="TResult">The type of values produced by the converter.</typeparam>
        /// <param name="o">
        /// The object to observe. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="property">
        /// The property to track. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="converter">
        /// A function that converts each property value to <typeparamref name="TResult"/>. Must not be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// An observable that immediately emits the current property value converted by <paramref name="converter"/>,
        /// then emits each subsequent converted value whenever the property changes.
        /// </returns>
        /// <remarks>
        /// The subscription uses a weak reference to the target object.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="o"/>, <paramref name="property"/>, or <paramref name="converter"/> is <see langword="null"/>.
        /// </exception>
        public static IObservable<TResult> GetObservable<TSource, TResult>(this AvaloniaObject o, AvaloniaProperty<TSource> property, Func<TSource, TResult> converter)
        {
            return new AvaloniaPropertyObservable<TSource, TResult>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)),
                converter ?? throw new ArgumentNullException(nameof(converter)));
        }

        /// <summary>
        /// Creates an observable that tracks changes to a property and converts each value with a function.
        /// </summary>
        /// <typeparam name="TResult">The type of values produced by the converter.</typeparam>
        /// <param name="o">
        /// The object to observe. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="property">
        /// The property to track. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="converter">
        /// A function that converts each property value to <typeparamref name="TResult"/>. Must not be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// An observable that immediately emits the current property value converted by <paramref name="converter"/>,
        /// then emits each subsequent converted value whenever the property changes.
        /// </returns>
        /// <remarks>
        /// The subscription uses a weak reference to the target object.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="o"/>, <paramref name="property"/>, or <paramref name="converter"/> is <see langword="null"/>.
        /// </exception>
        public static IObservable<TResult> GetObservable<TResult>(this AvaloniaObject o, AvaloniaProperty property, Func<object?, TResult> converter)
        {
            return new AvaloniaPropertyObservable<object?, TResult>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)),
                converter ?? throw new ArgumentNullException(nameof(converter)));
        }

        /// <summary>
        /// Gets an observable for an <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>
        /// An observable which fires immediately with the current value of the property on the
        /// object and subsequently each time the property value changes.
        /// </returns>
        /// <remarks>
        /// The subscription to <paramref name="o"/> is created using a weak reference.
        /// </remarks>
        public static IObservable<BindingValue<object?>> GetBindingObservable(
            this AvaloniaObject o,
            AvaloniaProperty property)
        {
            return new AvaloniaPropertyBindingObservable<object?, object?>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)));
        }

        /// <inheritdoc cref="GetObservable{TSource,TResult}"/>
        public static IObservable<BindingValue<TResult>> GetBindingObservable<TResult>(this AvaloniaObject o, AvaloniaProperty property, Func<object?, TResult> converter)
        {
            return new AvaloniaPropertyBindingObservable<object?, TResult>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)),
                converter?? throw new ArgumentNullException(nameof(converter)));
        }

        /// <summary>
        /// Gets an observable for an <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>
        /// An observable which fires immediately with the current value of the property on the
        /// object and subsequently each time the property value changes.
        /// </returns>
        /// <remarks>
        /// The subscription to <paramref name="o"/> is created using a weak reference.
        /// </remarks>
        public static IObservable<BindingValue<T>> GetBindingObservable<T>(
            this AvaloniaObject o,
            AvaloniaProperty<T> property)
        {
            return new AvaloniaPropertyBindingObservable<T, T>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)));

        }

        /// <inheritdoc cref="GetBindingObservable{T}(AvaloniaObject, AvaloniaProperty{T})"/>
        /// <param name="o"/>
        /// <param name="property"/>
        /// <param name="converter">A method which is executed to convert each property value to <typeparamref name="TResult"/>.</param>
        public static IObservable<BindingValue<TResult>> GetBindingObservable<TSource, TResult>(
            this AvaloniaObject o,
            AvaloniaProperty<TSource> property,
            Func<TSource, TResult> converter)
        {
            return new AvaloniaPropertyBindingObservable<TSource, TResult>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)),
                converter ?? throw new ArgumentNullException(nameof(converter)));
        }

        /// <summary>
        /// Creates an observable that emits <see cref="AvaloniaPropertyChangedEventArgs"/> whenever
        /// the specified property changes on an object.
        /// </summary>
        /// <param name="o">
        /// The object to observe. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="property">
        /// The property to track. Must not be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// An observable that emits the <see cref="AvaloniaPropertyChangedEventArgs"/> each time
        /// <see cref="AvaloniaObject.PropertyChanged"/> is raised for <paramref name="property"/>
        /// on <paramref name="o"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Unlike <see cref="GetObservable(AvaloniaObject, AvaloniaProperty)"/>, which emits property values,
        /// this method emits the full change event arguments, providing access to both old and new values,
        /// priority information, and whether the change is an effective value change.
        /// </para>
        /// <para>
        /// This observable does not emit the current value immediately; it only emits when the property
        /// changes after subscription.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="o"/> or <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        public static IObservable<AvaloniaPropertyChangedEventArgs> GetPropertyChangedObservable(
            this AvaloniaObject o,
            AvaloniaProperty property)
        {
            return new AvaloniaPropertyChangedObservable(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)));
        }

        /// <summary>
        /// Binds an <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public static IDisposable Bind<T>(
            this AvaloniaObject target,
            AvaloniaProperty<T> property,
            IObservable<BindingValue<T>> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));
            source = source ?? throw new ArgumentNullException(nameof(source));

            return property switch
            {
                StyledProperty<T> styled => target.Bind(styled, source, priority),
                DirectPropertyBase<T> direct => target.Bind(direct, source),
                _ => throw new NotSupportedException("Unsupported AvaloniaProperty type."),
            };
        }

        /// <summary>
        /// Binds an <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public static IDisposable Bind<T>(
            this AvaloniaObject target,
            AvaloniaProperty<T> property,
            IObservable<T> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            return property switch
            {
                StyledProperty<T> styled => target.Bind(styled, source, priority),
                DirectPropertyBase<T> direct => target.Bind(direct, source),
                _ => throw new NotSupportedException("Unsupported AvaloniaProperty type."),
            };
        }

        /// <summary>
        /// Binds a property on an <see cref="AvaloniaObject"/> to an <see cref="IBinding"/>.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property to bind.</param>
        /// <param name="binding">The binding.</param>
        /// <param name="anchor">
        /// An optional anchor from which to locate required context. When binding to objects that
        /// are not in the logical tree, certain types of binding need an anchor into the tree in 
        /// order to locate named controls or resources. The <paramref name="anchor"/> parameter 
        /// can be used to provide this context.
        /// </param>
        /// <returns>An <see cref="IDisposable"/> which can be used to cancel the binding.</returns>
        [Obsolete("Use AvaloniaObject.Bind(AvaloniaProperty, IBinding")]
        public static IDisposable Bind(
            this AvaloniaObject target,
            AvaloniaProperty property,
            IBinding binding,
            object? anchor = null)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));
            binding = binding ?? throw new ArgumentNullException(nameof(binding));

            return target.Bind(property, binding);
        }

        /// <summary>
        /// Gets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public static T GetValue<T>(this AvaloniaObject target, AvaloniaProperty<T> property)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));

            return property switch
            {
                StyledProperty<T> styled => target.GetValue(styled),
                DirectPropertyBase<T> direct => target.GetValue(direct),
                _ => throw new NotSupportedException("Unsupported AvaloniaProperty type.")
            };
        }

        /// <summary>
        /// Gets an <see cref="AvaloniaProperty"/> base value.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// For styled properties, gets the value of the property excluding animated values, otherwise
        /// <see cref="AvaloniaProperty.UnsetValue"/>. Note that this method does not return
        /// property values that come from inherited or default values.
        /// 
        /// For direct properties returns the current value of the property.
        /// </remarks>
        public static object? GetBaseValue(
            this AvaloniaObject target,
            AvaloniaProperty property)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));

            return property.RouteGetBaseValue(target);
        }

        /// <summary>
        /// Gets an <see cref="AvaloniaProperty"/> base value.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// For styled properties, gets the value of the property excluding animated values, otherwise
        /// <see cref="Optional{T}.Empty"/>. Note that this method does not return property values
        /// that come from inherited or default values.
        /// 
        /// For direct properties returns the current value of the property.
        /// </remarks>
        public static Optional<T> GetBaseValue<T>(
            this AvaloniaObject target,
            AvaloniaProperty<T> property)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));

            return property switch
            {
                StyledProperty<T> styled => target.GetBaseValue(styled),
                DirectPropertyBase<T> direct => target.GetValue(direct),
                _ => throw new NotSupportedException("Unsupported AvaloniaProperty type.")
            };
        }

        /// <summary>
        /// Subscribes to a property changed notifications for changes that originate from a
        /// <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the property change sender.</typeparam>
        /// <param name="observable">The property changed observable.</param>
        /// <param name="action">
        /// The method to call. The parameters are the sender and the event args.
        /// </param>
        /// <returns>A disposable that can be used to terminate the subscription.</returns>
        public static IDisposable AddClassHandler<TTarget>(
            this IObservable<AvaloniaPropertyChangedEventArgs> observable,
            Action<TTarget, AvaloniaPropertyChangedEventArgs> action)
            where TTarget : AvaloniaObject
        {
            return observable.Subscribe(new ClassHandlerObserver<TTarget>(action));
        }

        /// <summary>
        /// Subscribes to a property changed notifications for changes that originate from a
        /// <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the property change sender.</typeparam>
        /// <typeparam name="TValue">The type of the property.</typeparam>
        /// <param name="observable">The property changed observable.</param>
        /// <param name="action">
        /// The method to call. The parameters are the sender and the event args.
        /// </param>
        /// <returns>A disposable that can be used to terminate the subscription.</returns>
        public static IDisposable AddClassHandler<TTarget, TValue>(
            this IObservable<AvaloniaPropertyChangedEventArgs<TValue>> observable,
            Action<TTarget, AvaloniaPropertyChangedEventArgs<TValue>> action) where TTarget : AvaloniaObject
        {
            return observable.Subscribe(new ClassHandlerObserver<TTarget, TValue>(action));
        }

        private class BindingAdaptor : IBinding2
        {
            private readonly IObservable<object?> _source;

            public BindingAdaptor(IObservable<object?> source)
            {
                this._source = source;
            }

            public InstancedBinding? Initiate(
                AvaloniaObject target,
                AvaloniaProperty? targetProperty,
                object? anchor = null,
                bool enableDataValidation = false)
            {
                var expression = new UntypedObservableBindingExpression(_source, BindingPriority.LocalValue);
                return new InstancedBinding(expression, BindingMode.OneWay, BindingPriority.LocalValue);
            }

            BindingExpressionBase IBinding2.Instance(AvaloniaObject target, AvaloniaProperty? property, object? anchor)
            {
                return new UntypedObservableBindingExpression(_source, BindingPriority.LocalValue);
            }
        }

        private class ClassHandlerObserver<TTarget, TValue> : IObserver<AvaloniaPropertyChangedEventArgs<TValue>>
        {
            private readonly Action<TTarget, AvaloniaPropertyChangedEventArgs<TValue>> _action;

            public ClassHandlerObserver(Action<TTarget, AvaloniaPropertyChangedEventArgs<TValue>> action)
            {
                _action = action;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(AvaloniaPropertyChangedEventArgs<TValue> value)
            {
                if (value.Sender is TTarget target)
                {
                    _action(target, value);
                }
            }
        }

        private class ClassHandlerObserver<TTarget> : IObserver<AvaloniaPropertyChangedEventArgs>
        {
            private readonly Action<TTarget, AvaloniaPropertyChangedEventArgs> _action;

            public ClassHandlerObserver(Action<TTarget, AvaloniaPropertyChangedEventArgs> action)
            {
                _action = action;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(AvaloniaPropertyChangedEventArgs value)
            {
                if (value.Sender is TTarget target)
                {
                    _action(target, value);
                }
            }
        }
    }
}
