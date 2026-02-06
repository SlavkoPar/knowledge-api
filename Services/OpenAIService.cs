using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Knowledge.Services;

/// <summary>
/// Service to access Azure OpenAI.
/// </summary>
public class OpenAIService
{
    private readonly string _openAIEmbeddingDeployment = string.Empty;
    private readonly string _openAICompletionDeployment = string.Empty;
    private readonly int _openAIMaxTokens = default;

    private readonly AzureOpenAIClient? _openAIClient;

    //System prompts to send with user prompts to instruct the model for chat session
    private readonly string _systemPromptRecipeAssistant = @"
        You are an intelligent assistant for Contoso Recipes. 
        You are designed to provide helpful answers to user questions about using
        recipes, cooking instructions only using the provided JSON strings.

        Instructions:
        - In case a recipe is not provided in the prompt politely refuse to answer all queries regarding it. 
        - Never refer to a recipe not provided as input to you.
        - If you're unsure of an answer, you can say ""I don't know"" or ""I'm not sure"" and recommend users search themselves.        
        - Your response  should be complete. 
        - List the Name of the Recipe at the start of your response folowed by step by step cooking instructions
        - Assume the user is not an expert in cooking.
        - Format the content so that it can be printed to the Command Line 
        - In case there are more than one recipes you find let the user pick the most appropiate recipe.";

    public OpenAIService(IConfigurationSection config)
    {
        string endpoint = config["OpenAIEndpoint"];
        string key = config["OpenAIKey"];
        string embeddingDeployment = config["OpenAIEmbeddingDeployment"];
        string completionsDeployment = config["OpenAIcompletionsDeployment"];
        string maxToken = config["OpenAIMaxToken"];

        _openAIEmbeddingDeployment = embeddingDeployment;
        _openAICompletionDeployment = completionsDeployment;
        _openAIMaxTokens = int.TryParse(maxToken, out _openAIMaxTokens) ? _openAIMaxTokens : 8191;


        //OpenAIClientOptions clientOptions = new OpenAIClientOptions()
        //{
        //    Retry =
        //    {
        //        Delay = TimeSpan.FromSeconds(2),
        //        MaxRetries = 10,
        //        Mode = RetryMode.Exponential
        //    }
        //};

        try
        {

            //Use this as endpoint in configuration to use non-Azure Open AI endpoint and OpenAI model names
            //if (endpoint.Contains("api.openai.com"))
            //    _openAIClient = new OpenAIClient(key, clientOptions);
            //else
            //    _openAIClient = new(new Uri(endpoint), new AzureKeyCredential(key), clientOptions);


            // 2.0 - NEW: Get a chat completions client from a top-level Azure client
            //AzureOpenAIClient openAIClient = new(
            //    new Uri("https://your-resource.openai.azure.com/"),
            //    new AzureKeyCredential(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"));
            // ChatClient chatClient = openAIClient.GetChatClient("my-gpt-4o-mini-deployment");

            // 2.0: Microsoft Entra ID via Azure.Identity's DefaultAzureCredential
            _openAIClient = new(new Uri(endpoint!), new AzureKeyCredential(key!)); //, new DefaultAzureCredential());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OpenAIService Constructor failure: {ex.Message}");
        }
    }

    public async Task<float[]?> GetEmbeddingsAsync(dynamic data)
    {
        try
        {
            //EmbeddingsOptions embeddingsOptions = new()
            //{
            //    DeploymentName = _openAIEmbeddingDeployment,
            //    Input = { data },
            //};
            //var response = await _openAIClient.GetEmbeddingsAsync(embeddingsOptions);

            //Embeddings embeddings = response.Value;

            //float[] embedding = embeddings.Data[0].Embedding.ToArray();

            //return embedding;

            // 2.0 - NEW
            EmbeddingClient client = _openAIClient.GetEmbeddingClient(_openAIEmbeddingDeployment);

            //string description = "Best hotel in town if you like luxury hotels. They have an amazing infinity pool, a spa,"
            //    + " and a really helpful concierge. The location is perfect -- right downtown, close to all the tourist"
            //    + " attractions. We highly recommend this hotel.";

            OpenAIEmbedding embedding = client.GenerateEmbedding(data);
            ReadOnlyMemory<float> vector = embedding.ToFloats();
            return vector.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetEmbeddingsAsync Exception: {ex.Message}");
            return null;
        }
    }

    // Fix for CS0103: The name 'aoaiClient' does not exist in the current context
    // The variable 'aoaiClient' is not defined in the current context. Based on the context provided, it seems you intended to use '_openAIClient' instead.

    public async Task<(string response, int promptTokens, int responseTokens)>  GetChatCompletionAsync(string userPrompt, string documents)
    {
        try
        {
            // Use the existing '_openAIClient' instead of 'aoaiClient'
            ChatClient chatClient = _openAIClient.GetChatClient(_openAICompletionDeployment);

            ChatCompletion completion = chatClient.CompleteChat(
                new ChatMessage[]
                {
                    // System messages represent instructions or other guidance about how the assistant should behave
                    new SystemChatMessage("You are a helpful assistant that talks like a pirate."),
                    // User messages represent user input, whether historical or the most recent input
                    new UserChatMessage("Hi, can you help me?"),
                    // Assistant messages in a request represent conversation history for responses
                    new AssistantChatMessage("Arrr! Of course, me hearty! What can I do for ye?"),
                    new UserChatMessage("What's the best way to train a parrot?")
                });

            // Fix for CS1061: 'ChatCompletion' does not contain a definition for 'Choices'
            // Based on the provided type signature, use 'Content' and 'Usage' properties instead.
            return (
                response: completion.Content.ToString(),
                promptTokens: completion.Usage.InputTokenCount,
                responseTokens: completion.Usage.OutputTokenCount
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetChatCompletionAsync Exception: {ex.Message}");
            throw;
        }
    }
}