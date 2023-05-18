// See https://aka.ms/new-console-template for more information

using System.Reflection;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;
using Open.Autom.AI.tion.Console;

Console.WriteLine("Hi 👋🏽");
Console.WriteLine("I am a OpenAI based Virtual Assistant.");
Console.WriteLine("Ask me anything and I will do my best to answer!");

var configurationRoot = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Development.json")
    .Build();

var options = new OpenAiOptions(string.Empty, string.Empty, string.Empty);
configurationRoot.GetSection("OpenAI").Bind(options);

var context = """
//You are a virtual assistant for a user.
//The user will ask you a question that you have to answer.
//The question stands directly after the code block.
//You must not return the answer directly in form of text.
//Instead you are supposed to return the body of a function with the following signature "Task<string> answer(IMicrosoftInterface ms);".
//The return value of this function has to be the answer you intend to give to the user.
//The working hours are from 8am to 5pm.
""";

var code = File.ReadAllText("IMicrosoftInterface.cs");

var openAiClient = new OpenAIClient(new Uri(options.Endpoint), new AzureKeyCredential(options.Key));

while (true)
{
    Console.Write(" > ");
    var question = Console.ReadLine();

    if (question == null)
    {
        Console.WriteLine(" 🧐 I believe there was no input?! ");
        continue;
    }

    Console.WriteLine(" 🤔 Let me try to come up with an answer!");

    var response = openAiClient.GetCompletions(options.ModelName, new CompletionsOptions
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
    var generatedCode = $"public static async Task<string> Answer(IMicrosoftInterface ms) {{{choice}";
    var assemblyCode = FormatAssembly(generatedCode);
    Console.WriteLine(assemblyCode);

    var assembly = Compile(assemblyCode);

    if (assembly == null) continue;

    var answerMethodInfo = assembly
        .GetType("OpenGenerated.OpenGeneratedClass")!
        .GetMethod("Answer", BindingFlags.Public | BindingFlags.Static)!;

    var answerObject = answerMethodInfo.Invoke(null, new object?[] { new MicrosoftClient() });
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

IEnumerable<MetadataReference> GetGlobalReferences()
{
    var returnList = new List<MetadataReference>();

    var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

    returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")));
    returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.dll")));
    returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Core.dll")));
    returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Linq.dll")));
    returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Collections.dll")));
    returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")));
    returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Private.CoreLib.dll")));

    returnList.Add(MetadataReference.CreateFromFile(typeof(IMicrosoftInterface).Assembly.Location));

    return returnList;
}

Assembly? Compile(string code)
{
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var assemblyName = Path.GetRandomFileName();

    MetadataReference[] references =
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
        MetadataReference.CreateFromFile(Assembly.GetCallingAssembly().Location),
    };

    var compilation = CSharpCompilation.Create(
        assemblyName,
        syntaxTrees: new[] { syntaxTree },
        references: GetGlobalReferences(),
        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    using var ms = new MemoryStream();
    var emitResult = compilation.Emit(ms);

    if (!emitResult.Success)
    {
        var failures = emitResult.Diagnostics.Where(diagnostic =>
            diagnostic.IsWarningAsError ||
            diagnostic.Severity == DiagnosticSeverity.Error);

        foreach (var diagnostic in failures)
        {
            Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
        }

        return null;
    }

    ms.Seek(0, SeekOrigin.Begin);

    return Assembly.Load(ms.ToArray());
}

record OpenAiOptions(string Endpoint, string Key, string ModelName);