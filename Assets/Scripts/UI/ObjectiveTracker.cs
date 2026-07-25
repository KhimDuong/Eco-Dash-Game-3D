using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Mission tracker HUD widget (M7). Shows the active mission title plus a checklist of
/// sub-objectives that tick off as the player progresses. It LISTENS to
/// <see cref="GameManager"/> events (OnCoresChanged / OnAllCoresCollected /
/// OnLevelComplete) and never polls. Per-scene objective data is authored in the
/// inspector; a TMP row is generated per objective from a template. Sits alongside the
/// HP/trash HUD and replaces nothing.
/// </summary>
public class ObjectiveTracker : MonoBehaviour
{
    /// <summary>What ticks an objective off.</summary>
    public enum Goal
    {
        CollectCores,   // counter line (cores/keycards); done when all required collected
        AllCoresDone,   // done on OnAllCoresCollected (gate armed / core door open)
        LevelComplete,  // done on OnLevelComplete (boss down / level cleared)
        QuestHerb,      // M8 quest: collect 3 herbs
        QuestAntidote   // M8 quest: return to Ông Sáu and get antidote
    }

    [System.Serializable]
    public class Objective
    {
        [Tooltip("Label, e.g. \"Tìm Lõi Năng Lượng\". For CollectCores a (x/y) counter is appended.")]
        public string label;
        public Goal goal;
    }

    [Header("Content")]
    [SerializeField] string missionTitle = "Nhiệm vụ";
    [SerializeField] Objective[] objectives;

    [Header("UI")]
    [SerializeField] TMP_Text titleText;
    [Tooltip("Parent for the generated objective rows (give it a Vertical Layout Group).")]
    [SerializeField] RectTransform listParent;
    [Tooltip("A TMP_Text template (kept inactive) cloned once per objective.")]
    [SerializeField] TMP_Text rowTemplate;

    [Header("Look")]
    [SerializeField] Color pendingColor = new Color(0.92f, 0.92f, 0.92f);
    [SerializeField] Color doneColor = new Color(0.5f, 1f, 0.55f);
    [SerializeField] string pendingBullet = "•";
    [SerializeField] string doneBullet = "✓";

    readonly List<TMP_Text> rows = new();
    bool[] done;
    int cores, requiredCores;

    void Start()
    {
        if (titleText != null) titleText.text = missionTitle;
        BuildRows();

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnCoresChanged += HandleCores;
            gm.OnAllCoresCollected += HandleAllCores;
            gm.OnLevelComplete += HandleComplete;
            HandleCores(gm.CoresCollected, gm.RequiredCores);
        }

        QuestProgress.OnChanged += HandleQuestProgress;
        HandleQuestProgress();
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnCoresChanged -= HandleCores;
            gm.OnAllCoresCollected -= HandleAllCores;
            gm.OnLevelComplete -= HandleComplete;
        }

        QuestProgress.OnChanged -= HandleQuestProgress;
    }

    void BuildRows()
    {
        if (objectives == null || rowTemplate == null || listParent == null) return;
        rowTemplate.gameObject.SetActive(false);
        done = new bool[objectives.Length];
        for (int i = 0; i < objectives.Length; i++)
        {
            var row = Instantiate(rowTemplate, listParent);
            row.gameObject.SetActive(true);
            rows.Add(row);
            Refresh(i);
        }
    }

    void HandleCores(int collected, int required)
    {
        cores = collected; requiredCores = required;
        if (done == null) return;
        for (int i = 0; i < objectives.Length; i++)
            if (objectives[i].goal == Goal.CollectCores)
            {
                done[i] = required > 0 && collected >= required;
                Refresh(i);
            }
    }

    void HandleQuestProgress()
    {
        if (done == null) return;
        for (int i = 0; i < objectives.Length; i++)
        {
            if (objectives[i].goal == Goal.QuestHerb)
            {
                // Display the objective only when we start gathering herbs
                done[i] = QuestProgress.HerbCount >= 3 || QuestProgress.HasAntidote || QuestProgress.Stage == QuestStage.TiSaved;
                Refresh(i);
            }
            else if (objectives[i].goal == Goal.QuestAntidote)
            {
                done[i] = QuestProgress.HasAntidote || QuestProgress.Stage == QuestStage.TiSaved;
                Refresh(i);
            }
        }
    }

    void HandleAllCores() => MarkGoal(Goal.AllCoresDone);
    void HandleComplete() => MarkGoal(Goal.LevelComplete);

    void MarkGoal(Goal g)
    {
        if (done == null) return;
        for (int i = 0; i < objectives.Length; i++)
            if (objectives[i].goal == g) { done[i] = true; Refresh(i); }
    }

    void Refresh(int i)
    {
        if (i < 0 || i >= rows.Count) return;
        var o = objectives[i];
        
        // Hide quest objectives until they are actually unlocked/given
        bool isVisible = true;
        if (o.goal == Goal.QuestHerb) 
            isVisible = QuestProgress.Stage >= QuestStage.HerbsInProgress;
        else if (o.goal == Goal.QuestAntidote) 
            isVisible = QuestProgress.Stage >= QuestStage.HerbsReady;

        rows[i].gameObject.SetActive(isVisible);

        string label = o.label;
        if (o.goal == Goal.CollectCores) label = $"{o.label} ({cores}/{requiredCores})";
        else if (o.goal == Goal.QuestHerb) label = $"{o.label} ({Mathf.Min(QuestProgress.HerbCount, 3)}/3)";
        
        string bullet = done[i] ? doneBullet : pendingBullet;
        rows[i].text = $"{bullet} {label}";
        rows[i].color = done[i] ? doneColor : pendingColor;
        rows[i].fontStyle = done[i] ? FontStyles.Strikethrough : FontStyles.Normal;
    }
}
