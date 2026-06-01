using PragmaticScaffolder.Core.Models;
using PragmaticScaffolder.Core.Templates;

namespace PragmaticScaffolder.Core.Services.Generators;

/// <summary>
/// Generates a typed HttpClient wrapper per feature in the Blazor project.
/// Blazor pages call the API through these clients — never touching DbContext directly.
/// </summary>
public sealed class BlazorApiClientGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationRequest request)
    {
        var allTableLookup = request.AllTables
            .ToDictionary(t => $"{t.Schema}.{t.Name}", StringComparer.OrdinalIgnoreCase);

        foreach (var table in request.Tables)
        {
            var className     = NamingHelper.ToClassName(table.Name);
            var featureFolder = NamingHelper.ToCollectionName(table.Name);
            var pkColumns     = table.PrimaryKeyColumns.ToList();
            var fkDisplays    = DtoGenerator.BuildFkDisplays(table, allTableLookup);

            var model = new
            {
                Namespace       = $"{request.RootNamespace}.Blazor.Features.{featureFolder}",
                SharedNamespace = $"{request.RootNamespace}.Shared",
                ClassName       = className,
                FeatureFolder   = featureFolder,
                RoutePrefix     = featureFolder.ToLowerInvariant(),
                HasFkDisplay    = fkDisplays.Count > 0,
                HasSinglePk     = pkColumns.Count == 1,
                PkColumns       = pkColumns.Select(c => new
                {
                    c.Name,
                    PropertyName    = NamingHelper.ToPropertyName(c.Name),
                    c.ClrType,
                    ParamName       = NamingHelper.ToParamName(NamingHelper.ToPropertyName(c.Name)),
                    RouteConstraint = GetRouteConstraint(c.ClrType)
                }).ToList()
            };

            yield return new GeneratedFile
            {
                RelativePath = $"src/{request.RootNamespace}.Blazor/Features/{featureFolder}/{featureFolder}ApiClient.cs",
                Content      = TemplateLoader.Render("BlazorApiClient", model)
            };
        }
    }

    private static string GetRouteConstraint(string clrType) => clrType.TrimEnd('?') switch
    {
        "int"  => ":int",
        "long" => ":long",
        "Guid" => ":guid",
        _      => string.Empty
    };
}
