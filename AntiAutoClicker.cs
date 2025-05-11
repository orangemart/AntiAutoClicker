using System;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("AntiAutoClicker", "Orangemart", "1.3.0")]
    [Description("Teleports or kicks players who stay near shopkeepers or drone marketplaces too long, with configurable settings.")]
    public class AntiAutoClicker : RustPlugin
    {
        private readonly Oxide.Core.Libraries.WebRequests webRequests = Interface.Oxide.GetLibrary<Oxide.Core.Libraries.WebRequests>();
        private Dictionary<ulong, float> playerAfkTimers = new Dictionary<ulong, float>();
        private List<Vector3> shopkeeperPositions = new List<Vector3>();

        private float afkThreshold;
        private float shopkeeperProximity;
        private float teleportDistance;
        private float checkInterval;
        private bool kickInsteadOfTeleport;
        private string discordWebhookUrl;
        private bool checkDroneMarketplaces;

        private const string KickPermission = "antiautoclicker.kickable";

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new default configuration file for AntiAutoClicker...");
            Config["AFKThresholdSeconds"] = 180f;
            Config["ShopkeeperProximityMeters"] = 3f;
            Config["TeleportDistanceMeters"] = 3f;
            Config["CheckIntervalSeconds"] = 15f;
            Config["KickInsteadOfTeleport"] = false;
            Config["DiscordWebhookURL"] = "";
            Config["CheckDroneMarketplaces"] = false; // New config option
            SaveConfig();
        }

        private void Init()
        {
            LoadConfigValues();
            permission.RegisterPermission(KickPermission, this);
        }

        private void LoadConfigValues()
        {
            afkThreshold = GetConfig("AFKThresholdSeconds", 180f);
            shopkeeperProximity = GetConfig("ShopkeeperProximityMeters", 3f);
            teleportDistance = GetConfig("TeleportDistanceMeters", 3f);
            checkInterval = GetConfig("CheckIntervalSeconds", 15f);
            kickInsteadOfTeleport = GetConfig("KickInsteadOfTeleport", false);
            discordWebhookUrl = GetConfig("DiscordWebhookURL", "");
            checkDroneMarketplaces = GetConfig("CheckDroneMarketplaces", false);
        }

        private T GetConfig<T>(string key, T defaultValue)
        {
            if (Config[key] == null)
            {
                Config[key] = defaultValue;
                SaveConfig();
            }

            try
            {
                return (T)Convert.ChangeType(Config[key], typeof(T));
            }
            catch
            {
                PrintWarning($"[WARNING] Invalid config value for {key}. Using default: {defaultValue}");
                return defaultValue;
            }
        }

        private void OnServerInitialized()
        {
            PrintWarning("AntiAutoClicker initialized. Finding monitored locations (shopkeepers/marketplaces)...");

            FindShopkeepers();
            timer.Every(checkInterval, CheckAfkPlayers);
        }

        private void FindShopkeepers()
        {
            shopkeeperPositions.Clear();
            int npcVendorCount = 0;
            int droneMarketplaceCount = 0;

            foreach (var entity in BaseNetworkable.serverEntities)
            {
                // Debugging: Log all Marketplace entities found to see their details
                if (entity is Marketplace marketplaceInstance)
                {
                    PrintWarning($"[DEBUG AAC] Iterating: Found a Marketplace entity. Type: {entity.GetType().Name}, ShortPrefabName: '{marketplaceInstance.ShortPrefabName ?? "null"}', PrefabName: '{marketplaceInstance.PrefabName ?? "null"}', Position: {marketplaceInstance.transform.position}");
                }

                if (entity is NPCVendingMachine vendingMachine && vendingMachine.ShortPrefabName.Contains("shopkeeper_vm_invis"))
                {
                    shopkeeperPositions.Add(vendingMachine.transform.position);
                    npcVendorCount++;
                }
                // Check for Drone Marketplaces if the config option is enabled
                else if (checkDroneMarketplaces && entity is Marketplace marketplaceTerminal && marketplaceTerminal.ShortPrefabName == "marketplace")
                {
                    // Explicitly log when we are about to add a drone marketplace
                    PrintWarning($"[DEBUG AAC] Adding Marketplace to monitored locations: ShortPrefabName='{marketplaceTerminal.ShortPrefabName}', Position={marketplaceTerminal.transform.position}");
                    shopkeeperPositions.Add(marketplaceTerminal.transform.position);
                    droneMarketplaceCount++;
                }
            }

            string foundMessage = $"[INFO] Found {npcVendorCount} NPC shopkeepers.";
            if (checkDroneMarketplaces)
            {
                foundMessage += $" Found {droneMarketplaceCount} drone marketplaces.";
            }
            foundMessage += $" Total monitored locations: {shopkeeperPositions.Count}.";
            PrintWarning(foundMessage);
        }

        private void CheckAfkPlayers()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player == null || !player.IsConnected || player.IsSleeping()) continue;

                bool nearShopkeeper = IsNearShopkeeper(player);
                if (!nearShopkeeper)
                {
                    if (playerAfkTimers.ContainsKey(player.userID))
                        playerAfkTimers.Remove(player.userID);
                    continue;
                }

                if (!playerAfkTimers.ContainsKey(player.userID))
                {
                    playerAfkTimers[player.userID] = UnityEngine.Time.realtimeSinceStartup;
                }
                else
                {
                    float afkTime = UnityEngine.Time.realtimeSinceStartup - playerAfkTimers[player.userID];
                    if (afkTime >= afkThreshold)
                    {
                        if (kickInsteadOfTeleport && permission.UserHasPermission(player.UserIDString, KickPermission))
                        {
                            KickPlayer(player);
                        }
                        else
                        {
                            TeleportPlayerBack(player);
                        }
                        playerAfkTimers[player.userID] = UnityEngine.Time.realtimeSinceStartup; // Reset timer after action
                    }
                }
            }
        }

        private bool IsNearShopkeeper(BasePlayer player)
        {
            foreach (var shopkeeperPos in shopkeeperPositions)
            {
                if (Vector3.Distance(player.transform.position, shopkeeperPos) <= shopkeeperProximity)
                {
                    return true;
                }
            }
            return false;
        }

        private void TeleportPlayerBack(BasePlayer player)
        {
            Vector3 backwardPosition = player.transform.position - (player.eyes.BodyForward() * teleportDistance);
            backwardPosition.y = player.transform.position.y; // Maintain height
            player.Teleport(backwardPosition);
            player.ChatMessage("You've been moved for staying near a vending area too long.");
            PrintWarning($"[INFO] Player {player.displayName} was teleported for staying near a vending area too long.");
            LogToDiscord($":truck: **{player.displayName}** was teleported for loitering near a vending area.");
        }

        private void KickPlayer(BasePlayer player)
        {
            player.Kick("You were kicked for staying near a vending area too long.");
            PrintWarning($"[INFO] Player {player.displayName} was kicked for staying near a vending area too long.");
            LogToDiscord($":boot: **{player.displayName}** was kicked for loitering near a vending area.");
        }

        private void LogToDiscord(string message)
        {
            if (string.IsNullOrEmpty(discordWebhookUrl)) return;

            var json = $"{{\"content\":\"{message.Replace("\"", "\\\"")}\"}}";

            webRequests.Enqueue(
                discordWebhookUrl,
                json,
                (code, response) =>
                {
                    if (code != 204 && code != 200)
                    {
                        PrintWarning($"[Discord] Webhook failed with code {code}: {response}");
                    }
                },
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                new Dictionary<string, string> { ["Content-Type"] = "application/json" }
            );
        }
    }
}
