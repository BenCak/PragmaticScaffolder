using PragmaticScaffolder.Core.Models;
using PragmaticScaffolder.Core.Templates;

namespace PragmaticScaffolder.Core.Services.Generators;

/// <summary>Generates AppDbContext in the Data project.</summary>
public sealed class DbContextGenerator
{
    public IEnumerable<GeneratedFile> Generate(GenerationRequest request)
    {
        var model = new
        {
            Namespace  = $"{request.RootNamespace}.Data",
            Entities   = request.Tables.Select(t => new
            {
                ClassName = NamingHelper.ToClassName(t.Name),
                SetName   = NamingHelper.ToCollectionName(t.Name),
                t.Schema,
                t.Name
            }).ToList()
        };

        yield return new GeneratedFile
        {
            RelativePath = $"src/{request.RootNamespace}.Data/AppDbContext.cs",
            Content      = TemplateLoader.Render("DbContext", model)
        };
    }
}
