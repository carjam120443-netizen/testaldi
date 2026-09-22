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
        {
            Log.LogInfo($"Baldi's Basics Plus Dev API detected: {api.Metadata.Version}");
            CustomMapItem.Initialize(api);
        }
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
    private const string CustomName = "TestaldiMap";

    private static object? customMap;
    private static object? apiMetadata;

    internal static void Initialize(object metadata)
    {
        apiMetadata = metadata;
    }

    internal static void TryGive()
    {
        try
        {
            var itemObjectType = FindType("ItemObject");
            var itemType = FindType("Item");
            var itemBuilderType = FindType("MTM101BaldAPI.ObjectCreation.ItemBuilder");

            if (itemObjectType == null || itemType == null || itemBuilderType == null || apiMetadata == null)
            {
                Plugin.Log.LogError("Testaldi Map: Dev API ItemBuilder or required BB+ types were not found.");
                return;
            }

            customMap ??= BuildCustomMap(itemObjectType, itemType, itemBuilderType);

            if (customMap == null)
                return;

            var coreGameManagerType = FindType("CoreGameManager");
            if (coreGameManagerType == null)
            {
                Plugin.Log.LogError("Testaldi Map: CoreGameManager type was not found.");
                return;
            }

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
            Plugin.Log.LogInfo("F2 pressed — gave the registered Testaldi Map.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Testaldi Map failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static object? BuildCustomMap(Type itemObjectType, Type itemType, Type itemBuilderType)
    {
        var map = FindMap(itemObjectType);
        if (map == null)
        {
            Plugin.Log.LogError("Testaldi Map: could not find the built-in Map item.");
            return null;
        }

        var itemField = itemObjectType.GetField(
            "item",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        var mapItem = itemField?.GetValue(map);

        if (mapItem == null || !itemType.IsInstanceOfType(mapItem))
        {
            Plugin.Log.LogError("Testaldi Map: built-in Map item component was not available.");
            return null;
        }

        var constructor = itemBuilderType.GetConstructor(new[] { apiMetadata!.GetType() });
        if (constructor == null)
        {
            Plugin.Log.LogError("Testaldi Map: could not construct the Dev API ItemBuilder.");
            return null;
        }

        var builder = constructor.Invoke(new[] { apiMetadata });

        var itemsEnumType = FindType("Items");
        var setEnum = itemsEnumType == null
            ? null
            : itemBuilderType.GetMethod("SetEnum", new[] { itemsEnumType });
        var setName = itemBuilderType.GetMethod(
            "SetNameAndDescription",
            new[] { typeof(string), typeof(string) });

        var setComponentDefinition = itemBuilderType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m =>
            {
                if (m.Name != "SetItemComponent" || !m.IsGenericMethodDefinition)
                    return false;

                var parameters = m.GetParameters();
                return parameters.Length == 1 &&
                       parameters[0].ParameterType.IsGenericParameter;
            });

        var setComponent = setComponentDefinition?.MakeGenericMethod(itemType);

        var build = itemBuilderType.GetMethod("Build", Type.EmptyTypes);

        if (itemsEnumType == null || setEnum == null || setName == null || setComponent == null || build == null)
        {
            Plugin.Log.LogError("Testaldi Map: required Dev API ItemBuilder methods were not found.");
            return null;
        }

        var mapEnum = Enum.Parse(itemsEnumType, "Map");
        setEnum.Invoke(builder, new[] { mapEnum });
        setName.Invoke(builder, new object[] { "Testaldi Map", "A Testaldi map item." });
        setComponent.Invoke(builder, new[] { mapItem });

        var built = build.Invoke(builder, null);

        if (built == null)
        {
            Plugin.Log.LogError("Testaldi Map: Dev API ItemBuilder returned no ItemObject.");
            return null;
        }

        Plugin.Log.LogInfo("Testaldi Map: built a custom ItemObject using the built-in Items.Map enum and Dev API.");
        return built;
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
