using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebPush;

namespace Graduation_Project.Services
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PushNotificationService> _logger;
        private readonly string _vapidPublicKey;
        private readonly string _vapidPrivateKey;
        private readonly string _vapidSubject;

        public PushNotificationService(
            IServiceProvider serviceProvider,
            ILogger<PushNotificationService> logger,
            IWebHostEnvironment env,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _vapidSubject = configuration["Vapid:Subject"] ?? "mailto:admin@nabd.app";

            var configPublicKey = configuration["Vapid:PublicKey"];
            var configPrivateKey = configuration["Vapid:PrivateKey"];

            if (!string.IsNullOrWhiteSpace(configPublicKey) && !string.IsNullOrWhiteSpace(configPrivateKey))
            {
                _vapidPublicKey = configPublicKey;
                _vapidPrivateKey = configPrivateKey;
            }
            else
            {
                (_vapidPublicKey, _vapidPrivateKey) = LoadOrGenerateVapidKeys(env, logger);
            }
        }

        private static (string publicKey, string privateKey) LoadOrGenerateVapidKeys(
            IWebHostEnvironment env, ILogger logger)
        {
            var keyFile = Path.Combine(env.ContentRootPath, "vapid-keys.json");

            if (File.Exists(keyFile))
            {
                try
                {
                    var json = File.ReadAllText(keyFile);
                    var stored = JsonSerializer.Deserialize<VapidKeyStore>(json);
                    if (stored is { PublicKey: { Length: > 0 }, PrivateKey: { Length: > 0 } })
                        return (stored.PublicKey, stored.PrivateKey);
                }
                catch { }
            }

            var generated = VapidHelper.GenerateVapidKeys();
            try
            {
                File.WriteAllText(keyFile, JsonSerializer.Serialize(new VapidKeyStore
                {
                    PublicKey = generated.PublicKey,
                    PrivateKey = generated.PrivateKey
                }));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not persist VAPID keys to {File}. Keys are in-memory only this session.", keyFile);
            }

            logger.LogInformation(
                "Generated VAPID keys. To persist, add to appsettings.json: Vapid:PublicKey={Pub}",
                generated.PublicKey);

            return (generated.PublicKey, generated.PrivateKey);
        }

        public string GetVapidPublicKey() => _vapidPublicKey;

        public Task SendToUserAsync(string userId, string title, string body, string? url = null)
            => SendToUsersAsync([userId], title, body, url);

        public async Task SendToUsersAsync(IEnumerable<string> userIds, string title, string body, string? url = null)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var userIdList = userIds.ToList();
            var subscriptions = await db.UserPushSubscriptions
                .Where(s => userIdList.Contains(s.UserId))
                .ToListAsync();

            if (subscriptions.Count == 0) return;

            var payload = JsonSerializer.Serialize(new
            {
                title,
                body,
                url = url ?? "/",
                icon = "/images/logo.png",
                badge = "/images/logo.png"
            });

            var webPushClient = new WebPushClient();
            var vapidDetails = new VapidDetails(_vapidSubject, _vapidPublicKey, _vapidPrivateKey);
            var staleIds = new List<int>();

            foreach (var sub in subscriptions)
            {
                try
                {
                    var pushSub = new PushSubscription(sub.Endpoint, sub.P256DH, sub.Auth);
                    await webPushClient.SendNotificationAsync(pushSub, payload, vapidDetails);
                }
                catch (WebPushException ex) when (
                    ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                    ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    staleIds.Add(sub.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Push notification failed for subscription {Id}", sub.Id);
                }
            }

            if (staleIds.Count > 0)
            {
                db.UserPushSubscriptions.RemoveRange(
                    db.UserPushSubscriptions.Where(s => staleIds.Contains(s.Id)));
                await db.SaveChangesAsync();
            }
        }

        private sealed class VapidKeyStore
        {
            public string PublicKey { get; set; } = "";
            public string PrivateKey { get; set; } = "";
        }
    }
}
