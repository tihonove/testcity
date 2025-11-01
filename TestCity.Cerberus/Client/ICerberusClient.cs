using TestCity.Cerberus.Client.Models;

namespace TestCity.Cerberus.Client;

public interface ICerberusClient
{
    Task<CheckObjectsAllResponse> CheckObjectsAsync(
        CheckObjectsByHierarchyLevelRequest request,
        CancellationToken cancellationToken = default);

    Task<CheckObjectsResponse> CheckObjectsByNameAsync(
        CheckObjectsByNameRequest request,
        CancellationToken cancellationToken = default);
}
