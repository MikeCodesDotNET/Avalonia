using System;
using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// Provides data for <see cref="AvaloniaObject.PropertyChanged"/> events, describing what property
    /// changed, the old and new values, and the priority at which the change occurred.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the base class for property change notifications in Avalonia's property system. The generic
    /// <see cref="AvaloniaPropertyChangedEventArgs{T}"/> provides strongly-typed access to values.
    /// </para>
    /// <para>
    /// Property changes can represent either effective value changes (changes visible to property consumers)
    /// or intermediate changes (such as updates to non-active priority levels). Use
    /// <see cref="IsEffectiveValueChange"/> to distinguish between these cases.
    /// </para>
    /// </remarks>
    /// <seealso cref="AvaloniaPropertyChangedEventArgs{T}"/>
    public abstract class AvaloniaPropertyChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaPropertyChangedEventArgs"/> class.
        /// </summary>
        /// <param name="sender">
        /// The object on which the property changed. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="priority">
        /// The priority at which the property value changed.
        /// </param>
        public AvaloniaPropertyChangedEventArgs(
            AvaloniaObject sender,
            BindingPriority priority)
        {
            Sender = sender;
            Priority = priority;
            IsEffectiveValueChange = true;
        }

        internal AvaloniaPropertyChangedEventArgs(
            AvaloniaObject sender,
            BindingPriority priority,
            bool isEffectiveValueChange)
        {
            Sender = sender;
            Priority = priority;
            IsEffectiveValueChange = isEffectiveValueChange;
        }

        /// <summary>
        /// Gets the object on which the property changed.
        /// </summary>
        /// <value>
        /// The <see cref="AvaloniaObject"/> that raised the property change event. Never <see langword="null"/>.
        /// </value>
        public AvaloniaObject Sender { get; private set; }

        /// <summary>
        /// Gets the property that changed.
        /// </summary>
        /// <value>
        /// The <see cref="AvaloniaProperty"/> that changed. Never <see langword="null"/>.
        /// </value>
        public AvaloniaProperty Property => GetProperty();

        /// <summary>
        /// Gets the old value of the property before the change.
        /// </summary>
        /// <value>
        /// The previous value, or <see cref="AvaloniaProperty.UnsetValue"/> if the property had no value.
        /// May be <see langword="null"/> if the property type is nullable.
        /// </value>
        public object? OldValue => GetOldValue();

        /// <summary>
        /// Gets the new value of the property after the change.
        /// </summary>
        /// <value>
        /// The new value, or <see cref="AvaloniaProperty.UnsetValue"/> if the property was cleared.
        /// May be <see langword="null"/> if the property type is nullable.
        /// </value>
        public object? NewValue => GetNewValue();

        /// <summary>
        /// Gets the priority level at which the property value changed.
        /// </summary>
        /// <value>
        /// The <see cref="BindingPriority"/> at which the value was set, such as
        /// <see cref="BindingPriority.Animation"/>, <see cref="BindingPriority.LocalValue"/>,
        /// or <see cref="BindingPriority.Style"/>.
        /// </value>
        public BindingPriority Priority { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this change affected the property's effective value.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the change modified the value returned by
        /// <see cref="AvaloniaObject.GetValue(AvaloniaProperty)"/>; otherwise <see langword="false"/>
        /// if the change only affected a non-active priority level.
        /// </value>
        /// <remarks>
        /// A property can have multiple values at different priorities. A change is an effective value
        /// change only if it occurs at the highest active priority. Changes to lower priorities do not
        /// affect the effective value and this property will be <see langword="false"/>.
        /// </remarks>
        internal bool IsEffectiveValueChange { get; private set; }
        
        /// <summary>
        /// Sets the Sender property.
        /// This is purely for reuse in some code paths where multiple allocations may occur.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        internal void SetSender(AvaloniaObject sender)
        {
            Sender = sender;
        }

        protected abstract AvaloniaProperty GetProperty();
        protected abstract object? GetOldValue();
        protected abstract object? GetNewValue();
    }
}
