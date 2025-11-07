# XML Documentation Reviewer Guide

## Purpose

This document describes the standards, rules, and decisions applied during the XML documentation audit of Avalonia.Base. Use this guide to review the documentation changes and as a reference for future documentation work.

---

## Core Documentation Philosophy

### 1. Intent Over Implementation

**Good**: "Clears the local value of a property, allowing lower-priority values to take effect."
**Bad**: "Calls _values.ClearValue(property)."

**Principle**: Document *why* and *when* to use an API, not *what* the code literally does. Developers can read the code for implementation details.

### 2. Framework Context for Newcomers

**Good**: "Styled properties support multiple concurrent values at different priorities. Common priorities include Animation (highest), LocalValue (set by code), and Style (set by stylesheets)."
**Bad**: "A styled property."

**Principle**: Avalonia has unique concepts (priorities, inheritance trees, routed events, etc.) that may be unfamiliar even to experienced .NET developers. Documentation should explain these concepts in context.

### 3. Behavioral Precision

**Good**: "Returns the effective value from the highest active priority, which may be a local value, style, animation, inherited value, or the default value."
**Bad**: "Gets the value."

**Principle**: Explain exactly what happens, especially for non-obvious behaviors. Include information about resolution order, fallback behavior, and edge cases.

---

## Required Documentation Tags

### For All Public Types (Class, Struct, Interface, Enum, Delegate)

```xml
/// <summary>
/// Brief 1-2 sentence description of what this type represents.
/// </summary>
/// <remarks>
/// <para>
/// Extended explanation with context, usage guidance, and framework concepts.
/// </para>
/// <para>
/// Additional paragraphs as needed for complex types.
/// </para>
/// </remarks>
/// <threadsafety>
/// Thread safety statement if relevant (especially for mutable types).
/// </threadsafety>
/// <seealso cref="RelatedType"/>
```

**Summary**: One or two sentences maximum. State what it is and its primary purpose.

**Remarks**: As much detail as needed. Include:
- How it fits into the framework
- Common usage patterns
- Important behaviors or characteristics
- Performance implications if relevant
- Relationships to other types

**Threadsafety**: For AvaloniaObject-derived types, always note "This type is not thread-safe. All members must be accessed from the UI thread only."

**Seealso**: Cross-reference closely related types. Don't overuse; include only direct relationships.

### For All Public Properties

```xml
/// <summary>
/// Brief description of what the property represents.
/// </summary>
/// <value>
/// Description of the property value, including type, nullability, and typical values.
/// </value>
/// <remarks>
/// Extended explanation of behavior, effects of setting, and usage guidance.
/// </remarks>
/// <exception cref="ExceptionType">
/// When and why this exception is thrown (for properties with setters that can throw).
/// </exception>
```

