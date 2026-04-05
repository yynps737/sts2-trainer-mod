using Godot;
using Sts2Trainer.Shared;

namespace Sts2Trainer.Mod.Runtime.UI;

public sealed partial class TrainerOverlay : CanvasLayer
{
    private readonly List<RowBinding> _rows = [];
    private TrainerRuntime? _runtime;
    private PanelContainer? _panel;
    private LineEdit? _searchBox;
    private OptionButton? _categoryFilter;
    private VBoxContainer? _rowsRoot;
    private Label? _versionLabel;
    private Label? _statusLabel;
    private Label? _hintLabel;
    private RichTextLabel? _logView;

    public void Initialize(TrainerRuntime runtime)
    {
        _runtime = runtime;
        if (IsInsideTree())
        {
            BuildUi();
            SyncFromSettings();
        }
    }

    public override void _Ready()
    {
        Layer = 100;
        if (_runtime is not null)
        {
            BuildUi();
            SyncFromSettings();
        }
    }

    public void Rebuild()
    {
        _rows.Clear();
        _rowsRoot = null;
        _versionLabel = null;
        _statusLabel = null;
        _hintLabel = null;
        _logView = null;
        _searchBox = null;
        _categoryFilter = null;

        if (_panel is not null)
        {
            _panel.QueueFree();
            _panel = null;
        }

        BuildUi();
    }

    public void Refresh(IGameStateSnapshot snapshot, string versionText, string compatibilityText, IReadOnlyList<string> logLines)
    {
        if (_versionLabel is not null)
        {
            _versionLabel.Text = $"{versionText} · {compatibilityText}";
        }

        if (_statusLabel is not null)
        {
            _statusLabel.Text = snapshot.HasRun
                ? Text(
                    $"{(snapshot.IsSinglePlayerRun ? "Single-player" : "Multiplayer")} · {(snapshot.IsInCombat ? "In combat" : "Out of combat")} · Round {snapshot.CurrentRound?.ToString() ?? "-"} · {snapshot.PlayerCurrentHp}/{snapshot.PlayerMaxHp} HP · {snapshot.PlayerGold} Gold",
                    $"{(snapshot.IsSinglePlayerRun ? "单人模式" : "多人模式")} · {(snapshot.IsInCombat ? "战斗中" : "非战斗")} · 第 {snapshot.CurrentRound?.ToString() ?? "-"} 回合 · {snapshot.PlayerCurrentHp}/{snapshot.PlayerMaxHp} HP · {snapshot.PlayerGold} 金币")
                : Text("No active run", "当前未进入 run");
        }

        if (_hintLabel is not null)
        {
            _hintLabel.Text = Text(
                "Toggles switch instantly. Values save automatically. Entries with Apply require one manual action.",
                "右侧按钮直接切换；数值会自动保存；带“应用”的项目需要手动执行。");
        }

        if (_logView is not null)
        {
            _logView.Text = string.Join('\n', logLines.TakeLast(10));
        }
    }

    public void SyncFromSettings()
    {
        if (_runtime is null || _rowsRoot is null)
        {
            return;
        }

        _rows.Clear();
        foreach (var child in _rowsRoot.GetChildren())
        {
            child.QueueFree();
        }

        BuildSystemSection();
        BuildCombatSection();
        BuildEconomySection();
        BuildShopSection();
        BuildRewardsSection();
        BuildMapSection();

        ApplyFilters();
    }

    private void BuildUi()
    {
        if (_panel is not null)
        {
            return;
        }

        Name = "Sts2TrainerOverlay";
        ProcessMode = ProcessModeEnum.Always;

        _panel = new PanelContainer
        {
            Name = "TrainerPanel",
            Visible = true,
            CustomMinimumSize = new Vector2(700, 760),
            OffsetLeft = 24,
            OffsetTop = 24
        };
        _panel.AddThemeStyleboxOverride("panel", BuildPanelStyle(new Color(0.06f, 0.07f, 0.1f, 0.90f), new Color(1f, 1f, 1f, 0.08f), 14));
        AddChild(_panel);

        var rootMargin = BuildMargin(16, 16, 14, 14);
        _panel.AddChild(rootMargin);

        var outer = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        outer.AddThemeConstantOverride("separation", 12);
        rootMargin.AddChild(outer);

        BuildHeader(outer);
        BuildStatusCard(outer);
        BuildScrollArea(outer);
        BuildLogCard(outer);
    }

