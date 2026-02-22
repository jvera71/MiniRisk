using MiniRisk.Components;

namespace MiniRisk
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // MiniRisk Services
            builder.Services.AddSingleton<MiniRisk.Services.Interfaces.IGameManager, MiniRisk.Services.GameManager>();
            builder.Services.AddSingleton<MiniRisk.Services.Interfaces.IMapService, MiniRisk.Services.MapService>();
            
            builder.Services.AddScoped<MiniRisk.Services.Interfaces.IPlayerSessionService, MiniRisk.Services.PlayerSessionService>();
            
            builder.Services.AddTransient<MiniRisk.Services.Interfaces.IGameEngine, MiniRisk.Services.GameEngine>();
            builder.Services.AddTransient<MiniRisk.Services.Interfaces.IDiceService, MiniRisk.Services.DiceService>();
            builder.Services.AddTransient<MiniRisk.Services.Interfaces.ICardService, MiniRisk.Services.CardService>();
            builder.Services.AddSignalR(options =>
            {
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                options.MaximumReceiveMessageSize = 64 * 1024; // 64 KB
                options.EnableDetailedErrors = builder.Environment.IsDevelopment();
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();
            app.MapHub<MiniRisk.Hubs.GameHub>("/gamehub");

            app.Run();
        }
    }
}
