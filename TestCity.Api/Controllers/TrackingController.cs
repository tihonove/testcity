using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Kontur.TestCity.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Kontur.TestCity.Api.Controllers;

[ApiController]
[Route("api/track")]
public class TrackingController(ILogger<TrackingController> logger) : ControllerBase
{
    private readonly ILogger<TrackingController> logger = logger;
    private static readonly Meter Meter = new("UserActivityMetrics");
    private static readonly Counter<long> RouteVisitsCounter = Meter.CreateCounter<long>("route_visits_count");
    private static readonly Histogram<int> UniqueUsersHistogram = Meter.CreateHistogram<int>("unique_users_histogram");
    private static readonly ConcurrentDictionary<string, bool> visitedUsers = new();

    [HttpPost("route")]
    public IActionResult TrackRoute([FromBody] UserTrackingData trackingData)
    {
        logger.LogInformation("Visit {Route}. UserId: {UserId}", trackingData.Route, trackingData.UserId);

        RouteVisitsCounter.Add(1, new KeyValuePair<string, object?>("route", trackingData.Route));
        if (visitedUsers.TryAdd(trackingData.UserId, true))
        {
            UniqueUsersHistogram.Record(visitedUsers.Count);
        }

        return Ok();
    }
}
