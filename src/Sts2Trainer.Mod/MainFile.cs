using System.Reflection;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using Sts2Trainer.Mod.Runtime;
using Sts2Trainer.Shared;

namespace Sts2Trainer.Mod;

[ModInitializer(nameof(Initialize))]
public sealed partial class MainFile : Node
{
    private static Harmony? _harmony;

    private static void Initialize()
    {
        if (_harmony is not null)
        {
            return;
        }

        ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        _harmony = new Harmony(TrainerConstants.ModId);
        TrainerPatches.Apply(_harmony);
        TrainerRuntime.Install();

        GD.Print($"[{TrainerConstants.ModId}] initialized.");
    }
}
