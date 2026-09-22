using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Testaldi;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "11.1.0.2")]
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
            CustomMapItem.TryGive();
    }
}

internal static class CustomMapItem
{
    private const string SourceName = "Itm_Map";
    private const string CustomName = "Itm_TestaldiMap";

    internal static void TryGive()
    {
        try
        {
            var itemObjectType = FindType("ItemObject");
            var coreGameManagerType = FindType("CoreGameManager");

            if (itemObjectType == null || coreGameManagerType == null)
            {
                Plugin.Log.LogError("Testaldi Map: required BB+ types were not found.");
                return;
            }

            var map = FindMap(itemObjectType);
            if (map == null)
            {
                Plugin.Log.LogError("Testaldi Map: could not find the built-in Map item.");
                return;
            }

            var customMap = (UnityEngine.Object)UnityEngine.Object.Instantiate((UnityEngine.Object)map);
            customMap.name = CustomName;

            // Keep the original Map nameKey. BB+ uses the built-in item metadata
            // when handling the Map's special use behavior; changing it breaks that path.

            var core = GetSingletonInstance(coreGameManagerType);
            if (core == null)
            {
                Plugin.Log.LogError("Testaldi Map: Singleton<CoreGameManager>.Instance was not available.");
                return;
            }

            var getPlayer = coreGameManagerType.GetMethod(
                "GetPlayer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            object? player = getPlayer?.Invoke(core, new object[] { 0 });

            if (player == null)
            {
                Plugin.Log.LogError("Testaldi Map: player 0 was not available.");
                return;
            }

            var playerManagerType = FindType("PlayerManager");
            var inventoryField = playerManagerType?.GetField(
                "itm",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var inventory = inventoryField?.GetValue(player);

            if (inventory == null)
            {
                Plugin.Log.LogError("Testaldi Map: player inventory was not available.");
                return;
            }

            var addItem = inventory.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "AddItem")
                        return false;

                    var parameters = m.GetParameters();
                    return parameters.Length == 1 &&
                           parameters[0].ParameterType.IsAssignableFrom(itemObjectType);
                });

            if (addItem == null)
            {
                Plugin.Log.LogError("Testaldi Map: no Inventory.AddItem(ItemObject) method was found.");
                return;
            }

            addItem.Invoke(inventory, new[] { customMap });
            Plugin.Log.LogInfo("F2 pressed — gave the player the custom Testaldi Map.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Testaldi Map failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static object? GetSingletonInstance(Type targetType)
    {
        var singletonTypeName = "Singleton" + ((char)96) + "1";
        var singletonType = FindType(singletonTypeName);

        if (singletonType == null || !singletonType.IsGenericTypeDefinition)
        {
            Plugin.Log.LogError("Testaldi Map: could not find the game's Singleton<T> type.");
            return null;
        }

        var closedSingleton = singletonType.MakeGenericType(targetType);

        var instanceProperty = closedSingleton.GetProperty(
            "Instance",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        return instanceProperty?.GetValue(null);
    }

    private static object? FindMap(Type itemObjectType)
    {
        var allItems = Resources.FindObjectsOfTypeAll(itemObjectType);

        foreach (var obj in allItems)
        {
            if (obj == null)
                continue;

            if (obj.name == SourceName ||
                obj.name.IndexOf("Map", StringComparison.OrdinalIgnoreCase) >= 0)
                return obj;
        }

        return null;
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