    private void BuildHeader(VBoxContainer outer)
    {
        var card = CreateCard();
        outer.AddChild(card);

        var margin = BuildMargin(12, 12, 12, 12);
        card.AddChild(margin);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 10);
        margin.AddChild(content);

        var titleRow = new HBoxContainer();
        titleRow.AddThemeConstantOverride("separation", 10);
        content.AddChild(titleRow);

        var titleBox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        titleBox.AddThemeConstantOverride("separation", 2);
        titleRow.AddChild(titleBox);

        var title = new Label
        {
            Text = "STS2 Trainer",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        titleBox.AddChild(title);

        var subtitle = new Label
        {
            Text = Text("Runtime Control Panel", "运行控制面板"),
            Modulate = new Color(1f, 1f, 1f, 0.58f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        titleBox.AddChild(subtitle);

        var presetBox = new OptionButton
        {
            CustomMinimumSize = new Vector2(150, 0)
        };
        foreach (var preset in TrainerFeatures.Presets)
        {
            presetBox.AddItem(preset.Name.Resolve(_runtime?.ResolvedLanguage ?? UiLanguage.English), presetBox.ItemCount);
            presetBox.SetItemMetadata(presetBox.ItemCount - 1, preset.Id);
        }
        titleRow.AddChild(presetBox);

        var presetButton = CreateUtilityButton(Text("Apply Preset", "应用预设"));
        presetButton.Pressed += () =>
        {
            if (_runtime is null || presetBox.Selected < 0)
            {
                return;
            }

            var presetId = presetBox.GetItemMetadata(presetBox.Selected).AsString();
            _runtime.ApplyPreset(presetId);
        };
        titleRow.AddChild(presetButton);

        var exportButton = CreateUtilityButton(Text("Export Log", "导出日志"));
        exportButton.Pressed += () => _runtime?.ExportLog();
        titleRow.AddChild(exportButton);

        var toolsRow = new HBoxContainer();
        toolsRow.AddThemeConstantOverride("separation", 10);
        content.AddChild(toolsRow);

        _searchBox = new LineEdit
        {
            PlaceholderText = Text("Search settings", "搜索设置"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _searchBox.TextChanged += _ => ApplyFilters();
        toolsRow.AddChild(_searchBox);

        _categoryFilter = new OptionButton
        {
            CustomMinimumSize = new Vector2(120, 0)
        };
        _categoryFilter.AddItem(Text("All", "全部"), -1);
        foreach (FeatureCategory category in Enum.GetValues<FeatureCategory>())
        {
            _categoryFilter.AddItem(CategoryLabel(category), (int)category);
        }
        _categoryFilter.ItemSelected += _ => ApplyFilters();
        toolsRow.AddChild(_categoryFilter);

        var reloadButton = CreateUtilityButton(Text("Reload", "重新载入"));
        reloadButton.Pressed += () => _runtime?.ReloadSettings();
        toolsRow.AddChild(reloadButton);
    }

    private void BuildStatusCard(VBoxContainer outer)
    {
        var card = CreateCard();
        outer.AddChild(card);

        var margin = BuildMargin(12, 12, 10, 10);
        card.AddChild(margin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);
        margin.AddChild(box);

        _versionLabel = new Label
        {
            Text = Text("Build · -", "版本 · -")
        };
        _versionLabel.Modulate = new Color(1f, 1f, 1f, 0.90f);
        box.AddChild(_versionLabel);

        _statusLabel = new Label
        {
            Text = Text("No active run", "当前未进入 run"),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        box.AddChild(_statusLabel);

        _hintLabel = new Label
        {
            Text = string.Empty,
            Modulate = new Color(1f, 1f, 1f, 0.60f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        box.AddChild(_hintLabel);
    }

    private void BuildScrollArea(VBoxContainer outer)
    {
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 430),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        outer.AddChild(scroll);

        _rowsRoot = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _rowsRoot.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_rowsRoot);
    }

    private void BuildLogCard(VBoxContainer outer)
    {
        var card = CreateCard();
        outer.AddChild(card);

        var margin = BuildMargin(12, 12, 10, 10);
        card.AddChild(margin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        margin.AddChild(box);

        var title = new Label
        {
            Text = Text("Recent Activity", "最近操作"),
            Modulate = new Color(1f, 1f, 1f, 0.74f)
        };
        box.AddChild(title);

        _logView = new RichTextLabel
        {
            CustomMinimumSize = new Vector2(0, 110),
            FitContent = true,
            ScrollFollowing = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        box.AddChild(_logView);
    }

    private void BuildSystemSection()
    {
        if (_runtime is null)
        {
            return;
        }

        AddSectionHeader(FeatureCategory.System, CategoryLabel(FeatureCategory.System));
        AddValueFeatureCard(
            FeatureCategory.System,
            Text("Game Speed", "游戏速度"),
            Text("Adjust the pace of combat and animation.", "调整战斗与动画节奏。"),
            CreateSpinBox(_runtime.Settings.GameSpeed, 0.25, 5, 0.25, value =>
            {
                _runtime.Settings.GameSpeed = (float)value;
                _runtime.PersistValue(Text("Game Speed", "游戏速度"), value, decimals: true);
            }, decimals: true));
        AddToggleFeatureCard(
            FeatureCategory.System,
            Text("Safe Mode", "安全模式"),
            Text("Only enable key features on verified builds and single-player runs.", "仅在已验证版本和单人 run 中启用关键功能。"),
            _runtime.Settings.SafeMode,
            value =>
            {
                _runtime.Settings.SafeMode = value;
                _runtime.PersistToggle(Text("Safe Mode", "安全模式"), value);
            });
    }

    private void BuildCombatSection()
    {
        if (_runtime is null)
        {
            return;
        }

        AddSectionHeader(FeatureCategory.Combat, CategoryLabel(FeatureCategory.Combat));
        AddToggleFeatureCard(
            FeatureCategory.Combat,
            Text("Unlimited HP", "无限生命"),
            Text("Ignore incoming damage and restore HP outside combat.", "忽略受到的伤害，并在非战斗阶段自动回满。"),
            _runtime.Settings.GodMode,
            value =>
            {
                _runtime.Settings.GodMode = value;
                _runtime.PersistToggle(Text("Unlimited HP", "无限生命"), value);
            });
        AddToggleFeatureCard(
            FeatureCategory.Combat,
            Text("Unlimited Block", "无限护甲"),
            Text("Maintain a block floor while enabled.", "启用后维持护甲保底值。"),
            _runtime.Settings.UnlimitedBlock,
            value =>
            {
                _runtime.Settings.UnlimitedBlock = value;
                _runtime.PersistToggle(Text("Unlimited Block", "无限护甲"), value);
            },
            controls =>
            {
                controls.AddChild(CreateLabeledSpin(
                    Text("Block Floor", "护甲保底"),
                    _runtime.Settings.BlockFloor,
                    0,
                    9999,
                    1,
                    value =>
                    {
                        _runtime.Settings.BlockFloor = value;
                        _runtime.PersistValue(Text("Block Floor", "护甲保底"), value);
                    }));
            });
        AddToggleFeatureCard(
            FeatureCategory.Combat,
            Text("Unlimited Energy", "无限能量"),
            Text("Keep energy available throughout the turn.", "持续维持本回合可用能量。"),
            _runtime.Settings.UnlimitedEnergy,
            value =>
            {
                _runtime.Settings.UnlimitedEnergy = value;
                _runtime.PersistToggle(Text("Unlimited Energy", "无限能量"), value);
            });
        AddToggleFeatureCard(
            FeatureCategory.Combat,
            Text("Unlimited Stars", "无限星星"),
            Text("Maintain the selected star reserve.", "维持设定的星星保底。"),
            _runtime.Settings.UnlimitedStars,
            value =>
            {
                _runtime.Settings.UnlimitedStars = value;
                _runtime.PersistToggle(Text("Unlimited Stars", "无限星星"), value);
            },
            controls =>
            {
                controls.AddChild(CreateLabeledSpin(
                    Text("Star Reserve", "星星保底"),
                    _runtime.Settings.TargetStars,
                    0,
                    999,
                    1,
                    value =>
                    {
                        _runtime.Settings.TargetStars = value;
                        _runtime.PersistValue(Text("Star Reserve", "星星保底"), value);
                    }));
            });
        AddToggleFeatureCard(
            FeatureCategory.Combat,
            Text("Enemies Can't Move", "敌人无法行动"),
            Text("Skip enemy turns.", "跳过敌方行动。"),
            _runtime.Settings.FreezeEnemies,
            value =>
            {
                _runtime.Settings.FreezeEnemies = value;
                _runtime.PersistToggle(Text("Enemies Can't Move", "敌人无法行动"), value);
            });
        AddValueFeatureCard(
            FeatureCategory.Combat,
            Text("Damage Multiplier", "伤害倍率"),
            Text("Increase outgoing damage.", "放大造成的伤害。"),
            CreateSpinBox((double)_runtime.Settings.DamageMultiplier, 1, 25, 0.5, value =>
            {
                _runtime.Settings.DamageMultiplier = (decimal)value;
                _runtime.PersistValue(Text("Damage Multiplier", "伤害倍率"), value, decimals: true);
            }, decimals: true));
        AddValueFeatureCard(
            FeatureCategory.Combat,
            Text("Defense Multiplier", "减伤倍率"),
            Text("Reduce incoming damage.", "降低受到的伤害。"),
            CreateSpinBox((double)_runtime.Settings.DefenseMultiplier, 1, 25, 0.5, value =>
            {
                _runtime.Settings.DefenseMultiplier = (decimal)value;
                _runtime.PersistValue(Text("Defense Multiplier", "减伤倍率"), value, decimals: true);
            }, decimals: true));
        AddToggleFeatureCard(
            FeatureCategory.Combat,
            Text("Fixed Max Energy", "固定最大能量"),
            Text("Keep max energy at the selected value.", "将最大能量维持在指定值。"),
            _runtime.Settings.EnforceMaxEnergy,
            value =>
            {
                _runtime.Settings.EnforceMaxEnergy = value;
                _runtime.PersistToggle(Text("Fixed Max Energy", "固定最大能量"), value);
            },
            controls =>
            {
                controls.AddChild(CreateLabeledSpin(
                    Text("Energy Limit", "能量上限"),
                    _runtime.Settings.MaxEnergyTarget,
                    1,
                    25,
                    1,
                    value =>
                    {
                        _runtime.Settings.MaxEnergyTarget = value;
                        _runtime.PersistValue(Text("Energy Limit", "能量上限"), value);
                    }));
            });
    }

    private void BuildEconomySection()
    {
        if (_runtime is null)
        {
            return;
        }

        AddSectionHeader(FeatureCategory.Economy, CategoryLabel(FeatureCategory.Economy));
        AddValueFeatureCard(
            FeatureCategory.Economy,
            Text("Gold Multiplier", "金币倍率"),
            Text("Increase future gold gains.", "提高之后获得的金币。"),
            CreateSpinBox((double)_runtime.Settings.GoldMultiplier, 1, 25, 0.5, value =>
            {
                _runtime.Settings.GoldMultiplier = (decimal)value;
                _runtime.PersistValue(Text("Gold Multiplier", "金币倍率"), value, decimals: true);
            }, decimals: true));
        AddActionFeatureCard(
            FeatureCategory.Economy,
            Text("Edit Gold", "编辑金币"),
            Text("Set current gold to the selected value.", "将当前金币调整到指定值。"),
            CreateLabeledSpin(
                Text("Gold", "金币"),
                _runtime.Settings.GoldSetValue,
                0,
                999999,
                1,
                value =>
                {
                    _runtime.Settings.GoldSetValue = value;
                    _runtime.PersistValue(Text("Gold Target", "金币目标"), value);
                }),
            Text("Apply", "应用"),
            () => _runtime.RequestGoldSet());
        AddToggleFeatureCard(
            FeatureCategory.Economy,
            Text("Fixed Potion Slots", "固定药水栏位"),
            Text("Keep potion slots at the selected value.", "将药水栏位维持在指定值。"),
            _runtime.Settings.EnforcePotionSlots,
            value =>
            {
                _runtime.Settings.EnforcePotionSlots = value;
                _runtime.PersistToggle(Text("Fixed Potion Slots", "固定药水栏位"), value);
            },
            controls =>
            {
                controls.AddChild(CreateLabeledSpin(
                    Text("Slot Count", "栏位数量"),
                    _runtime.Settings.PotionSlotTarget,
                    0,
                    12,
                    1,
                    value =>
                    {
                        _runtime.Settings.PotionSlotTarget = value;
                        _runtime.PersistValue(Text("Slot Count", "栏位数量"), value);
                    }));
            });
    }

    private void BuildShopSection()
    {
        if (_runtime is null)
        {
            return;
        }

        AddSectionHeader(FeatureCategory.Shop, CategoryLabel(FeatureCategory.Shop));
        AddToggleFeatureCard(
            FeatureCategory.Shop,
            Text("Free Purchase", "免费购买"),
            Text("Purchases in the shop cost no gold.", "商店购买不消耗金币。"),
            _runtime.Settings.FreePurchaseInShop,
            value =>
            {
                _runtime.Settings.FreePurchaseInShop = value;
                _runtime.PersistToggle(Text("Free Purchase", "免费购买"), value);
            });
    }

    private void BuildRewardsSection()
    {
        if (_runtime is null)
        {
            return;
        }

        AddSectionHeader(FeatureCategory.Rewards, CategoryLabel(FeatureCategory.Rewards));
        AddToggleFeatureCard(
            FeatureCategory.Rewards,
            Text("Potion Reward", "药水奖励"),
            Text("Always include a potion in rewards.", "奖励始终包含药水。"),
            _runtime.Settings.AlwaysRewardPotion,
            value =>
            {
                _runtime.Settings.AlwaysRewardPotion = value;
                _runtime.PersistToggle(Text("Potion Reward", "药水奖励"), value);
            });
        AddToggleFeatureCard(
            FeatureCategory.Rewards,
            Text("Upgraded Rewards", "升级奖励"),
            Text("Prefer upgraded card rewards.", "优先给出升级版卡牌奖励。"),
            _runtime.Settings.AlwaysUpgradeCardRewards,
            value =>
            {
                _runtime.Settings.AlwaysUpgradeCardRewards = value;
                _runtime.PersistToggle(Text("Upgraded Rewards", "升级奖励"), value);
            });
        AddToggleFeatureCard(
            FeatureCategory.Rewards,
            Text("Top Rarity Rewards", "最高稀有度奖励"),
            Text("Prefer the highest rarity available in the current pool.", "优先抽取当前奖励池中的最高稀有度。"),
            _runtime.Settings.MaxCardRewardRarity,
            value =>
            {
                _runtime.Settings.MaxCardRewardRarity = value;
                _runtime.PersistToggle(Text("Top Rarity Rewards", "最高稀有度奖励"), value);
            });
    }

    private void BuildMapSection()
    {
        if (_runtime is null)
        {
            return;
        }

        AddSectionHeader(FeatureCategory.Map, CategoryLabel(FeatureCategory.Map));
        AddToggleFeatureCard(
            FeatureCategory.Map,
            Text("Treasure Nodes", "宝藏节点"),
            Text("Prefer treasure rooms for unknown nodes.", "未知节点优先转为宝藏房。"),
            _runtime.Settings.UnknownMapPointsAlwaysGiveTreasure,
            value =>
            {
                _runtime.Settings.UnknownMapPointsAlwaysGiveTreasure = value;
                _runtime.PersistToggle(Text("Treasure Nodes", "宝藏节点"), value);
            });
    }

    private void AddSectionHeader(FeatureCategory category, string title)
    {
        if (_rowsRoot is null)
        {
            return;
        }

        var label = new Label
        {
            Text = title,
            Modulate = new Color(0.80f, 0.89f, 1f, 0.92f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _rowsRoot.AddChild(label);
        RegisterRow(label, category, title);
    }

    private void AddToggleFeatureCard(FeatureCategory category, string title, string description, bool value, Action<bool> onChanged, Action<VBoxContainer>? extendControls = null)
    {
        var parts = CreateFeatureCard(category, title, description);
        parts.Controls.AddChild(CreateToggleButton(value, onChanged));

        if (extendControls is not null)
        {
            var extra = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd
            };
            extra.AddThemeConstantOverride("separation", 6);
            parts.Controls.AddChild(extra);
            extendControls(extra);
        }

        parts.Card.TooltipText = description;
    }

    private void AddValueFeatureCard(FeatureCategory category, string title, string description, Control control)
    {
        var parts = CreateFeatureCard(category, title, description);
        parts.Controls.AddChild(control);
        parts.Card.TooltipText = description;
    }

    private void AddActionFeatureCard(FeatureCategory category, string title, string description, Control control, string buttonText, Action onAction)
    {
        var parts = CreateFeatureCard(category, title, description);
        parts.Controls.AddChild(control);
        parts.Controls.AddChild(CreateActionButton(buttonText, onAction));
        parts.Card.TooltipText = description;
    }

    private FeatureCardParts CreateFeatureCard(FeatureCategory category, string title, string description)
    {
        if (_rowsRoot is null)
        {
            throw new InvalidOperationException("Overlay rows root is not ready.");
        }

        var card = CreateCard();
        _rowsRoot.AddChild(card);

        var margin = BuildMargin(12, 12, 10, 10);
        card.AddChild(margin);

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 14);
        margin.AddChild(row);

        var content = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        content.AddThemeConstantOverride("separation", 4);
        row.AddChild(content);

        var titleLabel = new Label
        {
            Text = title,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        content.AddChild(titleLabel);

        var descriptionLabel = new Label
        {
            Text = description,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = new Color(1f, 1f, 1f, 0.62f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        content.AddChild(descriptionLabel);

        var controls = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd
        };
        controls.AddThemeConstantOverride("separation", 8);
        row.AddChild(controls);

        RegisterRow(card, category, $"{title} {description}");
        return new FeatureCardParts(controls, card);
    }

    private Button CreateToggleButton(bool initialValue, Action<bool> onChanged)
    {
        var button = new Button
        {
            ToggleMode = true,
            ButtonPressed = initialValue,
            CustomMinimumSize = new Vector2(108, 36),
            FocusMode = Control.FocusModeEnum.All
        };

        UpdateToggleVisual(button, initialValue);
        button.Toggled += pressed =>
        {
            UpdateToggleVisual(button, pressed);
            onChanged(pressed);
        };
        return button;
    }

    private Button CreateActionButton(string text, Action onPressed)
    {
        var button = CreateUtilityButton(text);
        button.CustomMinimumSize = new Vector2(108, 34);
        button.Pressed += onPressed;
        return button;
    }

    private Button CreateUtilityButton(string text)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(96, 34),
            FocusMode = Control.FocusModeEnum.All
        };
        ApplyButtonVisual(button, new Color(1f, 1f, 1f, 0.06f), new Color(1f, 1f, 1f, 0.10f), new Color(1f, 1f, 1f, 0.16f), new Color(1f, 1f, 1f, 0.22f), new Color(1f, 1f, 1f, 0.90f));
        return button;
    }

    private VBoxContainer CreateLabeledSpin(string labelText, int value, int min, int max, int step, Action<int> onChanged)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);

        var label = new Label
        {
            Text = labelText,
            Modulate = new Color(1f, 1f, 1f, 0.58f),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        box.AddChild(label);

        box.AddChild(CreateSpinBox(value, min, max, step, v => onChanged((int)v), decimals: false));
        return box;
    }

    private SpinBox CreateSpinBox(double value, double min, double max, double step, Action<double> onChanged, bool decimals)
    {
        var spin = new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = step,
            Value = value,
            Rounded = !decimals,
            CustomMinimumSize = new Vector2(140, 34)
        };
        spin.ValueChanged += newValue => onChanged(newValue);
        return spin;
    }

    private PanelContainer CreateCard()
    {
        var card = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        card.AddThemeStyleboxOverride("panel", BuildPanelStyle(new Color(1f, 1f, 1f, 0.035f), new Color(1f, 1f, 1f, 0.08f), 12));
        return card;
    }

    private MarginContainer BuildMargin(int left, int right, int top, int bottom)
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", left);
        margin.AddThemeConstantOverride("margin_right", right);
        margin.AddThemeConstantOverride("margin_top", top);
        margin.AddThemeConstantOverride("margin_bottom", bottom);
        return margin;
    }

    private StyleBoxFlat BuildPanelStyle(Color background, Color border, int radius)
    {
        var style = new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border
        };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(radius);
        return style;
    }

    private void UpdateToggleVisual(Button button, bool enabled)
    {
        button.Text = enabled ? Text("Enabled", "已开启") : Text("Disabled", "已关闭");
        ApplyButtonVisual(
            button,
            enabled ? new Color(0.20f, 0.42f, 0.33f, 0.92f) : new Color(1f, 1f, 1f, 0.05f),
            enabled ? new Color(0.27f, 0.60f, 0.45f, 1f) : new Color(1f, 1f, 1f, 0.08f),
            enabled ? new Color(0.24f, 0.50f, 0.39f, 0.98f) : new Color(1f, 1f, 1f, 0.11f),
            enabled ? new Color(0.17f, 0.36f, 0.28f, 0.98f) : new Color(1f, 1f, 1f, 0.14f),
            new Color(1f, 1f, 1f, 0.95f));
    }

    private void ApplyButtonVisual(Button button, Color normalBg, Color hoverBg, Color pressedBg, Color border, Color fontColor)
    {
        button.AddThemeStyleboxOverride("normal", BuildPanelStyle(normalBg, border, 10));
        button.AddThemeStyleboxOverride("hover", BuildPanelStyle(hoverBg, border, 10));
        button.AddThemeStyleboxOverride("pressed", BuildPanelStyle(pressedBg, border, 10));
        button.AddThemeStyleboxOverride("focus", BuildPanelStyle(pressedBg, border, 10));
        button.AddThemeStyleboxOverride("disabled", BuildPanelStyle(normalBg, border, 10));
        button.AddThemeColorOverride("font_color", fontColor);
        button.AddThemeColorOverride("font_hover_color", fontColor);
        button.AddThemeColorOverride("font_pressed_color", fontColor);
        button.AddThemeColorOverride("font_focus_color", fontColor);
        button.AddThemeColorOverride("font_disabled_color", new Color(fontColor.R, fontColor.G, fontColor.B, 0.55f));
    }

    private void RegisterRow(Control control, FeatureCategory category, string searchText)
    {
        _rows.Add(new RowBinding(control, category, searchText.ToLowerInvariant()));
    }

    private void ApplyFilters()
    {
        var search = _searchBox?.Text?.Trim().ToLowerInvariant() ?? string.Empty;
        var selectedCategory = _categoryFilter is not null ? _categoryFilter.GetSelectedId() : -1;

        foreach (var row in _rows)
        {
            var categoryMatches = selectedCategory < 0 || selectedCategory == (int)row.Category;
            var searchMatches = string.IsNullOrWhiteSpace(search) || row.SearchText.Contains(search, StringComparison.Ordinal);
            row.Control.Visible = categoryMatches && searchMatches;
        }
    }

    private string CategoryLabel(FeatureCategory category)
    {
        return category switch
        {
            FeatureCategory.Combat => Text("Combat", "战斗"),
            FeatureCategory.Economy => Text("Resources", "资源"),
            FeatureCategory.Rewards => Text("Rewards", "奖励"),
            FeatureCategory.Shop => Text("Shop", "商店"),
            FeatureCategory.Map => Text("Map", "地图"),
            FeatureCategory.System => Text("System", "系统"),
            _ => category.ToString()
        };
    }

    private string Text(string english, string chinese)
    {
        return (_runtime?.ResolvedLanguage ?? UiLanguage.English) == UiLanguage.ChineseSimplified ? chinese : english;
    }

    private readonly record struct RowBinding(Control Control, FeatureCategory Category, string SearchText);
    private readonly record struct FeatureCardParts(VBoxContainer Controls, PanelContainer Card);
}
