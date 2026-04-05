namespace Sts2Trainer.Shared;

public static class TrainerFeatures
{
    public static IReadOnlyList<FeatureDescriptor> All { get; } =
    [
        new(
            FeatureId.SafeMode,
            nameof(TrainerSettings.SafeMode),
            FeatureCategory.System,
            FeatureKind.Toggle,
            new("Safe Mode", "安全模式"),
            new("Limit key features to verified builds and single-player runs.", "仅在已验证版本和单人 run 中启用关键功能。"),
            GameplayAffecting: false),
        new(
            FeatureId.GodMode,
            nameof(TrainerSettings.GodMode),
            FeatureCategory.Combat,
            FeatureKind.Toggle,
            new("Unlimited HP", "无限生命"),
            new("Ignore incoming damage and restore HP outside combat.", "忽略受到的伤害，并在非战斗阶段自动回满。")),
        new(
            FeatureId.UnlimitedBlock,
            nameof(TrainerSettings.UnlimitedBlock),
            FeatureCategory.Combat,
            FeatureKind.Toggle,
            new("Unlimited Block", "无限护甲"),
            new("Maintain a minimum block floor while enabled.", "启用后维持护甲保底值。")),
        new(
            FeatureId.UnlimitedEnergy,
            nameof(TrainerSettings.UnlimitedEnergy),
            FeatureCategory.Combat,
            FeatureKind.Toggle,
            new("Unlimited Energy", "无限能量"),
            new("Keep energy available throughout the turn.", "持续维持本回合可用能量。")),
        new(
            FeatureId.UnlimitedStars,
            nameof(TrainerSettings.UnlimitedStars),
            FeatureCategory.Combat,
            FeatureKind.Toggle,
            new("Unlimited Stars", "无限星星"),
            new("Maintain the selected star reserve.", "维持设定的星星保底。")),
        new(
            FeatureId.FreezeEnemies,
            nameof(TrainerSettings.FreezeEnemies),
            FeatureCategory.Combat,
            FeatureKind.Toggle,
            new("Enemies Can't Move", "敌人无法行动"),
            new("Skip enemy turns.", "跳过敌方行动。")),
        new(
            FeatureId.DamageMultiplier,
            nameof(TrainerSettings.DamageMultiplier),
            FeatureCategory.Combat,
            FeatureKind.Decimal,
            new("Damage Multiplier", "伤害倍率"),
            new("Increase outgoing damage.", "放大造成的伤害。"),
            1m,
            25m,
            0.5m),
        new(
            FeatureId.DefenseMultiplier,
            nameof(TrainerSettings.DefenseMultiplier),
            FeatureCategory.Combat,
            FeatureKind.Decimal,
            new("Defense Multiplier", "减伤倍率"),
            new("Reduce incoming damage.", "降低受到的伤害。"),
            1m,
            25m,
            0.5m),
        new(
            FeatureId.GoldMultiplier,
            nameof(TrainerSettings.GoldMultiplier),
            FeatureCategory.Economy,
            FeatureKind.Decimal,
            new("Gold Multiplier", "金币倍率"),
            new("Increase future gold gains.", "提高之后获得的金币。"),
            1m,
            25m,
            0.5m),
        new(
            FeatureId.SetGold,
            nameof(TrainerSettings.GoldSetValue),
            FeatureCategory.Economy,
            FeatureKind.Integer,
            new("Edit Gold", "编辑金币"),
            new("Set current gold to the selected value.", "将当前金币调整到指定值。"),
            0m,
            999999m,
            1m),
        new(
            FeatureId.FreePurchase,
            nameof(TrainerSettings.FreePurchaseInShop),
            FeatureCategory.Shop,
            FeatureKind.Toggle,
            new("Free Purchase", "免费购买"),
            new("Purchases in the shop cost no gold.", "商店购买不消耗金币。")),
        new(
            FeatureId.MaxEnergyOverride,
            nameof(TrainerSettings.MaxEnergyTarget),
            FeatureCategory.Combat,
            FeatureKind.Integer,
            new("Fixed Max Energy", "固定最大能量"),
            new("Keep max energy at the selected value.", "将最大能量维持在指定值。"),
            1m,
            25m,
            1m),
        new(
            FeatureId.PotionSlotOverride,
            nameof(TrainerSettings.PotionSlotTarget),
            FeatureCategory.Economy,
            FeatureKind.Integer,
            new("Fixed Potion Slots", "固定药水栏位"),
            new("Keep potion slots at the selected value.", "将药水栏位维持在指定值。"),
            0m,
            12m,
            1m),
        new(
            FeatureId.AlwaysRewardPotion,
            nameof(TrainerSettings.AlwaysRewardPotion),
            FeatureCategory.Rewards,
            FeatureKind.Toggle,
            new("Potion Reward", "药水奖励"),
            new("Always include a potion in rewards.", "奖励始终包含药水。")),
        new(
            FeatureId.AlwaysUpgradeCardRewards,
            nameof(TrainerSettings.AlwaysUpgradeCardRewards),
            FeatureCategory.Rewards,
            FeatureKind.Toggle,
            new("Upgraded Rewards", "升级奖励"),
            new("Prefer upgraded card rewards.", "优先给出升级版卡牌奖励。")),
        new(
            FeatureId.MaxCardRewardRarity,
            nameof(TrainerSettings.MaxCardRewardRarity),
            FeatureCategory.Rewards,
            FeatureKind.Toggle,
            new("Top Rarity Rewards", "最高稀有度奖励"),
            new("Prefer the highest rarity available in the current pool.", "优先抽取当前奖励池中的最高稀有度。")),
        new(
            FeatureId.GameSpeed,
            nameof(TrainerSettings.GameSpeed),
            FeatureCategory.System,
            FeatureKind.Decimal,
            new("Game Speed", "游戏速度"),
            new("Adjust the pace of combat and animation.", "调整战斗与动画节奏。"),
            0.25m,
            5m,
            0.25m,
            GameplayAffecting: false),
        new(
            FeatureId.UnknownMapTreasure,
            nameof(TrainerSettings.UnknownMapPointsAlwaysGiveTreasure),
            FeatureCategory.Map,
            FeatureKind.Toggle,
            new("Treasure Nodes", "宝藏节点"),
            new("Prefer treasure rooms for unknown nodes.", "未知节点优先转为宝藏房。"))
    ];

