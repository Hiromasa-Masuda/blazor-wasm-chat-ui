using BlazorWasmChatUi.Client.Services;
using Markdig;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace BlazorWasmChatUi.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddScoped<OpenAiModelService>();

        builder.Services.AddSingleton(sp => 
            new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build());


        await builder.Build().RunAsync();
    }
}