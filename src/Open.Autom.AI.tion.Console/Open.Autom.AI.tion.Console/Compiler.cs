using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Open.Autom.AI.tion.Console;

public class Compiler
{
    public Assembly? Compile(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var assemblyName = Path.GetRandomFileName();

        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: new[] { syntaxTree },
            references: GetGlobalAssemblies().Concat(GetCustomAssemblies()),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var emitResult = compilation.Emit(ms);

        if (!emitResult.Success)
        {
            var failures = emitResult.Diagnostics.Where(
                diagnostic => diagnostic.IsWarningAsError ||
                              diagnostic.Severity == DiagnosticSeverity.Error
            );

            foreach (var diagnostic in failures)
            {
                System.Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
            }

            return null;
        }

        ms.Seek(0, SeekOrigin.Begin);

        return Assembly.Load(ms.ToArray());
    }

    private static IEnumerable<MetadataReference> GetGlobalAssemblies()
    {
        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        var systemAssemblies = new[]
        {
            "mscorlib.dll",
            "System.dll",
            "System.Core.dll",
            "System.Linq.dll",
            "System.Runtime.dll",
            "System.Collections.dll",
            "System.Private.CoreLib.dll"
        };

        return systemAssemblies.Select(
            assemblyName => MetadataReference.CreateFromFile(Path.Combine(assemblyPath, assemblyName))
        );
    }

    private static IEnumerable<MetadataReference> GetCustomAssemblies()
    {
        return new[]
        {
            MetadataReference.CreateFromFile(typeof(IMicrosoftInterface).Assembly.Location)
        };
    }
}