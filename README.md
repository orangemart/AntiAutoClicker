# AntiAutoClicker - Rust Plugin

## 📌 Overview
**AntiAutoClicker** is a Rust Oxide plugin that detects and punishes players who stay near NPC shopkeepers or Drone Marketplaces for too long, typically to abuse autoclickers. Players can either be **teleported** or **kicked**, based on configuration and permissions. Actions are logged to a Discord webhook for moderation visibility.

---

## 🛠️ Features
- 🛑 Detects AFK players near NPC shopkeepers and (optionally) Drone Marketplaces
- 🚫 Prevents autoclicker abuse with a configurable AFK timeout
- 🌀 Teleports or kicks offenders (configurable behavior)
- 🔐 Permission-based kicking (only specific players can be kicked)
- 📡 Logs actions (teleport/kick) to a configurable Discord webhook
- 🗺️ Configurable monitoring for Drone Marketplaces (`marketplace.prefab`)
- ⚙️ Fully configurable via `oxide/config/AntiAutoClicker.json`
- ✅ Lightweight, optimized, and has no plugin dependencies

---

## 🏗️ Installation
1. **Download** `AntiAutoClicker.cs`
2. Place it in your server’s `oxide/plugins/` folder
3. Reload the plugin:
   ```sh
   oxide.reload AntiAutoClicker
   ```

---

## ⚙️ Configuration
Generated at `oxide/config/AntiAutoClicker.json`:

```json
{
    "AFKThresholdSeconds": 180.0,
    "ShopkeeperProximityMeters": 3.0,
    "TeleportDistanceMeters": 3.0,
    "CheckIntervalSeconds": 15.0,
    "KickInsteadOfTeleport": false,
    "DiscordWebhookURL": "",
    "CheckDroneMarketplaces": false
}
```

---

### ⚙️ Configuration Settings

- **`AFKThresholdSeconds`**  
  Time (in seconds) a player must remain idle near a shopkeeper to trigger action.  
  **Default:** `180`

- **`ShopkeeperProximityMeters`**  
  Radius (in meters) around NPC shopkeepers used to detect proximity.  
  **Default:** `3`

- **`TeleportDistanceMeters`**  
  Distance (in meters) the player is moved backwards if teleported.  
  **Default:** `3`

- **`CheckIntervalSeconds`**  
  How often the server checks for AFK players near shopkeepers.  
  **Default:** `15`

- **`KickInsteadOfTeleport`**  
  If `true`, kicks players instead of teleporting them — but only if they have the `antiautoclicker.kickable` permission.  
  **Default:** `false`

- **`DiscordWebhookURL`**  
  Optional. If set, logs all kicks and teleports to this Discord webhook.  
  **Default:** `""`

---



💡 After editing the config, reload with:
```sh
oxide.reload AntiAutoClicker
```

---

## 🔑 Permissions
To **enable kicking** for specific players when `KickInsteadOfTeleport` is enabled, grant them this permission:

```sh
oxide.grant user <name|steamid> antiautoclicker.kickable
```

Players without this permission will still be teleported instead of kicked.

---

## 🏃 How It Works
1. All invisible NPC shopkeepers (`shopkeeper_vm_invis`) are tracked at plugin load.
2. Every `CheckIntervalSeconds`, the plugin checks all **active** players.
3. If a player remains in `ShopkeeperProximityMeters` of any shopkeeper for longer than `AFKThresholdSeconds`:
   - They are **kicked** (if `KickInsteadOfTeleport` is true **and** they have permission)
   - Otherwise, they are **teleported** backward by `TeleportDistanceMeters`
4. Events are **logged to Discord** if a webhook URL is configured.

---

## 🔄 Commands
No chat or console commands are needed — it's fully automated.

---

## ⚡ Performance Notes
- Optimized to run only every `CheckIntervalSeconds` (default: 15s)
- Scales linearly with number of active players × number of shopkeepers
- Suitable for most servers with moderate-to-large populations

---

## 🙏 Support

If you found this plugin useful and want to support development:

💸 Zap sats to [orangemart@geyser.fund](https://geyser.fund/project/orange?hero=orangemart)  
📡 Follow us on [Nostr](https://primal.net/ORANGEMART)


Thanks for using AntiAutoClicker! 🧡
