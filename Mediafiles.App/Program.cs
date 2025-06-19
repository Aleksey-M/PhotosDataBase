using LiteDB;
using Mediafiles.App.Components;
using Mediafiles.Application.Repositories;
using Mediafiles.Infrastructure;
using Mediafiles.Infrastructure.Repositories;
using Mediafiles.Infrastructure.Settings;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

//builder.AddServiceDefaults();

var liteDbSettings = new LiteDbSettings();
builder.Configuration.GetSection(nameof(LiteDbSettings)).Bind(liteDbSettings);
await LiteDbInitializer.initializeDb(liteDbSettings.ConnectionUri);
builder.Services.AddSingleton(_ => new LiteDatabase(liteDbSettings.ConnectionUri));

//builder.Services.AddSingleton(c => new LiteDbService("database.ldb"));
//builder.Services.AddSingleton<ImagesReader>();

builder.Services.AddScoped<IMediaCollectionRepository, MediaCollectionRepository>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

var app = builder.Build();

app.MapDefaultEndpoints();

//app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
