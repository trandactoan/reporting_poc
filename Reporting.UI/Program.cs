using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Reporting.UI;
using Reporting.UI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Default HttpClient (used by Blazor internals)
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Named HttpClient → Reporting.Api
var apiBase = builder.Configuration["ReportingApi:BaseUrl"]
              ?? "http://localhost:5100/";

builder.Services.AddHttpClient<ReportApiService>(client =>
    client.BaseAddress = new Uri(apiBase));

await builder.Build().RunAsync();
