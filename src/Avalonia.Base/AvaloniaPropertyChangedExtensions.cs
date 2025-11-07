namespace Avalonia
{
    /// <summary>
    /// Provides extensions for <see cref="AvaloniaPropertyChangedEventArgs"/>.
    /// </summary>
    public static class AvaloniaPropertyChangedExtensions
    {
        /// <summary>
        /// Extracts a strongly-typed old value from a property change event.
        /// </summary>
        /// <typeparam name="T">The expected type of the property value.</typeparam>
        /// <param name="e">
        /// The event arguments to extract the value from. Must be <see cref="AvaloniaPropertyChangedEventArgs{T}"/>.
        /// </param>
        /// <returns>
        /// The old value cast to <typeparamref name="T"/>, or the default value of <typeparamref name="T"/>
        /// if no old value was present.
        /// </returns>
        /// <remarks>
        /// This method casts the event args to <see cref="AvaloniaPropertyChangedEventArgs{T}"/> and extracts
        /// the typed old value, providing a convenient way to work with property change events when the type
        /// is known.
        /// </remarks>
        /// <exception cref="InvalidCastException">
        /// Thrown if <paramref name="e"/> is not an <see cref="AvaloniaPropertyChangedEventArgs{T}"/> or if
        /// the property type does not match <typeparamref name="T"/>.
        /// </exception>
        public static T GetOldValue<T>(this AvaloniaPropertyChangedEventArgs e)
        {
            return ((AvaloniaPropertyChangedEventArgs<T>)e).OldValue.GetValueOrDefault()!;
        }

        /// <summary>
        /// Extracts a strongly-typed new value from a property change event.
        /// </summary>
        /// <typeparam name="T">The expected type of the property value.</typeparam>
        /// <param name="e">
        /// The event arguments to extract the value from. Must be <see cref="AvaloniaPropertyChangedEventArgs{T}"/>.
        /// </param>
        /// <returns>
        /// The new value cast to <typeparamref name="T"/>, or the default value of <typeparamref name="T"/>
        /// if no new value is present.
        /// </returns>
        /// <remarks>
        /// This method casts the event args to <see cref="AvaloniaPropertyChangedEventArgs{T}"/> and extracts
        /// the typed new value, providing a convenient way to work with property change events when the type
        /// is known.
        /// </remarks>
        /// <exception cref="InvalidCastException">
        /// Thrown if <paramref name="e"/> is not an <see cref="AvaloniaPropertyChangedEventArgs{T}"/> or if
        /// the property type does not match <typeparamref name="T"/>.
        /// </exception>
        public static T GetNewValue<T>(this AvaloniaPropertyChangedEventArgs e)
        {
            return ((AvaloniaPropertyChangedEventArgs<T>)e).NewValue.GetValueOrDefault()!;
        }

        /// <summary>
        /// Extracts both old and new strongly-typed values from a property change event.
        /// </summary>
        /// <typeparam name="T">The expected type of the property value.</typeparam>
        /// <param name="e">
        /// The event arguments to extract the values from. Must be <see cref="AvaloniaPropertyChangedEventArgs{T}"/>.
        /// </param>
        /// <returns>
        /// A tuple containing the old and new values, each cast to <typeparamref name="T"/>. If either
        /// value was not present, returns the default value of <typeparamref name="T"/> for that position.
        /// </returns>
        /// <remarks>
        /// This is a convenience method for extracting both values at once when both old and new values
        /// are needed.
        /// </remarks>
        /// <exception cref="InvalidCastException">
        /// Thrown if <paramref name="e"/> is not an <see cref="AvaloniaPropertyChangedEventArgs{T}"/> or if
        /// the property type does not match <typeparamref name="T"/>.
        /// </exception>
        public static (T oldValue, T newValue) GetOldAndNewValue<T>(this AvaloniaPropertyChangedEventArgs e)
        {
            var ev = (AvaloniaPropertyChangedEventArgs<T>)e;
            return (ev.OldValue.GetValueOrDefault()!, ev.NewValue.GetValueOrDefault()!);
        }
    }
}
