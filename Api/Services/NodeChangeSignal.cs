using System.Collections.Concurrent;
using HinataProject.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HinataProject.Api.Services;

public class NodeChangeSignal
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _signals = new();
    private readonly IDbContextFactory<HinataProjectDataContext> _dbFactory;

    public NodeChangeSignal(IDbContextFactory<HinataProjectDataContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Waits for a signal that a node has changed, or until the cancellation token is cancelled.
    /// </summary>
    public async Task<bool> WaitAsync(string userId, int timeoutSeconds, CancellationToken ct)
    {
        var signal = _signals.GetOrAdd(userId, _ => new SemaphoreSlim(0));

        try
        {
            return await signal.WaitAsync(TimeSpan.FromSeconds(timeoutSeconds), ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// Notifies all users assigned to the specified node that it has changed.
    /// </summary>
    public async Task NotifyForNodeAsync(Guid nodeId, CancellationToken ct)
    {
        using var db = await _dbFactory.CreateDbContextAsync(ct);
        var assigneeUserIds = await db.NodeAssignees
            .Where(na => na.NodeId == nodeId)
            .Select(na => na.UserId)
            .ToListAsync(ct);

        foreach (var userId in assigneeUserIds)
        {
            // Convert int userId to string key
            var key = userId.ToString();
            if (_signals.TryGetValue(key, out var signal))
            {
                // Release one waiter (if any)
                try
                {
                    signal.Release();
                }
                catch (SemaphoreFullException)
                {
                    // Already released, ignore
                }
            }
        }
    }

    /// <summary>
    /// Synchronous version for fire-and-forget scenarios.
    /// </summary>
    public Task NotifyForNode(Guid nodeId)
    {
        return NotifyForNodeAsync(nodeId, CancellationToken.None);
    }
}
