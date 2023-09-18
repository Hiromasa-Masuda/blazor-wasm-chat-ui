using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorWasmChatUi.Shared;

public class ChatContext
{   
    public string? Id { get; set; }
    public string? UserId { get; set; }
    public string? Title { get; set; }
    public List<ChatMessage>? Messages { get; set; }
	public DateTimeOffset CreatedDateTime { get; set; }
	public DateTimeOffset UpdatedDateTime { get; set; }
}


public class ChatMessage
{
    public string? Id { get; set; }
    public string? Role { get; set; }
    public string? Content { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
}
