using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Sts2Trainer.Mod.Runtime.Services;
using Sts2Trainer.Mod.Runtime.UI;
using Sts2Trainer.Shared;

namespace Sts2Trainer.Mod.Runtime;

public sealed partial class TrainerRuntime : Node
{
    private const double FeatureSyncIntervalSeconds = 0.15d;
    private const double UiSyncIntervalSeconds = 0.25d;

    private static TrainerRuntime? _current;

    private readonly TrainerSettingsStore _settingsStore = new();
    private double _lastFeatureSyncAt;
    private double _lastUiSyncAt;
    private bool _lastTogglePressed;
    private bool _goldSetPending;
    private TrainerOverlay? _overlay;
    private DetectedGameVersion _gameVersion = DetectedGameVersion.Unknown;
    private UiLanguage _lastResolvedLanguage = UiLanguage.Auto;

    public static TrainerRuntime? Current => _current;

    public TrainerSettings Settings { get; private set; } = new();

    public UiLanguage ResolvedLanguage => ResolveLanguage(Settings.Language);

    public string VersionLabel => _gameVersion.ToDisplayString();

    public string CompatibilityLabel
    {
        get
        {
            if (!Settings.SafeMode)
            {
                return Text("Safe mode off", "安全模式关闭");
            }

            if (_gameVersion.Version == TrainerConstants.SupportedGameVersion &&
                _gameVersion.Commit == TrainerConstants.SupportedCommit)
            {
                return Text("Verified build", "已适配");
            }

            return Text(
                $"Unverified build · expected {TrainerConstants.SupportedGameVersion} / {TrainerConstants.SupportedCommit}",
                $"未验证 · 期望 {TrainerConstants.SupportedGameVersion} / {TrainerConstants.SupportedCommit}");
        }
    }

    public bool GameplayFeaturesAllowed
    {
        get
        {
            if (Settings.SinglePlayerOnly && !RunManager.Instance.IsSinglePlayerOrFakeMultiplayer)
            {
                return false;
            }

            if (Settings.SafeMode)
            {
                return _gameVersion.Version == TrainerConstants.SupportedGameVersion &&
                       _gameVersion.Commit == TrainerConstants.SupportedCommit;
            }

            return true;
        }
    }

    public static void Install()
    {
        if (_current is not null)
        {
            return;
        }

        if (Engine.GetMainLoop() is not SceneTree tree || tree.Root is null)
        {
            TrainerLog.Error("无法安装运行时：SceneTree 根节点不可用。");
            return;
        }

        var runtime = new TrainerRuntime
        {
            Name = "Sts2TrainerRuntime",
            ProcessMode = ProcessModeEnum.Always
        };

        _current = runtime;
        tree.Root.CallDeferred(Node.MethodName.AddChild, runtime);
    }

    public override void _Ready()
    {
        Settings = _settingsStore.Load();
        _gameVersion = DetectedGameVersion.Read();

        _overlay = new TrainerOverlay();
        AddChild(_overlay);
        _overlay.Initialize(this);
        _overlay.Visible = Settings.ShowOverlayByDefault;
        _lastResolvedLanguage = ResolvedLanguage;

        ModConfigBridge.DeferredRegister(this);
        TrainerLog.Info(Text($"Panel ready · {VersionLabel}", $"面板已就绪 · {VersionLabel}"));
    }

    public override void _ExitTree()
    {
        if (ReferenceEquals(_current, this))
        {
            _current = null;
        }

        Engine.TimeScale = 1f;
    }

    public override void _Process(double delta)
    {
        if (_overlay is not null)
        {
            var currentLanguage = ResolvedLanguage;
            if (currentLanguage != _lastResolvedLanguage)
            {
                _lastResolvedLanguage = currentLanguage;
                _overlay.Rebuild();
                _overlay.SyncFromSettings();
            }
        }

        HandleToggleHotkey();
        ApplyGlobalState();

        var now = Time.GetTicksMsec() / 1000.0d;
        if (now - _lastFeatureSyncAt >= FeatureSyncIntervalSeconds)
        {
            _lastFeatureSyncAt = now;
            SyncGameplayFeatures();
        }

        if (_overlay is not null && now - _lastUiSyncAt >= UiSyncIntervalSeconds)
        {
            _lastUiSyncAt = now;
            _overlay.Refresh(CreateSnapshot(), VersionLabel, CompatibilityLabel, TrainerLog.Snapshot());
        }
    }