    public static IReadOnlyList<TrainerPreset> Presets { get; } =
    [
        new(
            "balanced",
            new LocalizedText("Balanced", "平衡"),
            settings =>
            {
                settings.SafeMode = true;
                settings.GodMode = true;
                settings.UnlimitedEnergy = true;
                settings.UnlimitedStars = false;
                settings.UnlimitedBlock = false;
                settings.GoldMultiplier = 2m;
                settings.GameSpeed = 1.25f;
            }),
        new(
            "economy",
            new LocalizedText("Economy", "资源"),
            settings =>
            {
                settings.SafeMode = true;
                settings.GodMode = false;
                settings.UnlimitedEnergy = false;
                settings.GoldMultiplier = 10m;
                settings.FreePurchaseInShop = true;
                settings.AlwaysRewardPotion = true;
                settings.UnknownMapPointsAlwaysGiveTreasure = true;
            }),
        new(
            "overkill",
            new LocalizedText("Power", "压制"),
            settings =>
            {
                settings.SafeMode = false;
                settings.GodMode = true;
                settings.UnlimitedBlock = true;
                settings.UnlimitedEnergy = true;
                settings.UnlimitedStars = true;
                settings.FreezeEnemies = true;
                settings.DamageMultiplier = 10m;
                settings.DefenseMultiplier = 10m;
                settings.GoldMultiplier = 10m;
                settings.GameSpeed = 2f;
            })
    ];
}
