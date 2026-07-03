using Arora.Workflow.Application.Interfaces;

namespace Arora.Workflow.Testing;

/// <summary>
/// A fixed <see cref="ITenantContext"/> for use in unit tests.
/// Always returns the same tenant ID.
/// </summary>
public sealed class FakeTenantContext : ITenantContext
{
    /// <summary>
    /// The default tenant ID used in tests:
    /// <c>11111111-1111-1111-1111-111111111111</c>.
    /// </summary>
    /// <remarks>
    /// The repeated-digit pattern makes it visually distinct in test output.
    /// When you see this ID, you know it's the test tenant.
    /// </remarks>
    public static readonly Guid DefaultTenantId =
        new("11111111-1111-1111-1111-111111111111");

    /// <inheritdoc />
    public Guid TenantId { get; }

    /// <summary>Creates a context with the default test tenant ID.</summary>
    public FakeTenantContext() : this(DefaultTenantId) { }

    /// <summary>Creates a context with a specific tenant ID.</summary>
    /// <param name="tenantId">The tenant ID to use in tests.</param>
    public FakeTenantContext(Guid tenantId)
    {
        TenantId = tenantId;
    }
}
