using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using System;
using System.Reflection;
using UnityEngine;

namespace Testaldi;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "11.1.1.0")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "carjam120443.testaldi";
    public const string PluginName = "Testaldi";
    public const string PluginVersion = "0.1.0";

    internal static ManualLogSource Log { get; private set; } = null!;

    private void Awake()
    {
        Log = Logger;
        Log.LogInfo($"{PluginName} {PluginVersion} loaded.");

        if (Chainloader.PluginInfos.TryGetValue("mtm101.rulerp.bbplus.baldidevapi", out var api))
            Log.LogInfo($"Baldi's Basics Plus Dev API detected: {api.Metadata.Version}");
        else
            Log.LogError("Baldi's Basics Plus Dev API was not detected.");
    }

    private void Update()
    {
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F2))
        {
            GiveMapForF2();
        }
    }

    private static void GiveMapForF2()
    {
        try
        {
            var coreGameManagerType = FindType("CoreGameManager");
            var playerManagerType = FindType("PlayerManager");
            var itemsType = FindType("Items");

            if (coreGameManagerType == null || playerManagerType == null || itemsType == null)
            {
                Log.LogError("F2 map test could not find the BB+ game types.");
                return;
            }

            var coreInstanceProperty = coreGameManagerType.GetProperty(
                "Instance",
                BindingFlags.Public | BindingFlags.Static);

            object? coreInstance = coreInstanceProperty?.GetValue(null);
            if (coreInstance == null)
            {
                Log.LogError("F2 map test could not find CoreGameManager.Instance.");
                return;
            }

            var getPlayer = coreGameManagerType.GetMethod(
                "GetPlayer",
                BindingFlags.Public | BindingFlags.Instance);

            object? player = getPlayer?.Invoke(coreInstance, new object[] { 0 });
            if (player == null)
            {
                Log.LogError("F2 map test could not find player 0.");
                return;
            }

            var inventoryField = playerManagerType.GetField(
                "itm",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            object? inventory = inventoryField?.GetValue(player);
            if (inventory == null)
            {
                Log.LogError("F2 map test could not find the player's item inventory.");
                return;
            }

            var mapValue = Enum.Parse(itemsType, "Map");
            var addItem = inventory.GetType().GetMethod(
                "AddItem",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { itemsType },
                null);

            if (addItem == null)
            {
                Log.LogError("F2 map test could not find Inventory.AddItem(Items).");
                return;
            }

            addItem.Invoke(inventory, new[] { mapValue });
            Log.LogInfo("F2 pressed — gave the player the built-in BB+ Map item.");
        }
        catch (Exception ex)
        {
            Log.LogError($"F2 map test failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static Type? FindType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name);
            if (type != null)
                return type;
        }

        return null;
    }
}
