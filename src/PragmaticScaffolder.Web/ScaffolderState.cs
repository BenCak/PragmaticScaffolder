using PragmaticScaffolder.Core.Models;

namespace PragmaticScaffolder.Web;

/// <summary>
/// Holds wizard state across pages (connection → table selection → generate).
/// Scoped to the Blazor circuit, so one instance per browser session.
/// </summary>
public sealed class ScaffolderState
{
    public string ConnectionString { get; set; } = string.Empty;
    public DatabaseMetadata? Database { get; set; }
    public HashSet<string> SelectedTableKeys { get; set; } = [];  // "schema.table"
    public string RootNamespace { get; set; } = "MyApp";
    public string OutputPath { get; set; } = string.Empty;
    public string TablePrefix { get; set; } = string.Empty;
    public bool GenerateApiTests { get; set; } = true;
    public bool GenerateBlazorTests { get; set; } = true;

    public List<TableMetadata> SelectedTables =>
        Database?.AllTables
            .Where(t => SelectedTableKeys.Contains($"{t.Schema}.{t.Name}"))
            .ToList() ?? [];
}
