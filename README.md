# Blazor WebAssembly Chat UI
This repository provides the code for a Chat UI developed using Blazor Webassembly. This project is a simple code that sends messages to Azure OpenAI Service and displays the generated messages on the UI.Please customize this code to your preferred UI based on this.


## Technologies Used
- Blazor Webassembly
- Azure OpenAI Service

## Installation 
1. Open BlazorWasmChatUi\Server\appsettings.json.
2. Edit the appsettings.json as shown in the following example.

```json
{
  "azOpenAi": {
    "endpoint": "<Azure OpenAI Endpoint>",
    "key": "<Azure OpenAI Key>",
    "modelName": "<Azure OpenAI Deployment Model Name>"
  },
  "azBlobStorage": {
    "connectionString": "<Azure Blob Storage Connection String>"
  }
}
```