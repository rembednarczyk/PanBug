using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PanBug.Domain.Scoring;
using PanBug.Web;
using PanBug.Web.Services;

// Aplikacja jest po polsku: separator dziesiętny "," i waluta zł.
var polish = new CultureInfo("pl-PL");
CultureInfo.DefaultThreadCurrentCulture = polish;
CultureInfo.DefaultThreadCurrentUICulture = polish;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<IScoringEngine>(new StandardScoringEngine());
builder.Services.AddSingleton<IResultStore, LocalStorageResultStore>();
builder.Services.AddSingleton<GameSession>();

await builder.Build().RunAsync();
