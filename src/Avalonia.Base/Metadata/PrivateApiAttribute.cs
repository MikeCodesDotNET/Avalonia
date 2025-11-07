using System;

namespace Avalonia.Metadata;

/// <summary>
/// Indicates that a type or member is part of Avalonia's internal API and is not intended for
/// use by application code.
/// </summary>
/// <remarks>
/// <para>
/// Types and members marked with this attribute are public for technical reasons (such as cross-assembly
/// access within Avalonia itself) but are not part of the stable public API. They may change or be
/// removed in any release without notice.
/// </para>
/// <para>
/// Application developers should not depend on APIs marked with this attribute. Use documented public
/// APIs instead. If functionality is only available through a private API, consider requesting a public
/// API addition at https://github.com/AvaloniaUI/Avalonia/issues.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Constructor
                | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Struct)]
public sealed class PrivateApiAttribute : Attribute
{

}