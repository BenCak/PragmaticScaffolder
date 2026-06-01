// Generates the NWS app from the real Northwind SQL Server database into /home/ben/code/nws
// Run with: dotnet run --project src/PragmaticScaffolder.TestGen
using PragmaticScaffolder.Core.Models;
using PragmaticScaffolder.Core.Services;

const string ConnStr =
    "Data Source=localhost;Initial Catalog=northwind;User ID=sa;Password=Sa123465!;" +
    "Pooling=False;Connect Timeout=30;Encrypt=False;" +
    "Trust Server Certificate=True;Authentication=SqlPassword;" +
    "Application Name=vscode-mssql;Application Intent=ReadWrite;Command Timeout=30";

var outputPath = Environment.GetEnvironmentVariable("SCAFFOLDER_TEST_OUTPUT") ?? "/home/ben/code/nws";

Console.WriteLine("Connecting to Northwind...");
var reader = new SqlServerSchemaReader();

var ok = await reader.TestConnectionAsync(ConnStr);
if (!ok) { Console.Error.WriteLine("Connection failed."); return 1; }

var db = await reader.ReadDatabaseAsync(ConnStr);
Console.WriteLine($"Connected: {db.DatabaseName} on {db.ServerVersion}");

var allTables = db.AllTables.ToList();
Console.WriteLine($"Found {allTables.Count} tables: {string.Join(", ", allTables.Select(t => t.Name))}");

if (Directory.Exists(outputPath))
    Directory.Delete(outputPath, recursive: true);
Directory.CreateDirectory(outputPath);

var request = new GenerationRequest
{
    RootNamespace    = "nws",
    OutputPath       = outputPath,
    Tables           = allTables,
    AllTables        = allTables,
    ConnectionString = ConnStr
};

Console.WriteLine("Generating...");
var engine = new GenerationEngine();
var result = engine.Generate(request);

if (result.Success)
{
    Console.WriteLine($"OK — {result.Files.Count} files written to {outputPath}");
}
else
{
    Console.WriteLine($"FAILED ({result.Errors.Count} errors):");
    foreach (var e in result.Errors)
        Console.WriteLine($"  ERROR: {e}");
    return 1;
}

return 0;
