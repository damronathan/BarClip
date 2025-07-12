using Azure.Storage.Queues.Models;
using BarClip.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BarClipFunction;

public class BarClipQueueTrigger
{
    private readonly ILogger<BarClipQueueTrigger> _logger;
    private IVideoProcessingService _videoService;
    private IApiClientService _client;

    public BarClipQueueTrigger(ILogger<BarClipQueueTrigger> logger, IVideoProcessingService videoService, IApiClientService client)
    {
        _logger = logger;
        _videoService = videoService;
        _client = client;
    }

    [Function(nameof(BarClipQueueTrigger))]

    public async Task Run([QueueTrigger("new-video", Connection = "AzureWebJobsStorage")] QueueMessage message)
        {
        var request = await _videoService.TrimVideoFromStorage(message.MessageText);
        var response = await _client.SaveVideosAsync(request);
    }

}