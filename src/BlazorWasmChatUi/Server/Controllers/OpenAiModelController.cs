using Azure.AI.OpenAI;
using BlazorWasmChatUi.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.AI.ChatCompletion;

namespace BlazorWasmChatUi.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class OpenAiModelController : ControllerBase
{
    private readonly IChatCompletion _chatCompletion;    
    private readonly ILogger<OpenAiModelController> _logger;

    public OpenAiModelController(
        IChatCompletion chatCompletion,
        ILogger<OpenAiModelController> logger)
    {
        _chatCompletion = chatCompletion;
        _logger = logger;
    }

	[HttpPost]
    public async Task<IActionResult> Post(Shared.ChatMessage[] messages)
    {
        if (messages == null || messages.Length == 0) 
        {
            return BadRequest();
        }

        var history = _chatCompletion.CreateNewChat();

        foreach (var message in messages) 
        {
            if (message?.Role == null || message.Content == null)
            {
                continue;
            }

            AuthorRole? role = null;

            if (message.Role.ToLower() == "user")
            {
                role = AuthorRole.User;
            }
            else if (message.Role.ToLower() == "assistant")
            {
                role = AuthorRole.Assistant;
            }
            else if (message.Role.ToLower() == "tool")
            {
                role = AuthorRole.Tool;
            }
            else if (message.Role.ToLower() == "system")
            {
                role = AuthorRole.System;
            }

            if (role == null)
            {
                continue;
            }

            history.AddMessage(role.Value, message.Content);
        }

        string? replyMessage = null;

        try
        {
            replyMessage = await _chatCompletion.GenerateMessageAsync(history);
        }
        catch(Microsoft.SemanticKernel.Diagnostics.HttpOperationException e) 
        {
            if (e.InnerException is Azure.RequestFailedException innerEx)
            {
                if (innerEx.ErrorCode == "context_length_exceeded")
                {
                    // TODO:
                }                
            }

            _logger.LogWarning(e.ToString());
        }
        catch (Exception e) 
        {
            _logger.LogWarning(e.ToString());
        }
        
        if (replyMessage == null)
        {
            return NotFound();
        }

        var replyChatMessage = new Shared.ChatMessage()
        {
            Id = Guid.NewGuid().ToString(),
            Role = "assistant",
            Content = replyMessage,
            CreatedDateTime = DateTimeOffset.Now,
        };

        return Ok(replyChatMessage);        
    }
}
