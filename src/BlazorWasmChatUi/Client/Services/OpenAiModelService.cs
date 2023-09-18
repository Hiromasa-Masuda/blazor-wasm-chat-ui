using System.Text.Json;
using System.Text;
using BlazorWasmChatUi.Shared;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Net.Http;

namespace BlazorWasmChatUi.Client.Services;

public class OpenAiModelService
{
    private readonly HttpClient _httpClient;

    public OpenAiModelService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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

    public async Task<ChatMessage> PostAsync(ChatMessage[] messages)
	{
		var json = JsonSerializer.Serialize(messages);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		var response = await _httpClient.PostAsync("openaimodel", content).ConfigureAwait(false);

		if (response.IsSuccessStatusCode)
		{
			var result = await response.Content.ReadFromJsonAsync<ChatMessage>();
			return result;
		}

		throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).");
	}

}
