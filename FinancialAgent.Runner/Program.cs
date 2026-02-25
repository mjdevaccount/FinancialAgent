using FinancialAgent.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>()
    .Build();

var endpoint = config["AzureOpenAI:Endpoint"]!;
var key = config["AzureOpenAI:Key"]!;
var deployment = config["AzureOpenAI:ChatDeployment"]!;

// Build the kernel with Azure OpenAI and our tools
var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(deployment, endpoint, key)
    .Build();

var alphaVantageKey = config["AlphaVantage:Key"]!;
kernel.Plugins.AddFromObject(new FinancialTools(new HttpClient(), alphaVantageKey), "FinancialTools");

var chatService = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();

history.AddSystemMessage("""
                         You are a financial research assistant with access to real-time stock prices, 
                         return calculations, and news sentiment. When asked about stocks or investments, 
                         use your available tools to gather data before responding. Always cite the data 
                         you retrieved. Be concise and professional.
                         """);

var executionSettings = new AzureOpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

Console.WriteLine("Financial Agent ready. Type 'quit' to exit.\n");

while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();
    if (input?.ToLower() == "quit") break;

    history.AddUserMessage(input!);

    var response = await chatService.GetChatMessageContentAsync(history, executionSettings, kernel);
    history.AddAssistantMessage(response.Content!);

    Console.WriteLine($"\nAgent: {response.Content}\n");
}