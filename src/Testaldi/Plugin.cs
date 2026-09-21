using BepInEx;
using BepInEx.Logging;

namespace Testaldi;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
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
    }
}
