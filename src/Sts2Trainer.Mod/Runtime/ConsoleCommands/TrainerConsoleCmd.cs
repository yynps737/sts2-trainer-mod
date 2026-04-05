using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Sts2Trainer.Mod.Runtime.ConsoleCommands;

public sealed class TrainerConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "trainer";

    public override string Args => "overlay | reload | export-log | preset <balanced|economy|overkill> | gold <value>";

    public override string Description => "控制 STS2 Trainer 面板与快捷操作。";

    public override bool IsNetworked => false;

    public override bool DebugOnly => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        var runtime = TrainerRuntime.Current;
        if (runtime is null)
        {
            return new CmdResult(success: false, msg: "Trainer 运行时尚未加载。");
        }

        if (args.Length == 0)
        {
            return new CmdResult(success: true, msg: $"用法: {CmdName} {Args}");
        }

        return args[0].ToLowerInvariant() switch
        {
            "overlay" => ToggleOverlay(runtime),
            "reload" => Reload(runtime),
            "export-log" => ExportLog(runtime),
            "preset" => ApplyPreset(runtime, args),
            "gold" => SetGold(runtime, args),
            _ => new CmdResult(success: false, msg: $"未知 trainer 子命令: {args[0]}")
        };
    }

    private static CmdResult ToggleOverlay(TrainerRuntime runtime)
    {
        runtime.ToggleOverlay();
        return new CmdResult(success: true, msg: "Trainer 面板已切换。");
    }

    private static CmdResult Reload(TrainerRuntime runtime)
    {
        runtime.ReloadSettings();
        return new CmdResult(success: true, msg: "Trainer 设置已重载。");
    }

    private static CmdResult ExportLog(TrainerRuntime runtime)
    {
        runtime.ExportLog();
        return new CmdResult(success: true, msg: "Trainer 日志已导出。");
    }

    private static CmdResult ApplyPreset(TrainerRuntime runtime, string[] args)
    {
        if (args.Length < 2)
        {
            return new CmdResult(success: false, msg: "用法: trainer preset <balanced|economy|overkill>");
        }

        runtime.ApplyPreset(args[1]);
        return new CmdResult(success: true, msg: $"已应用预设: {args[1]}");
    }

    private static CmdResult SetGold(TrainerRuntime runtime, string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out var value))
        {
            return new CmdResult(success: false, msg: "用法: trainer gold <value>");
        }

        runtime.Settings.GoldSetValue = value;
        runtime.RequestGoldSet();
        runtime.PersistSilently();
        return new CmdResult(success: true, msg: $"已排队设置金币 => {value}");
    }
}
