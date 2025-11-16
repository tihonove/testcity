using Microsoft.AspNetCore.Http;
using TestCity.Cerberus.Client;
using TestCity.Cerberus.Client.Models;
using TestCity.Core.GitlabProjects.AccessChecking;

namespace TestCity.Api.Authorization;

public class CerberusGitLabEntityAccessContext(CerberusClient cerberusClient, CerberusSettings cerberusSettings, IHttpContextAccessor httpContextAccessor, ILogger<CerberusGitLabEntityAccessContext> logger) : IGitLabEntityAccessContext
{
    public async Task<List<AccessControlEntry>> ListAccessEntries()
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            var userName = httpContext.User?.Identity?.Name
                        ?? httpContext.User?.FindFirst("preferred_username")?.Value
                        ?? httpContext.User?.FindFirst("name")?.Value
                        ?? httpContext.User?.FindFirst("sub")?.Value;

            SubjectIdentity? subjectIdentity = null;
            if (subjectIdentity == null)
            {
                if (httpContext.Request.Cookies.TryGetValue("auth.sid", out var authSid))
                {
                    logger.LogInformation("Found AuthSid cookie for user {UserName}", userName);
                    subjectIdentity = new AuthSidIdentity
                    {
                        SessionId = authSid
                    };
                }
            }

            if (subjectIdentity == null)
            {
                logger.LogInformation("Unable to determine subject identity for user {UserName}, returning empty access list", userName);
                return [];
            }
            var request = new CheckObjectsByHierarchyLevelRequest
            {
                Service = cerberusSettings.DefaultService ?? "test-service",
                SubjectIdentity = subjectIdentity,
                Operations = ["read-project"],
                HierarchyLevel = 5
            };
            var result = await cerberusClient.CheckObjectsAsync(request);

            return result.Objects.Select(o => new AccessControlEntry(

                o.Object == "/" ? [] : o.Object.Split('/'),
                o.Operations != null && o.Operations.Contains("read-project")
            )).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while checking access entries via Cerberus");
            return [];
        }
    }
}
