using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using AdImpactOs.Models;

namespace AdImpactOs.Functions.Pixel;

public class PixelTrackerOutput
{
    public HttpResponseData? HttpResponse { get; set; }

    [EventHubOutput("ad-impressions", Connection = "EventHubConnection")]
    public TrackingResponse? EventData { get; set; }
}