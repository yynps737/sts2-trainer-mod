using System.Text.Json.Serialization;

namespace Sts2Trainer.Shared;

public sealed class TrainerSettings
{
    public UiLanguage Language { get; set; } = UiLanguage.Auto;

    public string TogglePanelKey { get; set; } = "F9";

    public bool ShowOverlayByDefault { get; set; }

    public bool SinglePlayerOnly { get; set; } = true;

    public bool SafeMode { get; set; } = true;

    public bool StructuredLogEnabled { get; set; } = true;

    public bool GodMode { get; set; }

    public bool UnlimitedBlock { get; set; }

    public int BlockFloor { get; set; } = 999;

    public bool UnlimitedEnergy { get; set; }

    public bool UnlimitedStars { get; set; }

    public int TargetStars { get; set; } = 99;

    public bool FreezeEnemies { get; set; }

    public decimal GoldMultiplier { get; set; } = 1m;

    public int GoldSetValue { get; set; } = 999;

    public bool FreePurchaseInShop { get; set; }

    public bool EnforceMaxEnergy { get; set; }

    public int MaxEnergyTarget { get; set; } = 6;

    public bool EnforcePotionSlots { get; set; }

    public int PotionSlotTarget { get; set; } = 6;

    public bool AlwaysRewardPotion { get; set; }

    public bool AlwaysUpgradeCardRewards { get; set; }

    public bool MaxCardRewardRarity { get; set; }

    public decimal DamageMultiplier { get; set; } = 1m;

    public decimal DefenseMultiplier { get; set; } = 1m;

    public float GameSpeed { get; set; } = 1f;

    public bool UnknownMapPointsAlwaysGiveTreasure { get; set; }

    [JsonIgnore]
    public bool HasEconomicOverrides => GoldMultiplier > 1m || FreePurchaseInShop;

    public TrainerSettings Clone()
    {
        return new TrainerSettings
        {
            Language = Language,
            TogglePanelKey = TogglePanelKey,
            ShowOverlayByDefault = ShowOverlayByDefault,
            SinglePlayerOnly = SinglePlayerOnly,
            SafeMode = SafeMode,
            StructuredLogEnabled = StructuredLogEnabled,
            GodMode = GodMode,
            UnlimitedBlock = UnlimitedBlock,
            BlockFloor = BlockFloor,
            UnlimitedEnergy = UnlimitedEnergy,
            UnlimitedStars = UnlimitedStars,
            TargetStars = TargetStars,
            FreezeEnemies = FreezeEnemies,
            GoldMultiplier = GoldMultiplier,
            GoldSetValue = GoldSetValue,
            FreePurchaseInShop = FreePurchaseInShop,
            EnforceMaxEnergy = EnforceMaxEnergy,
            MaxEnergyTarget = MaxEnergyTarget,
            EnforcePotionSlots = EnforcePotionSlots,
            PotionSlotTarget = PotionSlotTarget,
            AlwaysRewardPotion = AlwaysRewardPotion,
            AlwaysUpgradeCardRewards = AlwaysUpgradeCardRewards,
            MaxCardRewardRarity = MaxCardRewardRarity,
            DamageMultiplier = DamageMultiplier,
            DefenseMultiplier = DefenseMultiplier,
            GameSpeed = GameSpeed,
            UnknownMapPointsAlwaysGiveTreasure = UnknownMapPointsAlwaysGiveTreasure
        };
    }
}

public sealed record TrainerPreset(string Id, LocalizedText Name, Action<TrainerSettings> Apply);
