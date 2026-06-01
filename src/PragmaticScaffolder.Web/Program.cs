using MudBlazor.Services;
using PragmaticScaffolder.Core.Services;
using PragmaticScaffolder.Web;
using PragmaticScaffolder.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();

// Core scaffolder services
builder.Services.AddScoped<SqlServerSchemaReader>();
builder.Services.AddScoped<GenerationEngine>();

// Session-scoped state shared across wizard pages
builder.Services.AddScoped<ScaffolderState>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
