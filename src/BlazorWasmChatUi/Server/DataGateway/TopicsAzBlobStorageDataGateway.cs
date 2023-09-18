using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BlazorWasmChatUi.Shared;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;

namespace BlazorWasmChatUi.Server.DataGateway;

public class TopicsAzBlobStorageDataGateway : ITopicsDataGateway
{
    private string _containerName = "topics";
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<TopicsAzBlobStorageDataGateway> _logger;

    private readonly string _anonymousUserId = "anonymous";

    public TopicsAzBlobStorageDataGateway(
        IConfiguration configuration, 
        ILogger<TopicsAzBlobStorageDataGateway> logger)
    {
        var connectionString = configuration["azBlobStorage:connectionString"];
        _containerClient = new BlobContainerClient(connectionString, _containerName);
        _logger = logger;
    }

    public async Task<ChatContext?> FindByIdAsync(string? userId, string contextId, CancellationToken cancellationToken)
    {
        var blobName = this.GetBlobName(userId, contextId);
        var blobClient = _containerClient.GetBlobClient(blobName);
        var response = await blobClient.DownloadContentAsync(cancellationToken).ConfigureAwait(false);

        var json = response.Value.Content.ToString();
        var chatContext = this.FromJson(json);

        return chatContext;
    }

    public async Task<IEnumerable<ChatContext>> FindByUserIdAsync(string? userId, CancellationToken cancellationToken)
    {
        await _containerClient.CreateIfNotExistsAsync().ConfigureAwait(false);

        var prefix = $"{userId ?? _anonymousUserId}/";
        
        var chatContextes = new List<ChatContext>();

        await foreach (BlobItem item in _containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
        {
            var blobClient = _containerClient.GetBlobClient(item.Name);
            var response = await blobClient.DownloadContentAsync(cancellationToken).ConfigureAwait(false);

            var json = response.Value.Content.ToString();
            var chatContext = this.FromJson(json);

            if (chatContext == null ) 
            {
                _logger.LogWarning("Deserialization failed: {itemName}", item.Name);
                continue;
            }

            chatContextes.Add(chatContext);
        }

        return chatContextes;
    }

    public async Task SaveAsync(ChatContext chatContext, CancellationToken cancellationToken)
    {
        await _containerClient.CreateIfNotExistsAsync().ConfigureAwait(false);
        
        string json = this.ToJson(chatContext);
        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        if (chatContext.Id == null)
        {
            throw new InvalidOperationException();
        }

        var blobName = this.GetBlobName(chatContext.UserId, chatContext.Id);
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(stream, true, cancellationToken);
    }

    public async Task<bool> DeleteByIdAsync(string? userId, string contextId, CancellationToken cancellationToken)
    {
        await _containerClient.CreateIfNotExistsAsync().ConfigureAwait(false);

        var blobName = this.GetBlobName(userId, contextId);
        var blobClient = _containerClient.GetBlobClient(blobName);

        var response = await blobClient.DeleteIfExistsAsync().ConfigureAwait(false);

        return response.Value;
    }

    private string GetBlobName(string? userId, string contextId)
    {
        return $"{userId ?? _anonymousUserId}/{contextId}.json";
    }

    private string ToJson(ChatContext chatContext)
    {
        var options = new JsonSerializerOptions
        {
            //WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        string json = JsonSerializer.Serialize(chatContext, options);

        return json;
    }

    private ChatContext? FromJson(string json)
    {
        var options = new JsonSerializerOptions
        {
            //WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var chatContext = JsonSerializer.Deserialize<ChatContext>(json, options);

        return chatContext;
    }
}