    public void ToggleOverlay()
    {
        if (_overlay is null)
        {
            return;
        }

        _overlay.Visible = !_overlay.Visible;
        Settings.ShowOverlayByDefault = _overlay.Visible;
        PersistSilently();
    }

    public void PersistSilently()
    {
        _settingsStore.Save(Settings);
    }

    public void PersistToggle(string label, bool enabled)
    {
        PersistSilently();
        TrainerLog.Info(enabled
            ? Text($"Enabled · {label}", $"已开启 · {label}")
            : Text($"Disabled · {label}", $"已关闭 · {label}"));
    }

    public void PersistValue(string label, double value, bool decimals = false)
    {
        PersistSilently();
        var formatted = decimals ? value.ToString("0.##") : value.ToString("0");
        TrainerLog.Info(Text($"Updated · {label} · {formatted}", $"已更新 · {label} · {formatted}"));
    }

    public void PersistAction(string english, string chinese)
    {
        PersistSilently();
        TrainerLog.Info(Text(english, chinese));
    }

    public void ReloadSettings()
    {
        Settings = _settingsStore.Load();
        _overlay?.SyncFromSettings();
        TrainerLog.Info(Text("Settings reloaded", "设置已重载"));
    }

    public void RequestGoldSet()
    {
        _goldSetPending = true;
        TrainerLog.Info(Text($"Gold update queued · {Settings.GoldSetValue}", $"金币设置已提交 · {Settings.GoldSetValue}"));
    }

    public void ApplyPreset(string presetId)
    {
        var preset = TrainerFeatures.Presets.FirstOrDefault(p => p.Id == presetId);
        if (preset is null)
        {
            TrainerLog.Warn(Text($"Preset not found · {presetId}", $"未找到预设 · {presetId}"));
            return;
        }

        var clone = Settings.Clone();
        preset.Apply(clone);
        Settings = clone;
        _overlay?.SyncFromSettings();
        PersistAction($"Preset applied · {preset.Name.Resolve(ResolvedLanguage)}", $"已应用预设 · {preset.Name.Resolve(ResolvedLanguage)}");
    }

    public void ExportLog()
    {
        var path = TrainerLog.Export(_settingsStore.GetLogDirectory());
        TrainerLog.Info(Text($"Log exported · {path}", $"日志已导出 · {path}"));
    }

    public bool TryGetLocalPlayer(out Player? player)
    {
        player = null;
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState is null)
        {
            return false;
        }

