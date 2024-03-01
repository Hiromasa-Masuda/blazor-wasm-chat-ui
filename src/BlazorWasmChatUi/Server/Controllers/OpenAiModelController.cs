using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Data;
using System.Text.Json;

namespace BlazorWasmChatUi.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class OpenAiModelController : ControllerBase
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly ILogger<OpenAiModelController> _logger;

    public OpenAiModelController(
        IChatCompletionService chatCompletionService,
        ILogger<OpenAiModelController> logger)
    {
        _chatCompletionService = chatCompletionService;
        _logger = logger;
    }

	[HttpPost]
    public async Task<IActionResult> Post(Shared.ChatMessage[] messages)
    {
        if (messages == null || messages.Length == 0) 
        {
            return BadRequest();
        }        

        ChatHistory history = this.ToChatHistory(messages);

        string? replyMessage = null;

        try
        {
            var chatMessageContent = await _chatCompletionService.GetChatMessageContentAsync(history);
            replyMessage = chatMessageContent.Content;
        }        
        catch (Exception e) 
        {
            _logger.LogError(e, "OpenAiModelController.Post() - An error occurred.");
            return this.CreateResult(e);
        }
        
        if (replyMessage == null)
        {
            return NotFound();
        }

        Shared.ChatMessage replyChatMessage = new()
        {
            Id = Guid.NewGuid().ToString(),
            Role = "assistant",
            Content = replyMessage,
            CreatedDateTime = DateTimeOffset.Now,
        };

        return Ok(replyChatMessage);        
    }

    [HttpPost]
    [Route("streaming")]
    public async Task PostStreaming([FromBody] Shared.ChatMessage[] messages, CancellationToken cancellationToken)
    {
        if (messages == null || messages.Length == 0)
        {            
            Response.StatusCode = 400;
            return;
        }

        ChatHistory history = this.ToChatHistory(messages);

        Response.Headers.AppendCommaSeparatedValues("Cache-Control", "no-cache", "must-revalidate");                
        Response.Headers.Append("Content-Type", "text/event-stream");        
        Response.StatusCode = 200;

        var writer = new StreamWriter(Response.Body);

        string replyMessage = string.Empty;

        var messageId = Guid.NewGuid().ToString();
        string? messageRole = null;

        try
        {
            await foreach (var message in _chatCompletionService
                .GetStreamingChatMessageContentsAsync(new ChatHistory(history))
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                if (message == null)
                {
                    continue;
                }

                if (message.Role != null)
                {
                    messageRole = message.Role?.ToString().ToLower();
                }

                replyMessage += message.Content;

                var json = new { id = messageId, role = messageRole, content = message.Content, createdDateTime = DateTimeOffset.Now };

                await writer.WriteAsync($"data: {JsonSerializer.Serialize(json)}\n\n");
                await writer.FlushAsync();
            }
        }
        catch (Exception e) 
        {
            _logger.LogError(e, "OpenAiModelController.StreamingPost() - An error occurred.");
            var errorResult = this.CreateResult(e);

            Response.StatusCode = errorResult.StatusCode ?? 500;
            await Response.WriteAsJsonAsync(errorResult.Value, cancellationToken).ConfigureAwait(false);

            return;
        }

        await writer.WriteLineAsync("data: [DONE]");
        await writer.FlushAsync();        

        if (string.IsNullOrEmpty(replyMessage))
        {            
            Response.StatusCode = 404;
            return;
        }        
    }

    private ChatHistory ToChatHistory(Shared.ChatMessage[] messages)
    {
        var history = messages.Select(msg =>
        {
            if (msg?.Role == null)
            {
                return null;
            }

            AuthorRole? role = null;

            if (msg.Role.ToLower() == "user")
            {
                role = AuthorRole.User;
            }
            else if (msg.Role.ToLower() == "assistant")
            {
                role = AuthorRole.Assistant;
            }
            else if (msg.Role.ToLower() == "tool")
            {
                role = AuthorRole.Tool;
            }
            else if (msg.Role.ToLower() == "system")
            {
                role = AuthorRole.System;
            }

            if (role == null)
            {
                return null;
            }

            return new ChatMessageContent(role.Value, msg.Content);
        })
         .Where(cm => cm != null)
         .Cast<ChatMessageContent>();

        return new ChatHistory(history!);
    }

    private ObjectResult CreateResult(Exception ex)
    {
        if (ex is Microsoft.SemanticKernel.HttpOperationException httpOperationException)
        {
            if (httpOperationException.InnerException is Azure.RequestFailedException innerEx)
            {
                if (innerEx.ErrorCode == "context_length_exceeded")
                {                    
                    return BadRequest(httpOperationException.ResponseContent);
                }
            }

            return Problem(httpOperationException.ResponseContent);
        }
        else
        {
            return Problem(ex.Message);
        }
    }

}
