using BlazorWasmChatUi.Shared;

namespace BlazorWasmChatUi.Server.DataGateway;

public interface ITopicsDataGateway
{
    public Task<IEnumerable<ChatContext>> FindByUserIdAsync(string? userId, CancellationToken cancellationToken);
    public Task SaveAsync(ChatContext chatContext, CancellationToken cancellationToken);
    public Task<bool> DeleteByIdAsync(string? userId, string contextId, CancellationToken cancellationToken);
}