        player = LocalContext.GetMe(runState) ?? runState.Players.FirstOrDefault();
        return player is not null;
    }

    private void HandleToggleHotkey()
    {
        if (!Enum.TryParse<Key>(Settings.TogglePanelKey, true, out var key))
        {
            key = Key.F9;
        }

        var pressed = Input.IsKeyPressed(key);
        if (pressed && !_lastTogglePressed)
        {
            ToggleOverlay();
        }

        _lastTogglePressed = pressed;
    }

    private void ApplyGlobalState()
    {
        Engine.TimeScale = Mathf.Clamp(Settings.GameSpeed, 0.25f, 5f);
    }

    private void SyncGameplayFeatures()
    {
        if (!TryGetLocalPlayer(out var player) || player is null)
        {
            return;
        }

        if (!GameplayFeaturesAllowed)
        {
            return;
        }

        if (Settings.GodMode && player.Creature.CurrentHp < player.Creature.MaxHp)
        {
            Run(CreatureCmd.SetCurrentHp(player.Creature, player.Creature.MaxHp), "Restore HP", "恢复生命");
        }

        if (Settings.UnlimitedBlock &&
            CombatManager.Instance.IsInProgress &&
            player.Creature.Block < Settings.BlockFloor)
        {
            var missingBlock = Settings.BlockFloor - player.Creature.Block;
            if (missingBlock > 0)
            {
                Run(CreatureCmd.GainBlock(player.Creature, missingBlock, ValueProp.Unpowered, null, fast: true), "Restore block", "恢复格挡");
            }
        }

        if (Settings.UnlimitedEnergy &&
            CombatManager.Instance.IsInProgress &&
            player.PlayerCombatState is not null &&
            player.PlayerCombatState.Energy < player.PlayerCombatState.MaxEnergy)
        {
            Run(PlayerCmd.SetEnergy(player.PlayerCombatState.MaxEnergy, player), "Restore energy", "恢复能量");
        }

        if (Settings.UnlimitedStars &&
            CombatManager.Instance.IsInProgress &&
            player.PlayerCombatState is not null &&
            player.PlayerCombatState.Stars < Settings.TargetStars)
        {
            Run(PlayerCmd.SetStars(Settings.TargetStars, player), "Restore stars", "恢复星数");
        }

        if (Settings.EnforcePotionSlots)
        {
            SyncPotionSlots(player);
        }

        if (_goldSetPending)
        {
            _goldSetPending = false;
            Run(PlayerCmd.SetGold(Settings.GoldSetValue, player), "Set gold", "设置金币");
        }
    }

    private void SyncPotionSlots(Player player)
    {
        var delta = Settings.PotionSlotTarget - player.MaxPotionCount;
        if (delta > 0)
        {
            Run(PlayerCmd.GainMaxPotionCount(delta, player), "Increase potion slots", "增加药水栏位");
        }
        else if (delta < 0)
        {
            Run(PlayerCmd.LoseMaxPotionCount(-delta, player), "Decrease potion slots", "减少药水栏位");
        }
    }

    private TrainerSnapshot CreateSnapshot()
    {
        if (!TryGetLocalPlayer(out var player) || player is null)
        {
            return new TrainerSnapshot(false, RunManager.Instance.IsSinglePlayerOrFakeMultiplayer, CombatManager.Instance.IsInProgress, null, null, null, null);
        }

        var round = CombatManager.Instance.DebugOnlyGetState()?.RoundNumber;
        return new TrainerSnapshot(
            true,
            RunManager.Instance.IsSinglePlayerOrFakeMultiplayer,
            CombatManager.Instance.IsInProgress,
            round,
            player.Creature.CurrentHp,
            player.Creature.MaxHp,
            player.Gold);
    }

    private static UiLanguage ResolveLanguage(UiLanguage requested)
    {
        if (requested != UiLanguage.Auto)
        {
            return requested;
        }

        var gameLanguage = LocManager.Instance?.Language;
        if (!string.IsNullOrWhiteSpace(gameLanguage))
        {
            return gameLanguage.ToLowerInvariant() switch
            {
                "zhs" => UiLanguage.ChineseSimplified,
                _ => UiLanguage.English
            };
        }

        var locale = OS.GetLocaleLanguage();
        if (!string.IsNullOrWhiteSpace(locale) && locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return UiLanguage.ChineseSimplified;
        }

        return UiLanguage.English;
    }

    private string Text(string english, string chinese)
    {
        return ResolvedLanguage == UiLanguage.ChineseSimplified ? chinese : english;
    }

    private async void Run(Task task, string englishOperation, string chineseOperation)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            TrainerLog.Error(Text($"{englishOperation} failed: {ex.Message}", $"{chineseOperation}失败：{ex.Message}"));
        }
    }

    private readonly record struct TrainerSnapshot(
        bool HasRun,
        bool IsSinglePlayerRun,
        bool IsInCombat,
        int? CurrentRound,
        int? PlayerCurrentHp,
        int? PlayerMaxHp,
        int? PlayerGold) : IGameStateSnapshot;

    private sealed record DetectedGameVersion(string? Version, string? Commit)
    {
        public static DetectedGameVersion Unknown { get; } = new(null, null);

        public static DetectedGameVersion Read()
        {
            try
            {
                var exePath = OS.GetExecutablePath();
                var root = Path.GetDirectoryName(exePath);
                if (string.IsNullOrWhiteSpace(root))
                {
                    return Unknown;
                }

                var releaseInfoPath = Path.Combine(root, "release_info.json");
                if (!File.Exists(releaseInfoPath))
                {
                    return Unknown;
                }

                using var stream = File.OpenRead(releaseInfoPath);
                using var document = JsonDocument.Parse(stream);
                var version = document.RootElement.TryGetProperty("version", out var versionElement)
                    ? versionElement.GetString()
                    : null;
                var commit = document.RootElement.TryGetProperty("commit", out var commitElement)
                    ? commitElement.GetString()
                    : null;
                return new DetectedGameVersion(version, commit);
            }
            catch (Exception ex)
            {
                TrainerLog.Warn($"读取 release_info.json 失败：{ex.Message}");
                return Unknown;
            }
        }

        public string ToDisplayString()
        {
            if (string.IsNullOrWhiteSpace(Version) && string.IsNullOrWhiteSpace(Commit))
            {
                return "unknown build";
            }

            return $"{Version ?? "unknown"} / {Commit ?? "unknown"}";
        }
    }
}
