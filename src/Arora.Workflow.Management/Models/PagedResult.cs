using System.Collections.Generic;

namespace Arora.Workflow.Management.Models;

/// <summary>
/// A paginated result set.
/// </summary>
public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
