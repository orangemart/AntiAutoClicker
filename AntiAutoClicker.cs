using System;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("AntiAutoClicker", "Orangemart", "1.6.1")]
    [Description("Teleports or kicks players who AFK near select NPC vendors or vending machines with a nearby marker prefab.")]
    public class AntiAutoClicker : RustPlugin
    {
        private readonly Oxide.Core.Libraries.WebRequests webRequests = Interface.Oxide.GetLibrary<Oxide.Core.Libraries.WebRequests>();
        private Dictionary<ulong, float> playerAfkTimers = new Dictionary<ulong, float>();
        private List<Vector3> monitoredPositions = new List<Vector3>();

        private float afkThreshold;
        private float proximityRange;
        private float teleportDistance;
        private float checkInterval;
        private bool kickInsteadOfTeleport;
        private string discordWebhookUrl;
        private string targetItemPrefab;
        private bool useTargetItemFilter;
        private bool monitorNPCVendingMachines;
        private bool monitorNPCTraders;

        private const string KickPermission = "antiautoclicker.kickable";

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new default configuration file for AntiAutoClicker...");
            Config["AFKThresholdSeconds"] = 1800f;
            Config["ProximityRangeMeters"] = 4f;
            Config["TeleportDistanceMeters"] = 4f;
            Config["CheckIntervalSeconds"] = 300f;
            Config["KickInsteadOfTeleport"] = false;
            Config["DiscordWebhookURL"] = "";
            Config["TargetItemPrefab"] = "assets/prefabs/deployable/hazmatplushy/hazmatplushy_deployed.prefab";
            Config["UseTargetItemFilter"] = false;
            Config["MonitorNPCVendingMachines"] = true;
            Config["MonitorNPCTraders"] = true;
            SaveConfig();
        }

        private void Init()
        {
            LoadConfigValues();
            permission.RegisterPermission(KickPermission, this);
        }

        private void LoadConfigValues()
        {
            afkThreshold = GetConfig("AFKThresholdSeconds", 600f);
            proximityRange = GetConfig("ProximityRangeMeters", 4f);
            teleportDistance = GetConfig("TeleportDistanceMeters", 4f);
            checkInterval = GetConfig("CheckIntervalSeconds", 60f);
            kickInsteadOfTeleport = GetConfig("KickInsteadOfTeleport", false);
            discordWebhookUrl = GetConfig("DiscordWebhookURL", "");
            targetItemPrefab = GetConfig("TargetItemPrefab", "assets/prefabs/misc/xmas/sleigh/sleigh.prefab");
            useTargetItemFilter = GetConfig("UseTargetItemFilter", true);
            monitorNPCVendingMachines = GetConfig("MonitorNPCVendingMachines", true);
            monitorNPCTraders = GetConfig("MonitorNPCTraders", true);
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
            PrintWarning("AntiAutoClicker initialized. Finding monitored NPC positions...");
            FindMonitoredPositions();
            timer.Every(checkInterval, CheckAfkPlayers);
        }

        private void FindMonitoredPositions()
        {
            monitoredPositions.Clear();
            int matched = 0;

            foreach (var entity in BaseNetworkable.serverEntities)
            {
                bool isValidTarget = false;
                Vector3 entityPos = entity.transform.position;

                if (monitorNPCVendingMachines && entity is NPCVendingMachine vm && vm.OwnerID == 0)
                {
                    isValidTarget = true;
                }
                else if (monitorNPCTraders && entity.ShortPrefabName != null && entity.ShortPrefabName.Contains("shopkeeper_vm"))
                {
                    isValidTarget = true;
                }

                if (isValidTarget)
                {
                    if (useTargetItemFilter)
                    {
                        foreach (var nearby in BaseNetworkable.serverEntities)
                        {
                            if (nearby is BaseEntity marker && marker.PrefabName == targetItemPrefab)
                            {
                                if (Vector3.Distance(entityPos, marker.transform.position) <= 5f)
                                {
                                    monitoredPositions.Add(entityPos);
                                    matched++;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        monitoredPositions.Add(entityPos);
                        matched++;
                    }
                }
            }

            PrintWarning($"[AntiAutoClicker] Found {matched} monitored NPC positions.");
        }

      private void CheckAfkPlayers()
{
    int detectedCount = 0;

    foreach (var player in BasePlayer.activePlayerList)
    {
        if (player == null || !player.IsConnected || player.IsSleeping()) continue;

        bool isNear = IsNearMonitoredItem(player);
        if (isNear)
        {
            detectedCount++;
            PrintWarning($"[DEBUG] {player.displayName} is within range of a monitored area.");

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
                    playerAfkTimers[player.userID] = UnityEngine.Time.realtimeSinceStartup;
                }
            }
        }
        else
        {
            playerAfkTimers.Remove(player.userID);
        }
    }

    PrintWarning($"[DEBUG] AntiAutoClicker scan complete. Players near monitored areas: {detectedCount}");
}

        private bool IsNearMonitoredItem(BasePlayer player)
        {
            foreach (var pos in monitoredPositions)
            {
                if (Vector3.Distance(player.transform.position, pos) <= proximityRange)
                {
                    return true;
                }
            }
            return false;
        }

        private void TeleportPlayerBack(BasePlayer player)
        {
            Vector3 backwardPosition = player.transform.position - (player.eyes.BodyForward() * teleportDistance);
            backwardPosition.y = player.transform.position.y;
            player.Teleport(backwardPosition);
            player.ChatMessage("You've been moved for staying near a monitored area too long.");
            PrintWarning($"[INFO] Player {player.displayName} was teleported.");
            LogToDiscord($":truck: **{player.displayName}** was teleported for loitering near a monitored area.");
        }

        private void KickPlayer(BasePlayer player)
        {
            player.Kick("You were kicked for staying near a monitored area too long.");
            PrintWarning($"[INFO] Player {player.displayName} was kicked.");
            LogToDiscord($":boot: **{player.displayName}** was kicked for loitering near a monitored area.");
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
