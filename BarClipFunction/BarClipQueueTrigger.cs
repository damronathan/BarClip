using Azure.Storage.Queues.Models;
using BarClip.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BarClipFunction;

public class BarClipQueueTrigger
{
    private readonly ILogger<BarClipQueueTrigger> _logger;
    private IVideoService _videoService;

    public BarClipQueueTrigger(ILogger<BarClipQueueTrigger> logger, IVideoService videoService)
    {
        _logger = logger;
        _videoService = videoService;
    }

    [Function(nameof(BarClipQueueTrigger))]

    public async Task Run([QueueTrigger("new-video", Connection = "AzureWebJobsStorage")] QueueMessage message)
    {
        var request = await _videoService.TrimVideoFromStorage(message.MessageText);

    }

}