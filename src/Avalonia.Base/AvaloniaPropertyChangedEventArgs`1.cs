using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// Provides strongly-typed data for property change events, with type-safe access to old and new values.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <remarks>
    /// This generic version of <see cref="AvaloniaPropertyChangedEventArgs"/> provides typed access to
    /// property values, avoiding boxing for value types and providing compile-time type safety.
    /// </remarks>
    public class AvaloniaPropertyChangedEventArgs<T> : AvaloniaPropertyChangedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaPropertyChangedEventArgs{T}"/> class.
        /// </summary>
        /// <param name="sender">
        /// The object on which the property changed. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="property">
        /// The property that changed. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="oldValue">
        /// The old value of the property, or <see cref="Optional{T}.Empty"/> if the property had no value.
        /// </param>
        /// <param name="newValue">
        /// The new value of the property as a <see cref="BindingValue{T}"/>, which may represent a value,
        /// an error, or special markers like <see cref="BindingValue{T}.DoNothing"/>.
        /// </param>
        /// <param name="priority">
        /// The priority at which the property value changed.
        /// </param>
        public AvaloniaPropertyChangedEventArgs(
            AvaloniaObject sender,
            AvaloniaProperty<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority)
            : this(sender, property, oldValue, newValue, priority, true)
        {
        }

        internal AvaloniaPropertyChangedEventArgs(
            AvaloniaObject sender,
            AvaloniaProperty<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority,
            bool isEffectiveValueChange)
            : base(sender, priority, isEffectiveValueChange)
        {
            Property = property;
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Gets the strongly-typed property that changed.
        /// </summary>
        /// <value>
        /// The <see cref="AvaloniaProperty{T}"/> that changed. Never <see langword="null"/>.
        /// </value>
        public new AvaloniaProperty<T> Property { get; }

        /// <summary>
        /// Gets the old value of the property before the change.
        /// </summary>
        /// <value>
        /// An <see cref="Optional{T}"/> containing the previous value, or <see cref="Optional{T}.Empty"/>
        /// if the property had no value before this change.
        /// </value>
        public new Optional<T> OldValue { get; private set; }

        /// <summary>
        /// Gets the new value of the property after the change.
        /// </summary>
        /// <value>
        /// A <see cref="BindingValue{T}"/> containing the new value. Check <see cref="BindingValue{T}.HasValue"/>
        /// to determine if the value is valid, or if it represents an error or special marker.
        /// </value>
        public new BindingValue<T> NewValue { get; private set; }

        protected override AvaloniaProperty GetProperty() => Property;

        protected override object? GetOldValue() => OldValue.GetValueOrDefault(AvaloniaProperty.UnsetValue);

        protected override object? GetNewValue() => NewValue.GetValueOrDefault(AvaloniaProperty.UnsetValue);
    }
}
