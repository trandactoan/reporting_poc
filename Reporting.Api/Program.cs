using Reporting.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "Reporting API", Version = "v1" }));

// CORS — allow the Blazor WASM dev server and any localhost origin
builder.Services.AddCors(options =>
    options.AddPolicy("BlazorClient", policy =>
        policy.WithOrigins(
                  "https://localhost:7219",
                  "https://localhost:7299",
                  "http://localhost:5299",
                  "http://localhost:5000",
                  "https://localhost:5001")
              .AllowAnyMethod()
              .AllowAnyHeader()));

// ── Report services ────────────────────────────────────────────────────────────
//
// Stub — works without a Telerik license (active by default for this POC).
builder.Services.AddScoped<IReportGeneratorService, StubReportGeneratorService>();
//
// Full Telerik implementation — uncomment after adding Telerik NuGet feed:
//   <PackageReference Include="Telerik.Reporting" Version="18.*" />
// builder.Services.AddScoped<IReportGeneratorService, TelerikReportGeneratorService>();

builder.Services.AddSingleton<IReportStorageService, MongoReportStorageService>();

// ── App ────────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("BlazorClient");
app.UseAuthorization();
app.MapControllers();

app.Run();
