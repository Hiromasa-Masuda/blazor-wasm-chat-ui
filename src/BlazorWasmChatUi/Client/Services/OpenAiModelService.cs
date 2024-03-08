using BlazorWasmChatUi.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BlazorWasmChatUi.Client.Services;

public class OpenAiModelService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiModelService> _logger;

    public OpenAiModelService(HttpClient httpClient,ILogger<OpenAiModelService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

	public async Task<IEnumerable<ChatContext>> GetTopicsAsync(CancellationToken cancellationToken)
	{
		var chatContextes = await _httpClient.GetFromJsonAsync<IEnumerable<ChatContext>>("topics", cancellationToken);

		if (chatContextes == null)
		{
			return Enumerable.Empty<ChatContext>();
		}

		return chatContextes.OrderByDescending(c => c.UpdatedDateTime);
    }

	public async Task SaveTopicAsync(ChatContext chatContext, CancellationToken cancellationToken)
	{
		var json = JsonSerializer.Serialize(chatContext);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		var response = await _httpClient.PostAsync("topics", content, cancellationToken).ConfigureAwait(false);

		if (response.IsSuccessStatusCode)
		{
			return;
		}

		throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).");
	}

    public async Task DeleteTopicAsync(ChatContext chatContext, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(chatContext);

        var request = new HttpRequestMessage
        {
			RequestUri = new Uri(_httpClient.BaseAddress, "topics"),
            Method = HttpMethod.Delete,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).");
    }

    public async Task<ChatMessage> PostAsync(ChatMessage[] messages, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(messages);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("openaimodel", content, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ChatMessage>();
            return result;
        }

        throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).");
    }

    public async Task PostStreamingAsync(ChatMessage[] messages, Action<ChatMessage, string?> onReadStream, int readingMillisecondsDelay, CancellationToken cancellationToken)
	{
        if (_httpClient.BaseAddress == null)
        {
            throw new InvalidOperationException("The BaseAddress of HttpClient is null.");
        }

		var json = JsonSerializer.Serialize(messages);

        HttpRequestMessage httpRequestMessage = new()
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
            Method = HttpMethod.Post,
            RequestUri = new Uri(_httpClient.BaseAddress, "openaimodel/streaming"),
        };

        httpRequestMessage.SetBrowserResponseStreamingEnabled(true);

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "An error occurred while sending the HTTP request.");
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).");
        }

        ChatMessage chatMessage = new()
        {
            Content = string.Empty,
        };

        await foreach (var jsonNode in
            ReadSseStreamingAsync(response, cancellationToken)
            .ConfigureAwait(false))
        {
            if (jsonNode == null)
            {
                continue;
            }

            chatMessage.Id = jsonNode["id"]?.GetValue<string>();
            chatMessage.Role = jsonNode["role"]?.GetValue<string>();            
            chatMessage.CreatedDateTime = jsonNode["createdDateTime"]?.GetValue<DateTimeOffset>();

            var chunkedContent = jsonNode["content"]?.GetValue<string>();
            chatMessage.Content += chunkedContent;

            if (chatMessage is not { Id: null, Role: null, CreatedDateTime: null })
            {
                onReadStream.Invoke(chatMessage, chunkedContent);
            }

            await Task.Delay(readingMillisecondsDelay).ConfigureAwait(false);
        }        
    }

    private async IAsyncEnumerable<JsonNode?> ReadSseStreamingAsync(
        HttpResponseMessage httpResponseMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Stream responseStream = await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        using (var streamReader = new StreamReader(responseStream))
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await streamReader.ReadLineAsync().ConfigureAwait(false);

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                else if (line == "data: [DONE]")
                {
                    break;
                }
                else if (line.StartsWith("data: "))
                {
                    var body = line.Substring(6, line.Length - 6);
                    yield return JsonSerializer.Deserialize<JsonNode>(body);
                }
            }
        };
    }
}
