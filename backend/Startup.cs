using System;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Pinecone;
using ChatBot.Services;
using Google.GenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace ChatBot;

static class Startup
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        var openAiKey = builder.RequireEnv("OPENAI_API_KEY");
        var geminiAiKey = builder.RequireEnv("GEMINI_API_KEY");
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

        // builder.Services.AddSingleton<StringEmbeddingGenerator>(s => new OpenAI.Embeddings.EmbeddingClient(
        //         model: "text-embedding-3-small",
        //         apiKey: openAiKey
        //     ).AsIEmbeddingGenerator());
        //
        builder.Services.AddGoogleAIEmbeddingGenerator(
            modelId: "gemini-embedding-001",       // Name of the embedding model, e.g. "models/text-embedding-004".
            apiKey: geminiAiKey
        );

        builder.Services.AddTransient(serviceProvider=> {
            return new Kernel(serviceProvider);
        });
        
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
                  "gpt-5-mini",
                  openAiKey).AsIChatClient();

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