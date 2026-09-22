using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
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
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Log.LogInfo("F2 pressed — Testaldi map-item test hotkey triggered.");
        }
    }
}
