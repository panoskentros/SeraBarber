using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SeraBarber;
using Radzen;
using MudBlazor.Services;
using SeraBarber.Services;
using Supabase;
using Supabase.Postgrest;
using System.Globalization;
using System.Threading;

// Set the default cultures for the application
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("el-GR");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("el-GR");

// Set the current thread's culture
CultureInfo.CurrentCulture = new CultureInfo("el-GR");
CultureInfo.CurrentUICulture = new CultureInfo("el-GR");
Thread.CurrentThread.CurrentCulture = new CultureInfo("el-GR");
Thread.CurrentThread.CurrentUICulture = new CultureInfo("el-GR");


var builder = WebAssemblyHostBuilder.CreateDefault(args );

builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

// Register SupabaseService
builder.Services.AddScoped<SupabaseService>();

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();