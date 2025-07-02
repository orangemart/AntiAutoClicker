# AntiAutoClicker - Rust Plugin

## 📌 Overview
**AntiAutoClicker** is a Rust Oxide plugin that detects and punishes players who stay near select NPC vending machines or NPC traders (shopkeepers) for too long — often used to exploit autoclickers. Only locations explicitly marked with a configurable prefab (like Santa's sleigh) are monitored, giving server owners fine-grained control.

---

## 🛠️ Features
- 🎯 Monitors **only NPC vendors or vending machines** near a **target prefab**
- ⚙️ Configurable to scan for:
  - NPC vending machines (e.g. Outpost / Bandit vendors)
  - NPC traders (e.g. Invisible shopkeepers)
- 📍 Uses a **target prefab** (e.g. Hazmat Plushy) to decide which locations to monitor
- 💤 Detects AFK behavior based on a time threshold
- 🌀 Teleports or kicks players depending on config + permissions
- 🔐 Permission-based kicking support
- 📡 Logs moderation actions to a Discord webhook

---

## 🏗️ Installation
1. Download `AntiAutoClicker.cs`
2. Place it in your server’s `oxide/plugins/` folder
3. Reload the plugin:
   ```sh
   oxide.reload AntiAutoClicker
   ```

---

## ⚙️ Configuration
Created at `oxide/config/AntiAutoClicker.json`:
```json
{
  "AFKThresholdSeconds": 1800.0,
  "ProximityRangeMeters": 4.0,
  "TeleportDistanceMeters": 4.0,
  "CheckIntervalSeconds": 300.0,
  "KickInsteadOfTeleport": false,
  "DiscordWebhookURL": "",
  "TargetItemPrefab": "assets/prefabs/deployable/hazmatplushy/hazmatplushy_deployed.prefab",
  "UseTargetItemFilter": false,
  "MonitorNPCVendingMachines": true,
  "MonitorNPCTraders": true
}
```

### 🔍 Key Config Settings
- `AFKThresholdSeconds`: Time (in seconds) a player must loiter before being moved
- `ProximityRangeMeters`: Radius used to detect if the player is near a monitored location
- `TeleportDistanceMeters`: How far the player is moved backward (if not kicked)
- `CheckIntervalSeconds`: How often the plugin checks player positions
- `KickInsteadOfTeleport`: If `true`, kicks players (only if they have permission)
- `DiscordWebhookURL`: Logs actions to Discord if provided
- `TargetItemPrefab`: Only monitors locations with this prefab near the NPC
- `UseTargetItemFilter`: If `true`, enforces that the prefab must be nearby
- `MonitorNPCVendingMachines`: Enable/disable NPC vending machine scanning
- `MonitorNPCTraders`: Enable/disable invisible shopkeeper scanning

💡 After editing config:
```sh
oxide.reload AntiAutoClicker
```

---

## 🔑 Permissions
Grant this permission to allow players to be kicked (instead of just teleported):
```sh
oxide.grant user <name|steamid> antiautoclicker.kickable
```

---

## 🧠 How It Works
1. On load, scans the map for NPC vending machines and traders
2. If a target prefab is found within 5 meters of them, that position is monitored
3. Every 5 minutes (or as configured), players are checked
4. If a player stays within range for 30 minutes (or as configured):
   - If `KickInsteadOfTeleport` = true **and** player has permission → they are kicked
   - Otherwise, they are teleported backward
5. Discord logging is triggered if configured

---
## 🧩 Integration with Monument Addons
To make use of prefab filtering, you’ll need to place the target prefab (e.g. Hazmat Plushy) near the NPC vendor locations you wish to monitor. The easiest way to do this automatically on every wipe is with the **Monument Addons plugin**.

🔧 Check out our **Monument Addons profile** on [GitHub: `antiautoclicker`](https://github.com/orangemart/MonumentAddons/blob/main/antiautoclicker.json)
- It auto-places the marker prefab at known abuse hotspots


## 🔄 Commands
No chat or console commands are required. Everything is automated.

---

## ⚡ Performance Notes
- Monitors only select NPCs with nearby prefabs
- Low overhead — scales with monitored positions × player count

---

## 🙏 Support
If you found this plugin useful and want to support development:

💸 Zap sats to [orangemart@geyser.fund](https://geyser.fund/project/orange?hero=orangemart)  
📡 Follow [Orangemart on Nostr](https://primal.net/ORANGEMART)

Thanks for using AntiAutoClicker! 🧡
