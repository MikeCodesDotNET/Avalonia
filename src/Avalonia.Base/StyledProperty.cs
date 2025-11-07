using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.PropertyStore;
using Avalonia.Utilities;
using static Avalonia.StyledPropertyNonGenericHelper;

namespace Avalonia
{
    /// <summary>
    /// Represents a styled property that supports styling, theming, animations, bindings, and
    /// priority-based value resolution.
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <remarks>
    /// <para>
    /// Styled properties are the primary property type in Avalonia. Unlike direct properties, styled
    /// properties maintain multiple concurrent values at different priority levels and resolve the
    /// effective value based on priority. Value sources include (from highest to lowest priority):
    /// </para>
    /// <list type="number">
    /// <item><description>Animations - Active animations override all other sources</description></item>
    /// <item><description>Local values - Set via code or XAML attributes</description></item>
    /// <item><description>Template bindings - Set by control templates</description></item>
    /// <item><description>Styles - Applied by CSS-like style selectors</description></item>
    /// <item><description>Inherited values - Inherited from parent objects if <see cref="AvaloniaProperty.Inherits"/> is true</description></item>
    /// <item><description>Default value - Specified in metadata</description></item>
    /// </list>
    /// <para>
    /// Styled properties can be registered using <see cref="AvaloniaProperty.Register{TOwner, TValue}"/>,
    /// added to additional types via <see cref="AddOwner{TOwner}"/>, and have their metadata overridden
    /// per type using <see cref="OverrideMetadata{T}"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="AvaloniaProperty"/>
    /// <seealso cref="DirectProperty{TOwner, TValue}"/>
    /// <seealso cref="AttachedProperty{T}"/>
    /// <seealso cref="StyledPropertyMetadata{TValue}"/>
    public class StyledProperty<TValue> : AvaloniaProperty<TValue>, IStyledPropertyAccessor
    {
        // For performance, cache the default value if there's only one (mostly for AvaloniaObject.GetValue()),
        // avoiding a GetMetadata() call which might need to iterate through the control hierarchy.
        private Optional<TValue> _singleDefaultValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyledProperty{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="hostType">The class that the property being is registered on.</param>
        /// <param name="metadata">The property metadata.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="validate">
        /// <para>A method which returns "false" for values that are never valid for this property.</para>
        /// <para>This method is not part of the property's metadata and so cannot be changed after registration.</para>
        /// </param>
        /// <param name="notifying">A <see cref="AvaloniaProperty.Notifying"/> callback.</param>
        internal StyledProperty(
            string name,
            Type ownerType,
            Type hostType,
            StyledPropertyMetadata<TValue> metadata,
            bool inherits = false,
            Func<TValue, bool>? validate = null,
            Action<AvaloniaObject, bool>? notifying = null)
                : base(name, ownerType, hostType, metadata, notifying)
        {
            Inherits = inherits;
            ValidateValue = validate;

            if (validate?.Invoke(metadata.DefaultValue) == false)
            {
                ThrowInvalidDefaultValue(name, metadata.DefaultValue, name);
            }

            _singleDefaultValue = metadata.DefaultValue;
        }

        /// <summary>
        /// Gets the validation callback that determines whether a value is valid for this property.
        /// </summary>
        /// <value>
        /// A function that returns <see langword="false"/> for values that should never be accepted,
        /// or <see langword="null"/> if no validation is performed.
        /// </value>
        /// <remarks>
        /// <para>
        /// This validator is set during property registration and cannot be changed afterward. It provides
        /// permanent validation that applies regardless of metadata overrides. If validation fails, the
        /// property system rejects the value.
        /// </para>
        /// <para>
        /// This is distinct from coercion (in metadata), which adjusts values after validation. Validators
        /// enforce hard constraints like "Width must be non-negative," while coercion might clamp values
        /// to specific ranges.
        /// </para>
        /// </remarks>
        public Func<TValue, bool>? ValidateValue { get; }

        /// <summary>
        /// Registers this property on an additional owner type, optionally with different metadata.
        /// </summary>
        /// <typeparam name="TOwner">
        /// The additional owner type. Must be <see cref="AvaloniaObject"/> or a derived type.
        /// </typeparam>
        /// <param name="metadata">
        /// Optional metadata override for the new owner type. If <see langword="null"/>, the property
        /// uses metadata from the inheritance hierarchy.
        /// </param>
        /// <returns>
        /// This property instance, enabling fluent method chaining.
        /// </returns>
        /// <remarks>
        /// <para>
        /// AddOwner allows a property originally registered on one type to be recognized on other types.
        /// All registrations share the same property identity and <see cref="AvaloniaProperty.Id"/>.
        /// This is commonly used when a derived type needs different default values or change callbacks.
        /// </para>
        /// <para>
        /// If <paramref name="metadata"/> is provided, it applies specifically to <typeparamref name="TOwner"/>
        /// and its derived types. Metadata resolution walks the type hierarchy, so derived types without
        /// their own metadata override inherit from their nearest ancestor with metadata.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the property is already registered on <typeparamref name="TOwner"/>.
        /// </exception>
        public StyledProperty<TValue> AddOwner<TOwner>(StyledPropertyMetadata<TValue>? metadata = null) where TOwner : AvaloniaObject
        {
            AvaloniaPropertyRegistry.Instance.Register(typeof(TOwner), this);
            if (metadata != null)
            {
                OverrideMetadata<TOwner>(metadata);
            }

            return this;
        }

        /// <summary>
        /// Applies coercion to a property value using the metadata for the specified instance.
        /// </summary>
        /// <param name="instance">
        /// The object whose metadata determines the coercion callback. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="baseValue">
        /// The value to coerce.
        /// </param>
        /// <returns>
        /// The coerced value, or <paramref name="baseValue"/> unchanged if no coercion callback is defined.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Coercion allows metadata to constrain or adjust property values after validation but before
        /// the value is stored. Common uses include clamping numeric values to ranges or ensuring
        /// consistency with other properties.
        /// </para>
        /// <para>
        /// The coercion callback is retrieved from metadata for the runtime type of <paramref name="instance"/>.
        /// Derived types can provide different coercion logic via <see cref="OverrideMetadata{T}"/>.
        /// </para>
        /// <para>
        /// Coercion runs after <see cref="ValidateValue"/> but before the value is committed. If the
        /// coerced value fails validation, the property system rejects it.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="instance"/> is <see langword="null"/>.
        /// </exception>
        public TValue CoerceValue(AvaloniaObject instance, TValue baseValue)
        {
            var metadata = GetMetadata(instance);

            if (metadata.CoerceValue != null)
            {
                return metadata.CoerceValue.Invoke(instance, baseValue);
            }

            return baseValue;
        }

        /// <summary>
        /// Gets the default value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The default value.</returns>
        /// <remarks>
        /// For performance, prefer the <see cref="GetDefaultValue(Avalonia.AvaloniaObject)"/> overload when possible.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue GetDefaultValue(Type type)
        {
            return _singleDefaultValue.HasValue ?
                _singleDefaultValue.GetValueOrDefault()! :
                GetMetadata(type).DefaultValue;
        }

        /// <summary>
        /// Gets the default value for the property on the specified object.
        /// </summary>
        /// <param name="owner">The object.</param>
        /// <returns>The default value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue GetDefaultValue(AvaloniaObject owner)
        {
            return _singleDefaultValue.HasValue ?
                _singleDefaultValue.GetValueOrDefault()! :
                GetMetadata(owner).DefaultValue;
        }

        /// <inheritdoc cref="AvaloniaProperty.GetMetadata(System.Type)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new StyledPropertyMetadata<TValue> GetMetadata(Type type)
            => CastMetadata(base.GetMetadata(type));

        /// <inheritdoc cref="AvaloniaProperty.GetMetadata(Avalonia.AvaloniaObject)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new StyledPropertyMetadata<TValue> GetMetadata(AvaloniaObject owner)
            => CastMetadata(base.GetMetadata(owner));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static StyledPropertyMetadata<TValue> CastMetadata(AvaloniaPropertyMetadata metadata)
        {
#if DEBUG
            return (StyledPropertyMetadata<TValue>)metadata;
#else
            // Avoid casts in release mode for performance (GetMetadata is a hot path).
            // We control every path:
            // it shouldn't be possible a metadata type other than a StyledPropertyMetadata<T> stored for a StyledProperty<T>.
            return Unsafe.As<StyledPropertyMetadata<TValue>>(metadata);
#endif
        }

        /// <summary>
        /// Overrides the default value for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue<T>(TValue defaultValue) where T : AvaloniaObject
        {
            OverrideDefaultValue(typeof(T), defaultValue);
        }

        /// <summary>
        /// Overrides the default value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue(Type type, TValue defaultValue)
        {
            OverrideMetadata(type, new StyledPropertyMetadata<TValue>(defaultValue));
        }

        /// <summary>
        /// Overrides the metadata for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="metadata">The metadata.</param>
        public void OverrideMetadata<T>(StyledPropertyMetadata<TValue> metadata) where T : AvaloniaObject => OverrideMetadata(typeof(T), metadata);

        /// <summary>
        /// Overrides the metadata for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="metadata">The metadata.</param>
        public void OverrideMetadata(Type type, StyledPropertyMetadata<TValue> metadata)
        {
            if (ValidateValue != null)
            {
                if (!ValidateValue(metadata.DefaultValue))
                {
                    ThrowInvalidDefaultValue(Name, metadata.DefaultValue, nameof(metadata));
                }
            }

            base.OverrideMetadata(type, metadata);

            if (_singleDefaultValue != metadata.DefaultValue)
            {
                _singleDefaultValue = default;
            }
        }

        /// <summary>
        /// Gets the string representation of the property.
        /// </summary>
        /// <returns>The property's string representation.</returns>
        public override string ToString()
        {
            return Name;
        }

        object? IStyledPropertyAccessor.GetDefaultValue(Type type) => GetDefaultValue(type);

        object? IStyledPropertyAccessor.GetDefaultValue(AvaloniaObject owner) => GetDefaultValue(owner);

        bool IStyledPropertyAccessor.ValidateValue(object? value)
        {
            if (value is null)
            {
                if (!typeof(TValue).IsValueType || Nullable.GetUnderlyingType(typeof(TValue)) != null)
                    return ValidateValue?.Invoke(default!) ?? true;
            }
            else if (value is TValue typed)
            {
                return ValidateValue?.Invoke(typed) ?? true;
            }

            return false;
        }

        internal override EffectiveValue CreateEffectiveValue(AvaloniaObject o)
        {
            return o.GetValueStore().CreateEffectiveValue(this);
        }

        /// <inheritdoc/>
        internal override void RouteClearValue(AvaloniaObject o)
        {
            o.ClearValue<TValue>(this);
        }

        internal override void RouteCoerceDefaultValue(AvaloniaObject o)
        {
            o.GetValueStore().CoerceDefaultValue(this);
        }

        /// <inheritdoc/>
        internal override object? RouteGetValue(AvaloniaObject o)
        {
            return o.GetValue<TValue>(this);
        }

        /// <inheritdoc/>
        internal override object? RouteGetBaseValue(AvaloniaObject o)
        {
            var value = o.GetBaseValue<TValue>(this);
            return value.HasValue ? value.Value : AvaloniaProperty.UnsetValue;
        }

        /// <inheritdoc/>
        internal override IDisposable? RouteSetValue(
            AvaloniaObject target,
            object? value,
            BindingPriority priority)
        {
            if (ShouldSetValue(target, value, out var converted))
                return target.SetValue<TValue>(this, converted, priority);
            return null;
        }

        internal override void RouteSetCurrentValue(AvaloniaObject target, object? value)
        {
            if (ShouldSetValue(target, value, out var converted))
                target.SetCurrentValue<TValue>(this, converted);
        }

        internal override IDisposable RouteBind(
            AvaloniaObject target,
            IObservable<object?> source,
            BindingPriority priority)
        {
            return target.Bind<TValue>(this, source, priority);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.ImplicitTypeConversionSupressWarningMessage)]
        private bool ShouldSetValue(AvaloniaObject target, object? value, [NotNullWhen(true)] out TValue? converted)
        {
            if (value != BindingOperations.DoNothing)
            {
                if (value == UnsetValue)
                {
                    target.ClearValue(this);
                }
                else if (TypeUtilities.TryConvertImplicit(PropertyType, value, out var v))
                {
                    converted = (TValue)v!;
                    return true;
                }
                else
                {
                    ThrowInvalidValue(Name, value, nameof(value));
                }
            }

            converted = default;
            return false;
        }
    }
}
