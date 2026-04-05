namespace Sts2Trainer.Shared;

public enum UiLanguage
{
    Auto = 0,
    English = 1,
    ChineseSimplified = 2
}

public enum FeatureCategory
{
    Combat = 0,
    Economy = 1,
    Rewards = 2,
    Shop = 3,
    Map = 4,
    System = 5
}

public enum FeatureKind
{
    Toggle = 0,
    Integer = 1,
    Decimal = 2,
    Action = 3
}

public enum FeatureId
{
    ToggleOverlay = 0,
    SafeMode = 1,
    GodMode = 2,
    UnlimitedBlock = 3,
    UnlimitedEnergy = 4,
    UnlimitedStars = 5,
    FreezeEnemies = 6,
    GoldMultiplier = 7,
    SetGold = 8,
    FreePurchase = 9,
    MaxEnergyOverride = 10,
    PotionSlotOverride = 11,
    AlwaysRewardPotion = 12,
    AlwaysUpgradeCardRewards = 13,
    MaxCardRewardRarity = 14,
    DamageMultiplier = 15,
    DefenseMultiplier = 16,
    GameSpeed = 17,
    UnknownMapTreasure = 18
}

public sealed record LocalizedText(string English, string ChineseSimplified)
{
    public string Resolve(UiLanguage language)
    {
        return language switch
        {
            UiLanguage.English => English,
            UiLanguage.ChineseSimplified => ChineseSimplified,
            _ => English
        };
    }
}

public sealed record FeatureDescriptor(
    FeatureId Id,
    string SettingKey,
    FeatureCategory Category,
    FeatureKind Kind,
    LocalizedText Title,
    LocalizedText Description,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    decimal? Step = null,
    bool ExpertOnly = false,
    bool GameplayAffecting = true);

public interface IGameStateSnapshot
{
    bool HasRun { get; }

    bool IsSinglePlayerRun { get; }

    bool IsInCombat { get; }

    int? CurrentRound { get; }

    int? PlayerCurrentHp { get; }

    int? PlayerMaxHp { get; }

    int? PlayerGold { get; }
}

public interface ITrainerAction
{
    string Id { get; }

    LocalizedText DisplayName { get; }

    bool CanExecute(IGameStateSnapshot snapshot);

    void Execute();
}

public interface ICompatibilityGuard
{
    string Id { get; }

    bool IsCompatible(string? detectedVersion, string? detectedCommit, out string message);
}

public interface IHookModule
{
    string Id { get; }

    int Order { get; }

    void Apply(object harmony);
}
