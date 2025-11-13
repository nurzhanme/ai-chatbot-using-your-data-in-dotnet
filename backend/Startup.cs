using System;
using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Pinecone;
using ChatBot.Services;
using OpenAI;

namespace ChatBot;

static class Startup
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        var openAiKey = builder.RequireEnv("OPENAI_API_KEY");
        var pineconeKey = builder.RequireEnv("PINECONE_API_KEY");

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("FrontendCors", policy =>
                policy
                    .WithOrigins("http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
            );
        });

        builder.Services.AddSingleton<StringEmbeddingGenerator>(s => new OpenAI.Embeddings.EmbeddingClient(
                model: "gemini-embedding-001",
                credential: new ApiKeyCredential(openAiKey),
                options: new OpenAIClientOptions
                {
                    Endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/")
                }
            ).AsIEmbeddingGenerator());

        builder.Services.AddSingleton<IndexClient>(s => new PineconeClient(pineconeKey).Index("tours-chunks"));

        builder.Services.AddSingleton<DocumentChunkStore>(s => new DocumentChunkStore());

        builder.Services.AddSingleton<VectorSearchService>();

        builder.Services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information));

        builder.Services.AddSingleton<ILoggerFactory>(sp =>
            LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information)));

        builder.Services.AddSingleton<IChatClient>(sp =>
         {
             var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
             var client = new OpenAI.Chat.ChatClient(
                  "gemini-2.5-flash",
                  credential: new ApiKeyCredential(openAiKey),
                  options: new OpenAIClientOptions
                  {
                      Endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/")
                  }).AsIChatClient();

             return new ChatClientBuilder(client)
                 .UseLogging(loggerFactory)
                 .UseFunctionInvocation(loggerFactory, c =>
                 {
                     c.IncludeDetailedErrors = true;
                 })
                 .Build(sp);
         });

        builder.Services.AddTransient<ChatOptions>(sp => new ChatOptions
        {
            Tools = FunctionRegistry.GetTools(sp).ToList(),
        });

        builder.Services.AddSingleton<WikipediaClient>();
        builder.Services.AddSingleton<IndexBuilder>();
        builder.Services.AddSingleton<RagQuestionService>();
        builder.Services.AddSingleton<ArticleSplitter>();
        builder.Services.AddSingleton<PromptService>();
    }
}
