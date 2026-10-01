using AegisSchedule.Api.DTOs;

namespace AegisSchedule.Api.Integrations;

public interface ISisImportService
{
    Task<SisBatchSyncResult> SyncBatchAsync(SisBatchSyncRequest request, CancellationToken cancellationToken = default);
}