**Summary**: State what the property represents, not "Gets or sets the X" (that's implied by it being a property).

**Value**: Describe the actual value - its type, whether it can be null, its range, units, or typical values.

**Remarks**: Explain behavior, especially for setters with side effects.

### For All Public Methods

```xml
/// <summary>
/// Brief description of what the method does (present tense, active voice).
/// </summary>
/// <param name="parameterName">
/// Description of the parameter, including:
/// - What it represents and how it's used
/// - Whether null is allowed
/// - Valid ranges, units, or formats
/// - Ownership semantics (copied, captured, observed)
/// </param>
/// <returns>
/// Description of the return value, including:
/// - What it represents
/// - Nullability
/// - Ownership semantics
/// - Typical values or special cases
/// </returns>
/// <remarks>
/// Extended explanation of behavior, side effects, and usage guidance.
/// Include algorithm complexity if relevant (O(n), O(1), etc.).
/// </remarks>
/// <exception cref="ArgumentNullException">
/// Thrown if <paramref name="parameterName"/> is <see langword="null"/>.
/// </exception>
/// <exception cref="InvalidOperationException">
/// Specific condition that triggers this exception.
/// </exception>
/// <seealso cref="RelatedMethod"/>
```

**Summary**: Active voice, present tense. "Clears the value" not "This method clears the value."

**Param**: Every parameter must be documented. Include:
- Semantic meaning
- Nullability using `<see langword="null"/>`
- Valid ranges or constraints
- Whether the value is captured (stored) or observed (read once)

**Returns**: Always document return values except for `void`. Include:
- What it represents
- Nullability
- Special values (empty collection vs. null, IDisposable semantics, etc.)

**Remarks**: Any behavior not obvious from the signature. Include:
- Side effects (property changes, event raising, etc.)
- Thread safety requirements
- Performance characteristics for hot paths
- Relationships between parameters

**Exception**: Document all exceptions that can be thrown directly by the method. Include:
- Specific exception type
- Exact conditions that trigger it (parameter values, object state, etc.)
- Reference parameters by name using `<paramref name="..."/>`

### For Generic Type Parameters

```xml
/// <typeparam name="T">
/// Description of the type parameter, including constraints and typical uses.
/// </typeparam>
```

Always document generic type parameters, especially constraints and typical instantiations.

---

## Style Guidelines

### 1. Present Tense, Active Voice

**Good**: "Returns the effective value"
**Bad**: "This method will return the effective value"
**Bad**: "The effective value is returned"

### 2. Concise Summaries

**Summary**: 1-2 sentences maximum. Save details for `<remarks>`.

**Good summary**: "Clears the local value of a property, allowing lower-priority values to take effect."

**Bad summary**: "This method clears the local value of a styled property. When you call this method, it removes the value set via SetValue. After clearing, the property resolves its value from the next highest priority source. For styled properties, this clears all values at LocalValue priority. For direct properties, this resets the property to its unset value."

(The bad example belongs in `<remarks>`, not `<summary>`.)

### 3. Nullability

Always use `<see langword="null"/>` instead of writing "null" in plain text.

**Good**: "Returns <see langword="null"/> if the value is not set."
**Bad**: "Returns null if the value is not set."

Also use `<see langword="true"/>`, `<see langword="false"/>`, and `<see langword="default"/>`.

### 4. Cross-References

Use `<see cref="..."/>` for all type, member, and parameter references.

**Good**: "See <see cref="AvaloniaObject.SetValue"/> for details."
**Bad**: "See SetValue for details."

**Good**: "Thrown if <paramref name="property"/> is null."
**Bad**: "Thrown if property is null."

### 5. Lists and Structure

Use structured markup for clarity:

```xml
<para>
Multiple values sources are evaluated in priority order:
</para>
<list type="number">
<item><description>Animation - Highest priority</description></item>
<item><description>LocalValue - Set by code</description></item>
<item><description>Style - Set by stylesheets</description></item>
</list>
```

Use `<para>` to separate paragraphs within `<remarks>`.

Use `<list type="bullet">` or `<list type="number">` for lists.

### 6. Code Examples

Include `<example>` only when it genuinely clarifies usage. Don't include trivial examples.

**Good use case**: Complex binding scenarios, non-obvious API combinations, subtle behaviors

**Bad use case**: Showing `obj.Property = value;` when the property is self-explanatory

```xml
/// <example>
/// <code>
/// // Set an animated value
/// using var subscription = myControl.SetValue(
///     Control.OpacityProperty,
///     0.5,
///     BindingPriority.Animation);
///
/// // Dispose to remove the animated value
/// subscription.Dispose();
/// </code>
/// </example>
```

---

## Avalonia-Specific Terminology

Use consistent terminology for Avalonia concepts:

| Concept | Standard Term |
|---------|---------------|
| Property system | "Avalonia property system" (first reference), "property system" thereafter |
| Property descriptor | "property" or "property descriptor" (not "dependency property") |
| Property value sources | "value sources" or "value priorities" |
| Highest active value | "effective value" |
| Object tree for inheritance | "inheritance tree" or "inheritance parent chain" |
| Object tree for visuals | "visual tree" |
| Object tree for logical structure | "logical tree" |
| Setting a value | "setting" or "assigning" (not "writing") |
| Getting a value | "getting" or "retrieving" (not "reading") |
| Removing a value | "clearing" |
| Style-like properties | "styled properties" |
| CLR-backed properties | "direct properties" |
| Properties set on any object | "attached properties" |
| WPF equivalents | Note in `<remarks>` as "This is analogous to [WPF type/member]" |

---

## Framework Concepts to Explain

When documenting Avalonia.Base APIs, explain these concepts in context:

### 1. Property Value Priorities

Styled properties support multiple concurrent values at different priorities:

1. **Animation** (highest) - Set by active animations
2. **LocalValue** - Set by code or XAML attributes
3. **Template** - Set by control templates
4. **Style** - Set by styles
5. **Inherited** - Inherited from parent (if property is inheritable)
6. **Default** - Specified in metadata

The effective value is determined by the highest active priority.

### 2. Property Inheritance

Some properties (like DataContext, FontFamily) inherit values from parent objects. When not locally set, the property system walks up the `InheritanceParent` chain.

**Important**: Inheritance parent is not always the same as visual parent or logical parent.

### 3. Direct vs. Styled Properties

- **Styled properties**: Support styling, priorities, animation, inheritance. Stored in ValueStore.
- **Direct properties**: Wrap CLR properties, no styling support, optimized for performance. Call getter/setter directly.

### 4. Attached Properties

Properties that can be set on any AvaloniaObject, not just the owner type. Common for layout properties (e.g., Grid.Row, Grid.Column).

### 5. Metadata and Overrides

Metadata specifies default values, change callbacks, coercion, and validation. Can be overridden per type using `OverrideMetadata`.

### 6. Binding

Data binding continuously updates property values from observable sources. Bindings have priorities and can be disposed to terminate.

### 7. Coercion vs. Validation

- **Validation**: Returns false to reject invalid values (property system refuses them)
- **Coercion**: Adjusts values to meet constraints (e.g., clamping to range)

Validation runs first, then coercion, then value is committed.

### 8. Thread Affinity

All AvaloniaObject operations must occur on the UI thread. Use `CheckAccess()` to verify or `VerifyAccess()` to enforce.

---

## Common Patterns

### Pattern: Disposable Returns

Many methods return `IDisposable` to enable undo/cleanup:

```xml
/// <returns>
/// An <see cref="IDisposable"/> that, when disposed, removes this value or terminates the binding.
/// </returns>
```

Explain what disposing does.

### Pattern: UnsetValue

`AvaloniaProperty.UnsetValue` is a sentinel indicating "no value at this priority":

```xml
/// <param name="value">
/// The value to set, or <see cref="AvaloniaProperty.UnsetValue"/> to clear the value.
/// </param>
```

Always document UnsetValue handling where relevant.

### Pattern: Priority Parameters

```xml
/// <param name="priority">
/// The priority at which to set the value. Must be between <see cref="BindingPriority.Animation"/>
/// and <see cref="BindingPriority.LocalValue"/> (inclusive). Defaults to <see cref="BindingPriority.LocalValue"/>.
/// </param>
```

Always document valid priority ranges.

### Pattern: Metadata Resolution

```xml
/// <remarks>
/// Metadata is resolved by walking the type hierarchy upward from the runtime type of
/// <paramref name="instance"/> until metadata is found.
/// </remarks>
```

Explain how type-specific metadata is selected.

---

## Exception Documentation Rules

### Document All Public Exceptions

If a method throws an exception that isn't caught internally, document it:

```xml
/// <exception cref="ArgumentNullException">
/// Thrown if <paramref name="property"/> is <see langword="null"/>.
/// </exception>
/// <exception cref="ArgumentException">
/// Thrown if <paramref name="priority"/> is outside the valid range.
/// </exception>
/// <exception cref="InvalidOperationException">
/// Thrown if called from a thread other than the UI thread.
/// </exception>
```

### Be Specific

**Good**: "Thrown if <paramref name="property"/> is a read-only direct property."
**Bad**: "Thrown if the property is invalid."

### Reference Parameters

Always use `<paramref name="..."/>` when referencing parameters in exception docs.

---

## Testing and Validation

Documentation was validated against:

1. **Source code**: Every documented behavior verified in implementation
2. **Method signatures**: Params, returns, and exceptions match actual code
3. **Call sites**: Common usage patterns reviewed
4. **Tests**: Unit test behavior checked where available
5. **Framework patterns**: Consistency with established Avalonia conventions

**No speculation**: If behavior couldn't be verified from code/tests, it was not documented or marked as uncertain.

---

## Notable Decisions

### 1. Thread Safety Documentation

Decided to always include explicit thread safety notes for AvaloniaObject-derived types rather than assuming developers know about UI thread affinity. This makes the documentation more accessible to newcomers.

### 2. WPF Analogies

Included WPF analogies (e.g., "AvaloniaObject is analogous to DependencyObject in WPF") to help WPF developers transition, but kept them in `<remarks>` rather than `<summary>` to avoid confusion for developers without WPF background.

### 3. Performance Notes

Added performance notes for hot paths (e.g., GetValue, SetValue) and recommendations for preferring generic overloads. Performance documentation helps developers make informed choices.

### 4. Property vs. "Dependency Property"

Used "Avalonia property" or "styled property" rather than "dependency property" to avoid confusion with WPF terminology, since Avalonia's property system has some differences.

### 5. Comprehensive Exception Documentation

Documented all exceptions that can be thrown, even common ones like `ArgumentNullException`, because it makes the documentation complete and enables tools like IntelliSense to show the information.

---

## Review Checklist

When reviewing XML documentation changes, verify:

- [ ] Summary is concise (1-2 sentences) and explains intent
- [ ] All parameters documented with nullability and constraints
- [ ] Return values documented with nullability and semantics
- [ ] All exceptions documented with specific conditions
- [ ] Remarks explain non-obvious behavior and framework context
- [ ] Thread safety requirements stated for mutable types
- [ ] Cross-references use `<see cref="..."/>`
- [ ] Nullability uses `<see langword="null"/>`
- [ ] Present tense, active voice used consistently
- [ ] Avalonia terminology consistent with guide
- [ ] No speculation - all behaviors verified against code
- [ ] No duplicate information between summary and remarks
- [ ] American English spelling throughout

---

## Examples of Good Documentation

### Example 1: Property with Complex Behavior

```xml
/// <summary>
/// Gets or sets the parent object from which inheritable property values are inherited.
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
protected internal AvaloniaObject? InheritanceParent { get; set; }
```

### Example 2: Method with Multiple Overloads

```xml
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
```

### Example 3: Complex Method with Side Effects

```xml
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
```

---

## Conclusion

This guide establishes the standards applied to XML documentation in Avalonia.Base. Follow these patterns to maintain consistency and quality when documenting additional APIs. The goal is complete, accurate, accessible documentation that helps both newcomers and experienced developers use Avalonia effectively.
