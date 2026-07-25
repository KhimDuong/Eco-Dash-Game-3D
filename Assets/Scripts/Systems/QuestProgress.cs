using System;
using UnityEngine;

public enum QuestStage
{
    NotMet = 0,
    Offered = 1,
    HerbsInProgress = 2,
    HerbsReady = 3,
    AntidoteHeld = 4,
    TiSaved = 5
}

public static class QuestProgress
{
    private const string PREF_QUEST_STAGE = "EcoDash_QuestStage";
    private const string PREF_HERB_COUNT = "EcoDash_HerbCount";
    private const string PREF_HAS_ANTIDOTE = "EcoDash_HasAntidote";

    public static event Action OnChanged;

    public static QuestStage Stage
    {
        get => (QuestStage)PlayerPrefs.GetInt(PREF_QUEST_STAGE, (int)QuestStage.NotMet);
        private set
        {
            PlayerPrefs.SetInt(PREF_QUEST_STAGE, (int)value);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static int HerbCount
    {
        get => PlayerPrefs.GetInt(PREF_HERB_COUNT, 0);
        private set
        {
            PlayerPrefs.SetInt(PREF_HERB_COUNT, value);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static bool HasAntidote
    {
        get => PlayerPrefs.GetInt(PREF_HAS_ANTIDOTE, 0) == 1;
        private set
        {
            PlayerPrefs.SetInt(PREF_HAS_ANTIDOTE, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static void StartQuest()
    {
        if (Stage == QuestStage.NotMet)
        {
            Stage = QuestStage.Offered;
            HerbCount = 0;
            HasAntidote = false;
        }
    }

    public static void AcceptQuestAndStartGathering()
    {
        if (Stage == QuestStage.Offered)
        {
            Stage = QuestStage.HerbsInProgress;
        }
    }

    public static void AddHerb()
    {
        if (Stage == QuestStage.HerbsInProgress)
        {
            HerbCount++;
            if (HerbCount >= 3)
            {
                Stage = QuestStage.HerbsReady;
            }
            else
            {
                OnChanged?.Invoke();
            }
        }
    }

    public static void GrantAntidote()
    {
        if (Stage == QuestStage.HerbsReady)
        {
            HasAntidote = true;
            Stage = QuestStage.AntidoteHeld;
        }
    }

    public static void ConsumeAntidoteAndSaveTi()
    {
        if (Stage == QuestStage.AntidoteHeld && HasAntidote)
        {
            HasAntidote = false;
            Stage = QuestStage.TiSaved;
        }
    }

    public static void ResetQuest()
    {
        PlayerPrefs.DeleteKey(PREF_QUEST_STAGE);
        PlayerPrefs.DeleteKey(PREF_HERB_COUNT);
        PlayerPrefs.DeleteKey(PREF_HAS_ANTIDOTE);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }
}
