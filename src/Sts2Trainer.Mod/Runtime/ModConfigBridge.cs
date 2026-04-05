using Godot;
using System.Reflection;
using Sts2Trainer.Shared;

namespace Sts2Trainer.Mod.Runtime;

internal static class ModConfigBridge
{
    private static bool _available;
    private static bool _registered;
    private static TrainerRuntime? _runtime;
    private static Type? _apiType;
    private static Type? _entryType;
    private static Type? _configTypeEnum;

    public static void DeferredRegister(TrainerRuntime runtime)
    {
        _runtime = runtime;
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            return;
        }

        tree.ProcessFrame += OnNextFrame;
    }

    private static void OnNextFrame()
    {
        if (Engine.GetMainLoop() is SceneTree tree)
        {
            tree.ProcessFrame -= OnNextFrame;
        }

        Detect();
        if (_available)
        {
            Register();
        }
        else
        {
            TrainerLog.Info("未检测到 ModConfig，已切换为内置设置面板。");
        }
    }

    private static void Detect()
    {
        try
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(static assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch
                    {
                        return Type.EmptyTypes;
                    }
                })
                .ToArray();

            _apiType = allTypes.FirstOrDefault(static t => t.FullName == "ModConfig.ModConfigApi");
            _entryType = allTypes.FirstOrDefault(static t => t.FullName == "ModConfig.ConfigEntry");
            _configTypeEnum = allTypes.FirstOrDefault(static t => t.FullName == "ModConfig.ConfigType");
            _available = _apiType is not null && _entryType is not null && _configTypeEnum is not null;
        }
        catch (Exception ex)
        {
            _available = false;
            TrainerLog.Warn($"ModConfig detection failed: {ex.Message}");
        }
    }

    private static void Register()
    {
        if (_registered || _runtime is null || _apiType is null || _entryType is null || _configTypeEnum is null)
        {
            return;
        }

        _registered = true;

        try
        {
            var entries = BuildEntries();
            var displayNames = new Dictionary<string, string>
            {
                ["en"] = "STS2 Trainer",
                ["zhs"] = "STS2 训练器"
            };

            var registerMethod = _apiType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(static method => method.Name == "Register")
                .OrderByDescending(static method => method.GetParameters().Length)
                .First();

            if (registerMethod.GetParameters().Length == 4)
            {
                registerMethod.Invoke(null, [TrainerConstants.ModId, displayNames["en"], displayNames, entries]);
            }
            else
            {
                registerMethod.Invoke(null, [TrainerConstants.ModId, displayNames["en"], entries]);
            }

            TrainerLog.Info("ModConfig 集成已注册。");
        }
        catch (Exception ex)
        {
            TrainerLog.Warn($"ModConfig 注册失败：{ex.Message}");
        }
    }

    private static System.Array BuildEntries()
    {
        if (_runtime is null || _entryType is null)
        {
            throw new InvalidOperationException("ModConfig bridge is not initialized.");
        }

        var list = new List<object>
        {
            Header("General", "通用"),
            Toggle(
                "godMode",
                "Unlimited HP",
                "无限生命",
                _runtime.Settings.GodMode,
                value =>
                {
                    _runtime.Settings.GodMode = value;
                    _runtime.PersistToggle("无限生命", value);
                }),
            Toggle(
                "unlimitedEnergy",
                "Unlimited Energy",
                "无限能量",
                _runtime.Settings.UnlimitedEnergy,
                value =>
                {
                    _runtime.Settings.UnlimitedEnergy = value;
                    _runtime.PersistToggle("无限能量", value);
                }),
            Toggle(
                "freePurchase",
                "Free Purchase in Shop",
                "商店免费",
                _runtime.Settings.FreePurchaseInShop,
                value =>
                {
                    _runtime.Settings.FreePurchaseInShop = value;
                    _runtime.PersistToggle("商店免费", value);
                }),
            Slider(
                "goldMultiplier",
                "Gold Multiplier",
                "金币倍率",
                (float)_runtime.Settings.GoldMultiplier,
                1f,
                25f,
                0.5f,
                value =>
                {
                    _runtime.Settings.GoldMultiplier = (decimal)value;
                    _runtime.PersistSilently();
                }),
            Slider(
                "damageMultiplier",
                "Damage Multiplier",
                "伤害倍率",
                (float)_runtime.Settings.DamageMultiplier,
                1f,
                25f,
                0.5f,
                value =>
                {
                    _runtime.Settings.DamageMultiplier = (decimal)value;
                    _runtime.PersistSilently();
                }),
            Slider(
                "defenseMultiplier",
                "Defense Multiplier",
                "减伤倍率",
                (float)_runtime.Settings.DefenseMultiplier,
                1f,
                25f,
                0.5f,
                value =>
                {
                    _runtime.Settings.DefenseMultiplier = (decimal)value;
                    _runtime.PersistSilently();
                }),
            Slider(
                "gameSpeed",
                "Game Speed",
                "游戏速度",
                _runtime.Settings.GameSpeed,
                0.25f,
                5f,
                0.25f,
                value =>
                {
                    _runtime.Settings.GameSpeed = value;
                    _runtime.PersistSilently();
                }),
            Toggle(
                "unknownTreasure",
                "Unknown Nodes Always Treasure",
                "未知节点优先宝藏房",
                _runtime.Settings.UnknownMapPointsAlwaysGiveTreasure,
                value =>
                {
                    _runtime.Settings.UnknownMapPointsAlwaysGiveTreasure = value;
                    _runtime.PersistToggle("未知节点优先宝藏房", value);
                })
        };

        var result = System.Array.CreateInstance(_entryType, list.Count);
        for (var index = 0; index < list.Count; index++)
        {
            result.SetValue(list[index], index);
        }

        return result;
    }

    private static object Header(string english, string chinese)
    {
        return Entry(entry =>
        {
            Set(entry, "Label", english);
            Set(entry, "Labels", Localized(english, chinese));
            Set(entry, "Type", EnumVal("Header"));
        });
    }

    private static object Toggle(string key, string english, string chinese, bool defaultValue, Action<bool> onChanged)
    {
        return Entry(entry =>
        {
            Set(entry, "Key", key);
            Set(entry, "Label", english);
            Set(entry, "Labels", Localized(english, chinese));
            Set(entry, "Type", EnumVal("Toggle"));
            Set(entry, "DefaultValue", defaultValue);
            Set(entry, "OnChanged", new Action<object>(value => onChanged(Convert.ToBoolean(value))));
        });
    }

    private static object Slider(string key, string english, string chinese, float defaultValue, float min, float max, float step, Action<float> onChanged)
    {
        return Entry(entry =>
        {
            Set(entry, "Key", key);
            Set(entry, "Label", english);
            Set(entry, "Labels", Localized(english, chinese));
            Set(entry, "Type", EnumVal("Slider"));
            Set(entry, "DefaultValue", defaultValue);
            Set(entry, "Min", min);
            Set(entry, "Max", max);
            Set(entry, "Step", step);
            Set(entry, "Format", "F2");
            Set(entry, "OnChanged", new Action<object>(value => onChanged(Convert.ToSingle(value))));
        });
    }

    private static object Entry(Action<object> configure)
    {
        if (_entryType is null)
        {
            throw new InvalidOperationException("ConfigEntry type is unavailable.");
        }

        var instance = Activator.CreateInstance(_entryType)!;
        configure(instance);
        return instance;
    }

    private static void Set(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName)?.SetValue(target, value);
    }

    private static Dictionary<string, string> Localized(string english, string chinese)
    {
        return new Dictionary<string, string>
        {
            ["en"] = english,
            ["zhs"] = chinese
        };
    }

    private static object EnumVal(string name)
    {
        if (_configTypeEnum is null)
        {
            throw new InvalidOperationException("ConfigType enum is unavailable.");
        }

        return Enum.Parse(_configTypeEnum, name);
    }
}
