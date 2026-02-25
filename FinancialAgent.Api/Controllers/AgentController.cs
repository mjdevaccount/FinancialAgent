using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace FinancialAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController(Kernel kernel, IChatCompletionService chatService) : ControllerBase
{
    [HttpPost("ask")]
    public async Task<ActionResult<AgentResponse>> Ask([FromBody] AgentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question is required.");

        var history = new ChatHistory();
        history.AddSystemMessage("""
                                 You are a financial research assistant with access to real-time stock prices, 
                                 news sentiment, fundamentals, and earnings data. When asked about stocks, 
                                 use your available tools to gather data before responding. 
                                 Always cite the data you retrieved. Be concise and professional.
                                 """);

        history.AddUserMessage(request.Question);

        var executionSettings = new AzureOpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var response = await chatService.GetChatMessageContentAsync(history, executionSettings, kernel);

        return Ok(new AgentResponse { Answer = response.Content! });
    }
}

public class AgentRequest
{
    public string Question { get; set; } = string.Empty;
}

public class AgentResponse
{
    public string Answer { get; set; } = string.Empty;
}