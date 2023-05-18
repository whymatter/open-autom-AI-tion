// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Text;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Open.Autom.AI.tion.Console;

Console.OutputEncoding = Encoding.Unicode;

Console.WriteLine(" 👋 Hi there,");
Console.WriteLine("  I am an Lilly, your OpenAI based virtual assistant.");
Console.WriteLine("  You can ask me anything and I will do my best to answer!");

var configurationRoot = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Development.json")
    .Build();

var openAiOptions = new OpenAiOptions(string.Empty, string.Empty, string.Empty);
configurationRoot.GetSection("OpenAI").Bind(openAiOptions);

var msOptions = new MicrosoftOptions(string.Empty, string.Empty);
configurationRoot.GetSection("Microsoft").Bind(msOptions);

const string context = """
//You are a virtual assistant for a user.
//The user will ask you a question that you have to answer.
//The question stands directly after the code block.
//You must not return the answer directly in form of text.
//Instead you are supposed to return the body of a function with the following signature "Task<string> answer(IMicrosoftInterface ms);".
//The return value of this function has to be the answer you intend to give to the user.
//The working hours are from 8am to 5pm.
""";

var code = File.ReadAllText("IMicrosoftInterface.cs");

// Initialize OpenAI
var openAiClient = new OpenAIClient(new Uri(openAiOptions.Endpoint), new AzureKeyCredential(openAiOptions.Key));

// Initialize Microsoft
Console.WriteLine("");
Console.WriteLine(" For the start, I need you to grant me access to your account.");
Console.WriteLine(" 🤔 Are you okay with that?");

var grantChecker = new OpenAiGrantChecker(openAiClient, openAiOptions);
var interactiveGrantChecker = new InteractiveGrantChecker(grantChecker);

await interactiveGrantChecker.GetAsync("Are you okay with that?", require: true);

Console.WriteLine(" 🔐 Great, thanks, starting authentication");
var graphServiceClient = await GraphServiceClientFactory.Get(msOptions);
var microsoftClient = new MicrosoftGraphClient(graphServiceClient);
Console.WriteLine(" ✅ Authentication successful");
Console.WriteLine("");

await new CalendarExporter(graphServiceClient).ExportAsync();

var compiler = new Compiler();

Console.WriteLine("Now you can ask me for help!");

while (true)
{
    Console.Write(" > ");
    var question = Console.ReadLine();

    if (question == null)
    {
        Console.WriteLine(" 🧐 I believe there was no input?! ");
        continue;
    }

    Console.WriteLine(" 🤔 Thinking...");

    var response = openAiClient.GetCompletions(openAiOptions.ModelName, new CompletionsOptions
    {
        Temperature = 0,
        ChoicesPerPrompt = 1,
        MaxTokens = 1000,
        Echo = false,
        Prompts =
        {
            FormatPrompt(context, code, question)
        },
        StopSequences = { "//", "/*" }
    });

    if (!response.HasValue || !response.Value.Choices.Any())
    {
        Console.WriteLine(" ☹️ Sorry there went something wrong ...");
        continue;
    }

    var choice = response.Value.Choices[0].Text.Trim();
    var generatedCode = $"public static async Task<string> Answer(IMicrosoftInterface ms) {{\n{choice}";
    var assemblyCode = FormatAssembly(generatedCode);

    var foregroundColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("");
    Console.WriteLine($"    {string.Join("\n    ", assemblyCode.Split("\n"))}");
    Console.WriteLine("");
    Console.ForegroundColor = foregroundColor;

    var assembly = compiler.Compile(assemblyCode);

    if (assembly == null) continue;

    var answerMethodInfo = assembly
        .GetType("OpenGenerated.OpenGeneratedClass")!
        .GetMethod("Answer", BindingFlags.Public | BindingFlags.Static)!;

    var answerObject = answerMethodInfo.Invoke(null, new object?[] { microsoftClient });
    if (answerObject is Task<string> answerStringTask)
    {
        var answerString = await answerStringTask;
        Console.WriteLine($" 😌 {answerString}");
    }
}

string FormatPrompt(string context, string code, string question)
{
    return $"{context}\n{code}\n//{question}\nasync Task<string> Answer(IMicrosoftInterface ms) {{";
}

string FormatAssembly(string answerCode)
{
    return $$"""
using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Open.Autom.AI.tion.Console;

namespace OpenGenerated;

public static class OpenGeneratedClass {
{{answerCode}}
}
""";
}