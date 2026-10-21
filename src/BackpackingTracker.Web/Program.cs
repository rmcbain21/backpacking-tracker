using BackpackingTracker.Core;
using BackpackingTracker.Core.Ports;
using BackpackingTracker.Web;
using BackpackingTracker.Web.Adapters;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

builder.Services.AddSingleton<IRecordStore, DexieRecordStore>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<AppCore>();

await builder.Build().RunAsync();
