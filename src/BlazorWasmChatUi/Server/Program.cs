using Azure.Core.Diagnostics;
using BlazorWasmChatUi.Server.DataGateway;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Diagnostics.Tracing;

namespace BlazorWasmChatUi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();

        builder.Services.AddSingleton(service =>
        {
            var configuration = service.GetRequiredService<IConfiguration>();
            var loggerFactory = service.GetRequiredService<ILoggerFactory>();

            var endpoint = configuration["azOpenAi:endpoint"] 
                ?? throw new InvalidOperationException("azOpenAi:endpoint not defined.");
            var key = configuration["azOpenAi:key"]
                ?? throw new InvalidOperationException("azOpenAi:key not defined.");
            var modelName = configuration["azOpenAi:modelName"]
                ?? throw new InvalidOperationException("azOpenAi:modelName not defined.");

            var kernalBuilder = Kernel.CreateBuilder();
            kernalBuilder.Services.AddSingleton(loggerFactory);
            kernalBuilder.AddAzureOpenAIChatCompletion(modelName, endpoint, key);

            return kernalBuilder.Build();            
        });

        builder.Services.AddScoped(service => service.GetRequiredService<Kernel>().Services.GetRequiredService<IChatCompletionService>());
        builder.Services.AddSingleton<ITopicsDataGateway, TopicsAzBlobStorageDataGateway>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.UseRouting();


        app.MapRazorPages();
        app.MapControllers();
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}