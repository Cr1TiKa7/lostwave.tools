using BlazorDownloadFile;
using Lostwave.Tools.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:80");
builder.Services.AddSingleton<DiscordService>();
builder.Services.AddHostedService<BackgroundJobService>();

builder.Services.AddTransient<MySpaceCrawlingService>();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddHttpClient();
builder.Services.AddBlazorDownloadFile();
builder.Services.AddMudServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
