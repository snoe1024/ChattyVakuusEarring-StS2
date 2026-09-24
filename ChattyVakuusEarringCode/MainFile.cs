using System.Linq;
using System.Reflection;
using BaseLib.Config;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Chatter;
using ChattyVakuusEarring.ChattyVakuusEarringCode.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace ChattyVakuusEarring.ChattyVakuusEarringCode;

//You're recommended but not required to keep all your code in this package and all your assets in the ChattyVakuusEarring folder.
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string
        ModId = "ChattyVakuusEarring"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static ChattyVakuusEarringConfig Config { get; private set; } = null!;

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        Config = new ChattyVakuusEarringConfig();
        Config.Load();
        ModConfigRegistry.Register(ModId, Config);

        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);

        Harmony harmony = new(ModId);

        harmony.PatchAll(assembly);

        // [HarmonyPatch]の付け方を間違えると、例外も出ずに何もパッチされないことがある。
        // 実際にパッチされたメソッドを毎起動時に出しておき、godot.logだけで気付けるようにする。
        // (sts2_dev_knowledge/topics/harmony-patching.md)
        var patchedMethods = harmony.GetPatchedMethods().ToList();
        Logger.Info($"{ModId}: Harmony patched {patchedMethods.Count} method(s) on startup:");
        foreach (var method in patchedMethods)
        {
            Logger.Info($"{ModId}:   - {method.DeclaringType?.FullName}.{method.Name}");
        }

        ChatterHub.Initialize();
    }
}
