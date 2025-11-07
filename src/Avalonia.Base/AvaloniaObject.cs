using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.PropertyStore;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// Base class for objects that support Avalonia's property system, including styled properties,
    /// direct properties, attached properties, property inheritance, data binding, and change notifications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is analogous to DependencyObject in WPF. It provides the foundation for Avalonia's
    /// property system, which includes:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Styled properties that support styling, theming, and property value precedence</description></item>
    /// <item><description>Direct properties that wrap CLR properties for binding and notifications</description></item>
    /// <item><description>Attached properties that can be set on any AvaloniaObject</description></item>
    /// <item><description>Property value inheritance through the inheritance tree</description></item>
    /// <item><description>Data binding with multiple priority levels</description></item>
    /// <item><description>Property change notifications via INotifyPropertyChanged</description></item>
    /// </list>
    /// <para>
    /// All property operations must be performed on the UI thread. Use <see cref="CheckAccess"/> to
    /// verify thread affinity or <see cref="VerifyAccess"/> to enforce it.
    /// </para>
    /// </remarks>
    /// <threadsafety>
    /// This type is not thread-safe. All members must be accessed from the UI thread only.
    /// </threadsafety>
    [DebuggerDisplay("{DebugDisplay}")]
    public class AvaloniaObject : IAvaloniaObjectDebug, INotifyPropertyChanged
    {
        private readonly ValueStore _values;
        private AvaloniaObject? _inheritanceParent;
        private PropertyChangedEventHandler? _inpcChanged;
        private EventHandler<AvaloniaPropertyChangedEventArgs>? _propertyChanged;
        private List<AvaloniaObject>? _inheritanceChildren;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaObject"/> class.
        /// </summary>
        /// <remarks>
        /// Creates the internal property value store. Must be called from the UI thread.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called from a thread other than the UI thread.
        /// </exception>
        public AvaloniaObject()
        {
            VerifyAccess();
            _values = new ValueStore(this);
        }

        /// <summary>
        /// Raised when an <see cref="AvaloniaProperty"/> value changes on this object.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This event is raised for both effective value changes (changes visible to property consumers)
        /// and intermediate value changes (such as changes to non-active binding priorities). Handlers
        /// should check <see cref="AvaloniaPropertyChangedEventArgs.IsEffectiveValueChange"/> to determine
        /// if the change affects the property's effective value.
        /// </para>
        /// <para>
        /// This event is raised after the property value has been updated and after the
        /// <see cref="OnPropertyChanged"/> virtual method has been invoked.
        /// </para>
        /// </remarks>
        public event EventHandler<AvaloniaPropertyChangedEventArgs>? PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }

        /// <summary>
        /// Raised when an <see cref="AvaloniaProperty"/> value changes on this object. This is the
        /// explicit implementation of <see cref="INotifyPropertyChanged.PropertyChanged"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This event provides a lighter-weight notification using standard <see cref="PropertyChangedEventArgs"/>
        /// instead of <see cref="AvaloniaPropertyChangedEventArgs"/>. Only effective value changes are reported.
        /// </para>
        /// <para>
        /// Prefer the strongly-typed <see cref="PropertyChanged"/> event when working with Avalonia properties directly.
        /// </para>
        /// </remarks>
        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add { _inpcChanged += value; }
            remove { _inpcChanged -= value; }
        }

        /// <summary>
        /// Gets or sets the parent object from which inheritable <see cref="AvaloniaProperty"/> values
        /// are inherited.
        /// </summary>
        /// <value>
        /// The inheritance parent, or <see langword="null"/> if this object has no inheritance parent.
        /// </value>
        /// <remarks>
        /// <para>
        /// The inheritance parent determines where inheritable property values are resolved from when
        /// not explicitly set on this object. This is typically the logical parent in the tree, but
        /// can be set independently of the logical or visual tree structure.
        /// </para>
        /// <para>
        /// When changed, all inheritable properties on this object and its inheritance children are
        /// re-evaluated to reflect values from the new parent.
        /// </para>
        /// <para>
        /// Must be accessed from the UI thread.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if accessed from a thread other than the UI thread.
        /// </exception>
        protected internal AvaloniaObject? InheritanceParent
        {
            get
            {
                return _inheritanceParent;
            }

            set
            {
                VerifyAccess();

                if (_inheritanceParent != value)
                {
                    _inheritanceParent?.RemoveInheritanceChild(this);
                    _inheritanceParent = value;
                    _inheritanceParent?.AddInheritanceChild(this);
                    _values.SetInheritanceParent(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the value of an <see cref="AvaloniaProperty"/> using indexer syntax.
        /// </summary>
        /// <param name="property">The property to get or set. Must not be <see langword="null"/>.</param>
        /// <value>
        /// The current effective value of the property, or <see langword="null"/> if the property
        /// has no value set and the default value is <see langword="null"/>.
        /// </value>
        /// <remarks>
        /// This indexer provides convenient syntax for property access. For typed access with better
        /// performance, use the generic <see cref="GetValue{T}"/> and <see cref="SetValue{T}"/> overloads.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        public object? this[AvaloniaProperty property]
        {
            get { return GetValue(property); }
            set { SetValue(property, value); }
        }

        /// <summary>
        /// Gets or sets a binding for an <see cref="AvaloniaProperty"/> using indexer syntax.
        /// </summary>
        /// <param name="binding">
        /// The binding descriptor containing the property and binding mode. Must not be <see langword="null"/>.
        /// </param>
        /// <value>
        /// Getting returns a new <see cref="IndexerBinding"/> instance. Setting establishes the binding.
        /// </value>
        /// <remarks>
        /// This indexer enables XAML binding syntax. It is rarely used in code; prefer
        /// <see cref="Bind(AvaloniaProperty, IBinding)"/> for programmatic binding.
        /// </remarks>
        public IBinding this[IndexerDescriptor binding]
        {
            get { return new IndexerBinding(this, binding.Property!, binding.Mode); }
            set { this.Bind(binding.Property!, value); }
        }

        /// <summary>
        /// Gets a string to display inside the debugger for this object.
        /// </summary>
        internal string DebugDisplay => GetDebugDisplay(true);

        /// <summary>
        /// Determines whether the calling thread has access to this object.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the calling thread is the UI thread; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// All <see cref="AvaloniaObject"/> operations must be performed on the UI thread. Use this
        /// method to check thread affinity before accessing properties or calling methods if needed.
        /// To enforce thread affinity and throw an exception on violation, use <see cref="VerifyAccess"/> instead.
        /// </remarks>
        /// <seealso cref="VerifyAccess"/>
        public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

        /// <summary>
        /// Verifies that the calling thread has access to this object.
        /// </summary>
        /// <remarks>
        /// All <see cref="AvaloniaObject"/> operations must be performed on the UI thread. This method
        /// enforces thread affinity by throwing an exception if called from any thread other than the UI thread.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the calling thread is not the UI thread.
        /// </exception>
        /// <seealso cref="CheckAccess"/>
        public void VerifyAccess() => Dispatcher.UIThread.VerifyAccess();

        /// <summary>
        /// Clears the local value of an <see cref="AvaloniaProperty"/>, allowing lower-priority
        /// values to take effect.
        /// </summary>
        /// <param name="property">
        /// The property to clear. Must not be <see langword="null"/>.
        /// </param>
        /// <remarks>
        /// <para>
        /// This method removes the local value set via <see cref="SetValue(AvaloniaProperty, object, BindingPriority)"/>.
        /// After clearing, the property will resolve its value from the next highest priority source,
        /// such as styles, animations, inherited values, or the default value.
        /// </para>
        /// <para>
        /// For styled properties, this clears all values at <see cref="BindingPriority.LocalValue"/> priority.
        /// For direct properties, this resets the property to its unset value.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called from a thread other than the UI thread.
        /// </exception>
        public void ClearValue(AvaloniaProperty property)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));
            VerifyAccess();
            _values.ClearValue(property);
        }

        /// <summary>
        /// Clears the local value of an <see cref="AvaloniaProperty"/> with type information.
        /// </summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="property">
        /// The property to clear. Must not be <see langword="null"/>.
        /// </param>
        /// <remarks>
        /// This is a typed overload of <see cref="ClearValue(AvaloniaProperty)"/> that dispatches
        /// to the appropriate clear method based on the property type (styled or direct).
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called from a thread other than the UI thread.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown if the property is not a <see cref="StyledProperty{T}"/> or <see cref="DirectPropertyBase{T}"/>.
        /// </exception>
        public void ClearValue<T>(AvaloniaProperty<T> property)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));
            VerifyAccess();

            switch (property)
            {
                case StyledProperty<T> styled:
                    ClearValue(styled);
                    break;
                case DirectPropertyBase<T> direct:
                    ClearValue(direct);
                    break;
                default:
                    throw new NotSupportedException("Unsupported AvaloniaProperty type.");
            }
        }

        /// <summary>
        /// Clears a <see cref="AvaloniaProperty"/>'s local value.
        /// </summary>
        /// <param name="property">The property.</param>
        public void ClearValue<T>(StyledProperty<T> property)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));
            VerifyAccess();

            _values.ClearValue(property);
        }

        /// <summary>
        /// Clears a <see cref="AvaloniaProperty"/>'s local value.
        /// </summary>
        /// <param name="property">The property.</param>
        public void ClearValue<T>(DirectPropertyBase<T> property)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));
            VerifyAccess();

            var p = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);
            p.InvokeSetter(this, p.GetUnsetValue(this));
        }

        /// <summary>
        /// Compares two objects using reference equality.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <remarks>
        /// Overriding Equals and GetHashCode on an AvaloniaObject is disallowed for two reasons:
        /// 
        /// - AvaloniaObjects are by their nature mutable
        /// - The presence of attached properties means that the semantics of equality are
        ///   difficult to define
        /// 
        /// See https://github.com/AvaloniaUI/Avalonia/pull/2747 for the discussion that prompted
        /// this.
        /// </remarks>
        public sealed override bool Equals(object? obj) => base.Equals(obj);

        /// <summary>
        /// Gets the hash code for the object.
        /// </summary>
        /// <remarks>
        /// Overriding Equals and GetHashCode on an AvaloniaObject is disallowed for two reasons:
        /// 
        /// - AvaloniaObjects are by their nature mutable
        /// - The presence of attached properties means that the semantics of equality are
        ///   difficult to define
        /// 
        /// See https://github.com/AvaloniaUI/Avalonia/pull/2747 for the discussion that prompted
        /// this.
        /// </remarks>
        public sealed override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// Gets the current effective value of an <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="property">
        /// The property to get. Must not be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// The current effective value of the property, which may come from local values, styles,
        /// animations, inheritance, or the default value. Returns <see langword="null"/> if the
        /// effective value is <see langword="null"/>.
        /// </returns>
        /// <remarks>
        /// For styled properties, returns the value with the highest active priority. For direct
        /// properties, invokes the property's getter. For better performance with known property types,
        /// use the generic overloads <see cref="GetValue{T}(StyledProperty{T})"/> or
        /// <see cref="GetValue{T}(DirectPropertyBase{T})"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        public object? GetValue(AvaloniaProperty property)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));

            if (property.IsDirect)
                return property.RouteGetValue(this);
            else
                return _values.GetValue(property);
        }

        /// <summary>
        /// Gets the current effective value of a <see cref="StyledProperty{T}"/>.
        /// </summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="property">
        /// The property to get. Must not be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// The current effective value from the highest active priority, which may be a local value,
        /// style, animation, inherited value, or the default value.
        /// </returns>
        /// <remarks>
        /// This generic overload provides better type safety and performance than the non-generic
        /// <see cref="GetValue(AvaloniaProperty)"/> method.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called from a thread other than the UI thread.
        /// </exception>
        public T GetValue<T>(StyledProperty<T> property)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));
            VerifyAccess();
            return _values.GetValue(property);
        }

        /// <summary>
        /// Gets the current value of a <see cref="DirectPropertyBase{T}"/> by invoking its getter.
        /// </summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="property">
        /// The property to get. Must not be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// The current value returned by the property's getter.
        /// </returns>
        /// <remarks>
        /// Direct properties wrap CLR properties, so this method invokes the underlying getter.
        /// The returned value reflects the current state of the backing field or computed value.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called from a thread other than the UI thread.
        /// </exception>
        public T GetValue<T>(DirectPropertyBase<T> property)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));
            VerifyAccess();

            var registered = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);
            return registered.InvokeGetter(this);
        }

        /// <summary>
        /// Gets an <see cref="AvaloniaProperty"/> base value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// Gets the value of the property excluding animated values, otherwise <see cref="Optional{T}.Empty"/>.
        /// Note that this method does not return property values that come from inherited or default values.
        /// </remarks>
        public Optional<T> GetBaseValue<T>(StyledProperty<T> property)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));
            VerifyAccess();
            return _values.GetBaseValue(property);
        }

        /// <summary>
        /// Checks whether a <see cref="AvaloniaProperty"/> is animating.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is animating, otherwise false.</returns>
        public bool IsAnimating(AvaloniaProperty property)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));

            VerifyAccess();

            return _values.IsAnimating(property);
        }

        /// <summary>
        /// Checks whether a <see cref="AvaloniaProperty"/> is set on this object.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is set, otherwise false.</returns>
        /// <remarks>
        /// Returns true if <paramref name="property"/> is a styled property which has a value
        /// assigned to it or a binding targeting it; otherwise false.
        /// </remarks>
        public bool IsSet(AvaloniaProperty property)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));

            VerifyAccess();

            return _values.IsSet(property);
        }

        /// <summary>
        /// Sets the value of an <see cref="AvaloniaProperty"/> at a specified priority.
        /// </summary>
        /// <param name="property">
        /// The property to set. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="value">
        /// The value to set. May be <see langword="null"/> if the property type is nullable.
        /// </param>
        /// <param name="priority">
        /// The priority at which to set the value. Must be between <see cref="BindingPriority.Animation"/>
        /// and <see cref="BindingPriority.LocalValue"/> (inclusive). Defaults to <see cref="BindingPriority.LocalValue"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IDisposable"/> that can be disposed to revert this value assignment, or
        /// <see langword="null"/> if the property is a direct property.
        /// </returns>
        /// <remarks>
        /// For styled properties, values are stored with priority and can be overridden by higher-priority
        /// sources such as animations. For direct properties, the priority parameter is ignored and the
        /// setter is invoked directly.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="priority"/> is outside the valid range.
        /// </exception>
        public IDisposable? SetValue(
            AvaloniaProperty property,
            object? value,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));

            return property.RouteSetValue(this, value, priority);
        }

        /// <summary>
        /// Sets the value of a <see cref="StyledProperty{T}"/> at a specified priority.
        /// </summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="property">
        /// The property to set. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="value">
        /// The value to set. May be <see cref="AvaloniaProperty.UnsetValue"/> to clear the value at
        /// the specified priority, or <see langword="null"/> if T is nullable.
        /// </param>
        /// <param name="priority">
        /// The priority at which to set the value. Must be between <see cref="BindingPriority.Animation"/>
        /// and <see cref="BindingPriority.LocalValue"/> (inclusive). Defaults to <see cref="BindingPriority.LocalValue"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IDisposable"/> that, when disposed, removes this value from the specified priority,
        /// or <see langword="null"/> if the value was <see cref="AvaloniaProperty.UnsetValue"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Styled properties support multiple concurrent values at different priorities. The property's
        /// effective value is determined by the highest active priority. Common priorities:
        /// </para>
        /// <list type="bullet">
        /// <item><description><see cref="BindingPriority.Animation"/> (highest) - Set by active animations</description></item>
        /// <item><description><see cref="BindingPriority.LocalValue"/> - Set by code or XAML attributes</description></item>
        /// <item><description><see cref="BindingPriority.Style"/> - Set by styles</description></item>
        /// </list>
        /// <para>
        /// This method validates the value and raises property change notifications if the effective value changes.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="priority"/> is outside the valid range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if called from a thread other than the UI thread.
        /// </exception>
        public IDisposable? SetValue<T>(
            StyledProperty<T> property,
            T value,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));
            VerifyAccess();
            ValidatePriority(priority);

            LogPropertySet(property, value, priority);

            if (value is UnsetValueType)
            {
                if (priority == BindingPriority.LocalValue)
                    _values.ClearValue(property);
            }
            else if (value is not DoNothingType)
            {
                return _values.SetValue(property, value, priority);
            }

            return null;
        }

        /// <summary>
        /// Sets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        public void SetValue<T>(DirectPropertyBase<T> property, T value)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));
            VerifyAccess();

            property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);
            LogPropertySet(property, value, BindingPriority.LocalValue);
            SetDirectValueUnchecked(property, value);
        }

        /// <summary>
        /// Sets the value of a dependency property without changing its value source.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <remarks>
        /// This method is used by a component that programmatically sets the value of one of its
        /// own properties without disabling an application's declared use of the property. The
        /// method changes the effective value of the property, but existing data bindings and
        /// styles will continue to work.
        /// 
        /// The new value will have the property's current <see cref="BindingPriority"/>, even if
        /// that priority is <see cref="BindingPriority.Unset"/> or 
        /// <see cref="BindingPriority.Inherited"/>.
        /// </remarks>
        public void SetCurrentValue(AvaloniaProperty property, object? value) => 
            property.RouteSetCurrentValue(this, value);

        /// <summary>
        /// Sets the value of a dependency property without changing its value source.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <remarks>
        /// This method is used by a component that programmatically sets the value of one of its
        /// own properties without disabling an application's declared use of the property. The
        /// method changes the effective value of the property, but existing data bindings and
        /// styles will continue to work.
        /// 
        /// The new value will have the property's current <see cref="BindingPriority"/>, even if
        /// that priority is <see cref="BindingPriority.Unset"/> or 
        /// <see cref="BindingPriority.Inherited"/>.
        /// </remarks>
        public void SetCurrentValue<T>(StyledProperty<T> property, T value)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));
            VerifyAccess();

            LogPropertySet(property, value, BindingPriority.LocalValue);

            if (value is UnsetValueType)
            {
                _values.ClearValue(property);
            }
            else if (value is not DoNothingType)
            {
                _values.SetCurrentValue(property, value);
            }
        }

        /// <summary>
        /// Establishes a data binding between an <see cref="AvaloniaProperty"/> and an <see cref="IBinding"/> source.
        /// </summary>
        /// <param name="property">
        /// The target property to bind. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="binding">
        /// The binding source that provides values. Must not be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// A <see cref="BindingExpressionBase"/> that represents the active binding. Dispose this to remove the binding.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method creates a binding that continuously updates the property as the binding source changes.
        /// The binding remains active until the returned expression is disposed or the object is garbage collected.
        /// </para>
        /// <para>
        /// The <paramref name="binding"/> determines the priority, mode (one-way, two-way, etc.), and other
        /// binding characteristics. For styled properties, the binding value competes with other value sources
        /// according to its priority.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="property"/> or <paramref name="binding"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown if <paramref name="binding"/> is not a supported <see cref="IBinding"/> implementation.
        /// </exception>
        public BindingExpressionBase Bind(AvaloniaProperty property, IBinding binding)
        {
            return Bind(property, binding, null);
        }

        /// <summary>
        /// Binds an <see cref="AvaloniaProperty"/> to an observable sequence of values.
        /// </summary>
        /// <param name="property">
        /// The target property to bind. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="source">
        /// An observable that produces values for the property. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="priority">
        /// The priority at which to bind the values. Must be between <see cref="BindingPriority.Animation"/>
        /// and <see cref="BindingPriority.LocalValue"/>. Defaults to <see cref="BindingPriority.LocalValue"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IDisposable"/> that, when disposed, terminates the binding subscription.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method subscribes to the observable and updates the property with each emitted value.
        /// The binding is active until disposed. If the observable completes or errors, the binding
        /// remains at the last successfully emitted value.
        /// </para>
        /// <para>
        /// For styled properties, bound values at different priorities can coexist. For direct properties,
        /// the priority parameter is ignored.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="property"/> or <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="property"/> is a read-only direct property, or if <paramref name="priority"/>
        /// is outside the valid range.
        /// </exception>
        public IDisposable Bind(
            AvaloniaProperty property,
            IObservable<object?> source,
            BindingPriority priority = BindingPriority.LocalValue) => property.RouteBind(this, source, priority);

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public IDisposable Bind<T>(
            StyledProperty<T> property,
            IObservable<object?> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            return TryBindStyledPropertyUntyped(property, source, priority)
                ?? _values.AddBinding(property, source, priority);
        }

        // Non-generic path extracted to avoid unnecessary generic code duplication
        private BindingExpressionBase? TryBindStyledPropertyUntyped(
            AvaloniaProperty property,
            IObservable<object?> source,
            BindingPriority priority)
        {
            Debug.Assert(!property.IsDirect);
            ThrowHelper.ThrowIfNull(property, nameof(property));
            ThrowHelper.ThrowIfNull(source, nameof(source));
            VerifyAccess();
            ValidatePriority(priority);

            if (source is IBinding2 b)
            {
                if (b.Instance(this, property, null) is not UntypedBindingExpressionBase expression)
                    throw new NotSupportedException($"Binding returned unsupported {nameof(BindingExpressionBase)}.");

                if (priority != expression.Priority)
                {
                    throw new NotSupportedException(
                        $"The binding priority passed to AvaloniaObject.Bind ('{priority}') " +
                        "conflicts with the binding priority of the provided binding expression " +
                        $" ({expression.Priority}').");
                }

                return GetValueStore().AddBinding(property, expression);
            }

            return null;
        }

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public IDisposable Bind<T>(
            StyledProperty<T> property,
            IObservable<T> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));
            ThrowHelper.ThrowIfNull(source, nameof(source));
            VerifyAccess();
            ValidatePriority(priority);

            return _values.AddBinding(property, source, priority);
        }

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public IDisposable Bind<T>(
            StyledProperty<T> property,
            IObservable<BindingValue<T>> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));
            ThrowHelper.ThrowIfNull(source, nameof(source));
            VerifyAccess();
            ValidatePriority(priority);

            return _values.AddBinding(property, source, priority);
        }

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public IDisposable Bind<T>(
            DirectPropertyBase<T> property,
            IObservable<object?> source)
        {
            AvaloniaProperty untypedProperty = property;

            return TryBindDirectPropertyUntyped(ref untypedProperty, source)
                ?? _values.AddBinding((DirectPropertyBase<T>)untypedProperty, source);
        }

        // Non-generic path extracted to avoid unnecessary generic code duplication
        private BindingExpressionBase? TryBindDirectPropertyUntyped(
            ref AvaloniaProperty property,
            IObservable<object?> source)
        {
            Debug.Assert(property.IsDirect);
            ThrowHelper.ThrowIfNull(property, nameof(property));
            VerifyAccess();

            property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirectUntyped(this, property);

            if (property.IsReadOnly)
            {
                throw new ArgumentException($"The property {property.Name} is readonly.");
            }

            if (source is IBinding2 b)
            {
                if (b.Instance(this, property, null) is not UntypedBindingExpressionBase expression)
                    throw new NotSupportedException($"Binding returned unsupported {nameof(BindingExpressionBase)}.");
                return GetValueStore().AddBinding(property, expression);
            }

            return null;
        }

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public IDisposable Bind<T>(
            DirectPropertyBase<T> property,
            IObservable<T> source)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));
            VerifyAccess();

            property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);

            if (property.IsReadOnly)
            {
                throw new ArgumentException($"The property {property.Name} is readonly.");
            }

            return _values.AddBinding(property, source);
        }

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public IDisposable Bind<T>(
            DirectPropertyBase<T> property,
            IObservable<BindingValue<T>> source)
        {
            ThrowHelper.ThrowIfNull(property, nameof(property));
            VerifyAccess();

            property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);

            if (property.IsReadOnly)
            {
                throw new ArgumentException($"The property {property.Name} is readonly.");
            }

            return _values.AddBinding(property, source);
        }

        /// <summary>
        /// Coerces the specified <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        public void CoerceValue(AvaloniaProperty property) => _values.CoerceValue(property);

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an <see cref="IBinding"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="binding">The binding.</param>
        /// <param name="anchor">
        /// An optional anchor from which to locate required context. When binding to objects that
        /// are not in the logical tree, certain types of binding need an anchor into the tree in 
        /// order to locate named controls or resources. The <paramref name="anchor"/> parameter 
        /// can be used to provide this context.
        /// </param>
        /// <returns>
        /// The binding expression which represents the binding instance on this object.
        /// </returns>
        internal BindingExpressionBase Bind(AvaloniaProperty property, IBinding binding, object? anchor)
        {
            if (binding is not IBinding2 b)
                throw new NotSupportedException($"Unsupported IBinding implementation '{binding}'.");
            if (b.Instance(this, property, anchor) is not UntypedBindingExpressionBase expression)
                throw new NotSupportedException($"Binding returned unsupported {nameof(BindingExpressionBase)}.");

            return GetValueStore().AddBinding(property, expression);
        }

        internal void AddInheritanceChild(AvaloniaObject child)
        {
            _inheritanceChildren ??= new List<AvaloniaObject>();
            _inheritanceChildren.Add(child);
        }

        internal void RemoveInheritanceChild(AvaloniaObject child)
        {
            _inheritanceChildren?.Remove(child);
        }

        /// <inheritdoc/>
        Delegate[]? IAvaloniaObjectDebug.GetPropertyChangedSubscribers()
        {
            return _propertyChanged?.GetInvocationList();
        }

        internal AvaloniaPropertyValue GetDiagnosticInternal(AvaloniaProperty property)
        {
            if (property.IsDirect)
            {
                return new AvaloniaPropertyValue(
                    property,
                    GetValue(property),
                    BindingPriority.LocalValue,
                    null,
                    false);
            }

            return _values.GetDiagnostic(property);
        }

        internal ValueStore GetValueStore() => _values;
        internal IReadOnlyList<AvaloniaObject>? GetInheritanceChildren() => _inheritanceChildren;

        /// <summary>
        /// Called to update the validation state for properties for which data validation is
        /// enabled.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="state">The current data binding state.</param>
        /// <param name="error">The current data binding error, if any.</param>
        protected virtual void UpdateDataValidation(
            AvaloniaProperty property,
            BindingValueType state,
            Exception? error)
        {
        }

        /// <summary>
        /// Called when a avalonia property changes on the object.
        /// </summary>
        /// <param name="change">The property change details.</param>
        protected virtual void OnPropertyChangedCore(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.IsEffectiveValueChange)
            {
                OnPropertyChanged(change);
            }
        }

        /// <summary>
        /// Called when a avalonia property changes on the object.
        /// </summary>
        /// <param name="change">The property change details.</param>
        protected virtual void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for a direct property.
        /// </summary>
        /// <param name="property">The property that has changed.</param>
        /// <param name="oldValue">The old property value.</param>
        /// <param name="newValue">The new property value.</param>
        protected void RaisePropertyChanged<T>(
            DirectPropertyBase<T> property,
            T oldValue,
            T newValue)
        {
            RaisePropertyChanged(property, oldValue, newValue, BindingPriority.LocalValue, true);
        }

        /// <summary>
        /// This is an optimized path for <see cref="RaisePropertyChanged{T}(Avalonia.DirectPropertyBase{T},T,T)"/>.
        /// This will reuse the event args in situations where many allocations would otherwise happen.
        /// </summary>
        /// <param name="args">Avalonia property change args</param>
        /// <param name="inpcArgs">INPC event args/</param>
        internal void RaisePropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> args, PropertyChangedEventArgs? inpcArgs)
        {
            OnPropertyChangedCore(args);

            if (args.IsEffectiveValueChange && inpcArgs is not null)
            {
                args.Property.NotifyChanged(args);
                _propertyChanged?.Invoke(this, args);
                _inpcChanged?.Invoke(this, inpcArgs);
            }
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="property">The property that has changed.</param>
        /// <param name="oldValue">The old property value.</param>
        /// <param name="newValue">The new property value.</param>
        /// <param name="priority">The priority of the binding that produced the value.</param>
        /// <param name="isEffectiveValue">
        /// Whether the notification represents a change to the effective value of the property.
        /// </param>
        internal void RaisePropertyChanged<T>(
            AvaloniaProperty<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority,
            bool isEffectiveValue)
        {
            var e = new AvaloniaPropertyChangedEventArgs<T>(
                this,
                property,
                oldValue,
                newValue,
                priority,
                isEffectiveValue);

            OnPropertyChangedCore(e);

            if (isEffectiveValue)
            {
                property.NotifyChanged(e);
                _propertyChanged?.Invoke(this, e);
                _inpcChanged?.Invoke(this, new PropertyChangedEventArgs(property.Name));
            }
        }

        /// <summary>
        /// Sets the backing field for a direct avalonia property, raising the 
        /// <see cref="PropertyChanged"/> event if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="field">The backing field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// True if the value changed, otherwise false.
        /// </returns>
        protected bool SetAndRaise<T>(DirectPropertyBase<T> property, ref T field, T value)
        {
            VerifyAccess();

            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            var old = field;
            field = value;
            RaisePropertyChanged(property, old, value, BindingPriority.LocalValue, true);
            return true;
        }

        /// <summary>
        /// Sets the value of a direct property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        internal void SetDirectValueUnchecked<T>(DirectPropertyBase<T> property, T value)
        {
            if (value is UnsetValueType)
            {
                property.InvokeSetter(this, property.GetUnsetValue(this));
            }
            else if (!(value is DoNothingType))
            {
                property.InvokeSetter(this, value);
            }
        }

        /// <summary>
        /// Sets the value of a direct property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        internal void SetDirectValueUnchecked<T>(DirectPropertyBase<T> property, BindingValue<T> value)
        {
            switch (value.Type)
            {
                case BindingValueType.UnsetValue:
                case BindingValueType.BindingError:
                    var fallback = value.HasValue ? value : value.WithValue(property.GetUnsetValue(this));
                    property.InvokeSetter(this, fallback);
                    break;
                case BindingValueType.Value:
                case BindingValueType.BindingErrorWithFallback:
                case BindingValueType.DataValidationError:
                case BindingValueType.DataValidationErrorWithFallback:
                    property.InvokeSetter(this, value);
                    break;
            }

            var metadata = property.GetMetadata(this);

            if (metadata.EnableDataValidation == true)
            {
                UpdateDataValidation(property, value.Type, value.Error);
            }
        }

        internal void OnUpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
        {
            UpdateDataValidation(property, state, error);
        }

        /// <summary>
        /// Gets a description of an observable that can be used in logs.
        /// </summary>
        /// <param name="o">The observable.</param>
        /// <returns>The description.</returns>
        private string GetDescription(object o)
        {
            var description = o as IDescription;
            return description?.Description ?? o.ToString() ?? o.GetType().Name;
        }

        /// <summary>
        /// Logs a property set message.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The new value.</param>
        /// <param name="priority">The priority.</param>
        private void LogPropertySet<T>(AvaloniaProperty<T> property, T value, BindingPriority priority)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.Property)?.Log(
                this,
                "Set {Property} to {$Value} with priority {Priority}",
                property,
                value,
                priority);
        }

        internal string GetDebugDisplay(bool includeContent)
        {
            var builder = new StringBuilder();
            BuildDebugDisplay(builder, includeContent);
            return builder.ToString();
        }

        internal virtual void BuildDebugDisplay(StringBuilder builder, bool includeContent)
        {
            var type = GetType();

            if (type.Namespace is { } ns &&
                (ns == "Avalonia" || ns.StartsWith("Avalonia.", StringComparison.Ordinal)))
            {
                builder.Append(type.Name);
            }
            else
            {
                builder.Append(ToString());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidatePriority(BindingPriority priority)
        {
            if (priority < BindingPriority.Animation || priority >= BindingPriority.Inherited)
                ThrowInvalidPriority(priority);
        }

        private static void ThrowInvalidPriority(BindingPriority priority)
        {
            throw new ArgumentException($"Invalid priority ${priority}", nameof(priority));
        }
    }
}
