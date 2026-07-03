using Arora.Workflow.Domain.ValueObjects;

namespace Arora.Workflow.Domain.Aggregates;

/// <summary>
/// Represents the blueprint for a category of workflow.
/// A WorkflowDefinition describes the steps, states, and transitions that all
/// instances of that workflow type will follow.
/// </summary>
/// <remarks>
/// <para>
/// A WorkflowDefinition is immutable once Published. Any change to a
/// Published definition requires creating a new version via <see cref="CreateNewVersion"/>.
/// Existing instances always execute against the version they were started with.
/// </para>
/// <para>
/// The lifecycle of a definition:
/// <c>Draft</c> → <c>Published</c> → <c>Deprecated</c>
/// </para>
/// </remarks>
public sealed class WorkflowDefinition
{
    // -------------------------------------------------------------------------
    // Identity
    // -------------------------------------------------------------------------

    /// <summary>The unique identifier of this definition.</summary>
    public Guid Id { get; private set; }

    /// <summary>The tenant this definition belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// The name of this workflow type. Together with <see cref="Version"/>,
    /// uniquely identifies a definition within a tenant.
    /// Example: <c>"invoice-approval"</c>, <c>"vendor-onboarding"</c>.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// The version number. Starts at 1, increments with each published change.
    /// All instances of a definition continue executing on the version they started with.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>An optional human-readable description of this workflow.</summary>
    public string? Description { get; private set; }

    // -------------------------------------------------------------------------
    // Status
    // -------------------------------------------------------------------------

    /// <summary>The lifecycle status of this definition.</summary>
    public WorkflowDefinitionStatus Status { get; private set; }

    // -------------------------------------------------------------------------
    // Steps and States (loaded from DefinitionJson by the engine)
    // -------------------------------------------------------------------------

    /// <summary>
    /// The serialized step/transition graph for this definition.
    /// Stored as JSON in the database; deserialized by the engine at runtime.
    /// Not intended for direct consumption by host applications.
    /// </summary>
    public string DefinitionJson { get; private set; } = default!;

    // -------------------------------------------------------------------------
    // Timestamps
    // -------------------------------------------------------------------------

    /// <summary>The UTC time this definition was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The actor who created this definition.</summary>
    public string CreatedBy { get; private set; } = default!;

    /// <summary>The UTC time this definition was last modified.</summary>
    public DateTimeOffset ModifiedAt { get; private set; }

    /// <summary>The actor who last modified this definition.</summary>
    public string ModifiedBy { get; private set; } = default!;

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    private WorkflowDefinition() { }

    /// <summary>
    /// Creates a new WorkflowDefinition in <c>Draft</c> status.
    /// Call <see cref="Publish"/> once the definition is complete and validated.
    /// </summary>
    public static WorkflowDefinition Create(
        Guid tenantId,
        string name,
        int version,
        string? description,
        string definitionJson,
        string createdBy,
        DateTimeOffset clock)
    {
        return new WorkflowDefinition
        {
            Id             = Guid.NewGuid(),
            TenantId       = tenantId,
            Name           = name,
            Version        = version,
            Description    = description,
            Status         = WorkflowDefinitionStatus.Draft,
            DefinitionJson = definitionJson,
            CreatedAt      = clock,
            CreatedBy      = createdBy,
            ModifiedAt     = clock,
            ModifiedBy     = createdBy
        };
    }

    // -------------------------------------------------------------------------
    // Behaviours
    // -------------------------------------------------------------------------

    /// <summary>
    /// Publishes this definition, making it available for new workflow instances.
    /// A Published definition is immutable — further changes require a new version.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the definition is not in <c>Draft</c> status.
    /// </exception>
    public void Publish(string publishedBy, DateTimeOffset clock)
    {
        if (Status != WorkflowDefinitionStatus.Draft)
            throw new InvalidOperationException(
                $"Only a Draft definition can be published. Current status: {Status}.");

        Status     = WorkflowDefinitionStatus.Published;
        ModifiedAt = clock;
        ModifiedBy = publishedBy;
    }

    /// <summary>
    /// Deprecates this definition. Deprecated definitions cannot start new instances.
    /// Existing running instances continue to completion.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the definition is not in <c>Published</c> status.
    /// </exception>
    public void Deprecate(string deprecatedBy, DateTimeOffset clock)
    {
        if (Status != WorkflowDefinitionStatus.Published)
            throw new InvalidOperationException(
                $"Only a Published definition can be deprecated. Current status: {Status}.");

        Status     = WorkflowDefinitionStatus.Deprecated;
        ModifiedAt = clock;
        ModifiedBy = deprecatedBy;
    }

    /// <summary>
    /// Creates a new Draft version of this definition with an incremented version number.
    /// The new version starts in <c>Draft</c> status and can be edited before publishing.
    /// </summary>
    /// <param name="createdBy">The actor creating the new version.</param>
    /// <param name="clock">The current UTC time.</param>
    /// <returns>A new Draft WorkflowDefinition with version incremented by 1.</returns>
    public WorkflowDefinition CreateNewVersion(string createdBy, DateTimeOffset clock)
    {
        return new WorkflowDefinition
        {
            Id             = Guid.NewGuid(),
            TenantId       = TenantId,
            Name           = Name,
            Version        = Version + 1,
            Description    = Description,
            Status         = WorkflowDefinitionStatus.Draft,
            DefinitionJson = DefinitionJson,  // inherits the current definition as starting point
            CreatedAt      = clock,
            CreatedBy      = createdBy,
            ModifiedAt     = clock,
            ModifiedBy     = createdBy
        };
    }

    /// <summary>Returns true if this definition can accept new workflow instances.</summary>
    public bool CanStartNewInstances() => Status == WorkflowDefinitionStatus.Published;

    /// <inheritdoc/>
    public override string ToString() =>
        $"WorkflowDefinition '{Name}' v{Version} [{Status}]";
}
