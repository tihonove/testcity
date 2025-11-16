using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TestCity.Cerberus.Client.Models;

namespace TestCity.Cerberus.Client;

public class CerberusClient(Uri cerberusEndpoint, String apiKey, ILogger logger) : ICerberusClient, IDisposable
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = cerberusEndpoint,
        DefaultRequestHeaders = { { "X-Kontur-Apikey", apiKey } }
    };
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<CheckObjectsAllResponse> CheckObjectsAsync(
        CheckObjectsByHierarchyLevelRequest request,
        CancellationToken cancellationToken = default)
    {
        const string endpoint = "/cerberus/v2.1/check/objects/all";

        try
        {
            logger.LogDebug("Calling Cerberus check objects endpoint for service {Service}", request.Service);

            var response = await httpClient.PostAsJsonAsync(endpoint, request, jsonOptions, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CheckObjectsAllResponse>(jsonOptions, cancellationToken);
                if (result == null)
                {
                    throw new CerberusException("Failed to deserialize response from Cerberus");
                }

                logger.LogDebug("Successfully checked objects, received {Count} objects", result.Objects.Length);
                return result;
            }

            // 403 означает отсутствие доступа к объектам - возвращаем пустой список
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                logger.LogDebug("No access to any objects for service {Service}, returning empty list", request.Service);
                return new CheckObjectsAllResponse { Objects = [] };
            }

            await HandleErrorResponseAsync(response, cancellationToken);

            // Unreachable, but needed for compiler
            throw new CerberusException("Unexpected error from Cerberus");
        }
        catch (Exception ex) when (ex is not CerberusException)
        {
            logger.LogError(ex, "Error calling Cerberus API");
            throw new CerberusException("Error calling Cerberus API", ex);
        }
    }

    public async Task<CheckObjectsResponse> CheckObjectsByNameAsync(
        CheckObjectsByNameRequest request,
        CancellationToken cancellationToken = default)
    {
        const string endpoint = "/cerberus/v2.1/check/objects";

        try
        {
            logger.LogDebug("Calling Cerberus check objects by name endpoint for service {Service}, objects: {Objects}", request.Service, string.Join(", ", request.Objects));

            var response = await httpClient.PostAsJsonAsync(endpoint, request, jsonOptions, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CheckObjectsResponse>(jsonOptions, cancellationToken);
                if (result == null)
                {
                    throw new CerberusException("Failed to deserialize response from Cerberus");
                }

                logger.LogDebug("Successfully checked objects by name, received {Count} objects", result.Objects.Length);
                return result;
            }

            await HandleErrorResponseAsync(response, cancellationToken);

            // Unreachable, but needed for compiler
            throw new CerberusException("Unexpected error from Cerberus");
        }
        catch (Exception ex) when (ex is not CerberusException)
        {
            logger.LogError(ex, "Error calling Cerberus API");
            throw new CerberusException("Error calling Cerberus API", ex);
        }
    }

    private async Task HandleErrorResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var statusCode = response.StatusCode;
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        logger.LogWarning("Cerberus returned error: {StatusCode}, Content: {Content}", statusCode, content);

        switch (statusCode)
        {
            case HttpStatusCode.Unauthorized:
                var authError = TryDeserialize<AuthenticationError>(content);
                throw new CerberusAuthenticationException("Authentication failed")
                {
                    ErrorStatus = authError?.ErrorStatus,
                    ErrorFixUrl = authError?.ErrorFixUrl,
                    ErrorMessages = authError?.ErrorMessages
                };

            case HttpStatusCode.Forbidden:
                var accessError = TryDeserialize<AccessDeniedError>(content);
                var errorMessage = accessError?.ErrorMessages != null && accessError.ErrorMessages.Length > 0
                    ? string.Join("; ", accessError.ErrorMessages)
                    : "Access denied";

                throw new CerberusAccessDeniedException(errorMessage)
                {
                    ErrorStatus = accessError?.ErrorStatus,
                    ErrorFixUrl = accessError?.ErrorFixUrl,
                    ErrorMessages = accessError?.ErrorMessages
                };

            case HttpStatusCode.InternalServerError:
                throw new CerberusException($"Internal server error: {content}");

            case HttpStatusCode.ServiceUnavailable:
                throw new CerberusException($"Service unavailable: {content}");

            default:
                throw new CerberusException($"Unexpected error from Cerberus: {statusCode}, {content}");
        }
    }

    private T? TryDeserialize<T>(string content) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(content, jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
