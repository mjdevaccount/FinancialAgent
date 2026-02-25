using FinancialAgent.Core;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Financial Agent API",
        Version = "v1",
        Description = "AI-powered financial research agent using Azure OpenAI and Semantic Kernel"
    });
});

var config = builder.Configuration;
var endpoint = config["AzureOpenAI:Endpoint"]!;
var key = config["AzureOpenAI:Key"]!;
var deployment = config["AzureOpenAI:ChatDeployment"]!;
var alphaVantageKey = config["AlphaVantage:Key"]!;

var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(deployment, endpoint, key)
    .Build();

kernel.Plugins.AddFromObject(new FinancialTools(new HttpClient(), alphaVantageKey), "FinancialTools");

builder.Services.AddSingleton(kernel);
builder.Services.AddSingleton(kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>());

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();