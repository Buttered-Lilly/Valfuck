using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEditor;
using Buttplug;
using System;
using System.Threading.Tasks;

namespace FuckHeim
{
    [BepInPlugin("Lilly.FuckHeim", "FuckHeim", "1.0.0")]
    [BepInProcess("valheim.exe")]

    public class FuckHeim : BaseUnityPlugin
    {
        ConfigEntry<int> mode;
        ConfigEntry<int> gain;
        ConfigEntry<int> updatetime;
        ConfigEntry<int> devicecount;
        async void Awake()
        {
            mode = Config.Bind("Modes", "Stat Mode", 1, "Selects the stat that is used for power 1: use health 2: use stamina 3: use higher power of health and stamina");
            gain = Config.Bind("Modes", "Power Mode", 1, "Selects the method that is used for power 1: power goes up as stat goes down 0: power goes up as stat goes up");
            updatetime = Config.Bind("Modes", "Update Time", 100, "sets the time between power updates in milliseconds");
            devicecount = Config.Bind("Devices", "Device Count", 0, "set the number of devices you intend to connect, leave 0 if you're only using one device, this may not work with the default lovense adapter");
            await Run();
        }


        private async Task Run()
        {
            var connector = new ButtplugEmbeddedConnectorOptions();
            var client = new ButtplugClient("FuckHeim Client");

            try
            {
                await client.ConnectAsync(connector);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Can't connect, exiting!");
                Debug.LogWarning($"Message: {ex.InnerException.Message}");
                return;
            }
            Debug.LogWarning("Server Connected");
        devicelost:
            await client.StartScanningAsync();
            while (client.Devices.Length == 0)
                await Task.Delay(100);
            await client.StopScanningAsync();
            Debug.LogWarning("Client currently knows about these devices:");
            foreach (var device in client.Devices)
            {
                Debug.LogWarning($"- {device.Name}");
            }

            foreach (var device in client.Devices)
            {
                Debug.LogWarning($"{device.Name} supports these messages:");
                foreach (var msgInfo in device.AllowedMessages)
                {
                    Debug.LogWarning($"- {msgInfo.Key.ToString()}");
                    if (msgInfo.Value.FeatureCount != 0)
                    {
                        Debug.LogWarning($" - Features: {msgInfo.Value.FeatureCount}");
                    }
                }
            }

            Debug.LogWarning("Sending commands");

            var clientDevice = client.Devices;

            while (true)
            {
                playerlost:
                await Task.Delay(1000);
                try
                {
                    for (int i = 0; i < clientDevice.Length; i++)
                    {
                        await Task.Delay(updatetime.Value);
                        if (mode.Value == 1)
                        {
                            await clientDevice[i].SendVibrateCmd(Mathf.Abs(gain.Value - Player.m_localPlayer.GetHealthPercentage()));
                        }
                        else if (mode.Value == 2)
                        {
                            await clientDevice[i].SendVibrateCmd(Mathf.Abs(gain.Value - Player.m_localPlayer.GetStaminaPercentage()));
                        }
                        else if (mode.Value == 3)
                        {
                            if (Mathf.Abs(gain.Value - Player.m_localPlayer.GetHealthPercentage()) >= Mathf.Abs(gain.Value - Player.m_localPlayer.GetStaminaPercentage()))
                                await clientDevice[i].SendVibrateCmd(Mathf.Abs(gain.Value - Player.m_localPlayer.GetHealthPercentage()));
                            else
                                await clientDevice[i].SendVibrateCmd(Mathf.Abs(gain.Value - Player.m_localPlayer.GetStaminaPercentage()));
                        }
                        else
                            mode.Value = 1;
                    }
                }
                catch (ButtplugDeviceException)
                {
                    Debug.LogWarning("device lost");
                    goto devicelost;
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);
                    goto playerlost;
                }
            }
        }
    }
}
