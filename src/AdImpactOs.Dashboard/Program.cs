var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("PanelistApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiEndpoints:PanelistApi"] ?? "http://localhost:5001");
});
builder.Services.AddHttpClient("SurveyApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiEndpoints:SurveyApi"] ?? "http://localhost:5002");
});
builder.Services.AddHttpClient("CampaignApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiEndpoints:CampaignApi"] ?? "http://localhost:5003");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
