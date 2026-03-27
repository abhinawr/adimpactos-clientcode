using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using AdImpactOs.Models;

namespace AdImpactOs.Functions.S2S;

public class S2STrackerOutput
{
    public HttpResponseData? HttpResponse { get; set; }

    [EventHubOutput("ad-impressions", Connection = "EventHubConnection")]
    public TrackingResponse? EventData { get; set; }
}
