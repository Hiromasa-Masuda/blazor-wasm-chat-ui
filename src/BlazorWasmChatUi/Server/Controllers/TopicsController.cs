using BlazorWasmChatUi.Server.DataGateway;
using BlazorWasmChatUi.Shared;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWasmChatUi.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class TopicsController : Controller
{
    private readonly ITopicsDataGateway _topicsDataGateway;
    private readonly ILogger<TopicsController> _logger;

    public TopicsController(
        ITopicsDataGateway topicsDataGateway,
        ILogger<TopicsController> logger)
    {
        _topicsDataGateway = topicsDataGateway;
        _logger = logger;
        
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        CancellationToken cancellationToken = default;

        string? userId = null;

        var chatContextes = await _topicsDataGateway.FindByUserIdAsync(userId, cancellationToken);

        return Ok(chatContextes);
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync(ChatContext context)
    {
        if (context?.Messages == null || context.Messages.Count == 0)
        {
            return BadRequest();
        }

        CancellationToken cancellationToken = default;

        await _topicsDataGateway.SaveAsync(context, cancellationToken);

        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync(ChatContext context)
    {
        CancellationToken cancellationToken = default;

        string? userId = null;

        var result = await _topicsDataGateway.DeleteByIdAsync(userId, context.Id, cancellationToken);

        if (!result)
        {
            return NotFound();
        }

        return Ok();
    }
}
