using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A reusable settings overlay (volume + mute) shown from the Main Menu's
/// "Cài đặt" button and from the in-level pause menu. Reads / writes the shared
/// <see cref="GameSettings"/> store, so changes persist across scenes and
/// restarts. References are wired at build time; the controls add their own
/// listeners at runtime so no persistent-listener wiring is needed.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Tooltip("Root object toggled on/off (Dim + Window). Starts hidden.")]
    [SerializeField] GameObject panel;
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Toggle muteToggle;
    [SerializeField] TMP_Text masterValue;
    [SerializeField] TMP_Text musicValue;
    [SerializeField] Button closeButton;

    public bool IsOpen => panel != null && panel.activeSelf;

    bool syncing; // guards slider callbacks while we push values in

    void Awake()
    {
        if (panel != null) panel.SetActive(false);

        // Match the controls to the store BEFORE wiring the callbacks, not after. The widgets
        // are authored with whatever values the prefab happened to be saved at — in this
        // project a mute toggle that is ON and a music slider at 100%, neither of which is
        // what GameSettings defaults to — and a Toggle or Slider that reports its own value
        // through OnMute/OnMusic writes that value straight into the store and saves it to
        // PlayerPrefs. That is a one-way door: the player's settings are now the prefab's,
        // the game is muted, and nothing on screen explains why. Syncing first means the
        // controls can only ever echo the store, never define it.
        SyncFromSettings();

        if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMaster);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusic);
        if (muteToggle != null) muteToggle.onValueChanged.AddListener(OnMute);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        SyncFromSettings();
        if (panel != null) panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen) Close(); else Open();
    }

    void SyncFromSettings()
    {
        syncing = true;
        if (masterSlider != null) masterSlider.value = GameSettings.MasterVolume;
        if (musicSlider != null) musicSlider.value = GameSettings.MusicVolume;
        if (muteToggle != null) muteToggle.isOn = GameSettings.Muted;
        syncing = false;
        RefreshLabels();
    }

    void OnMaster(float v) { if (!syncing) { GameSettings.MasterVolume = v; RefreshLabels(); } }
    void OnMusic(float v) { if (!syncing) { GameSettings.MusicVolume = v; RefreshLabels(); } }
    void OnMute(bool m) { if (!syncing) GameSettings.Muted = m; }

    void RefreshLabels()
    {
        if (masterValue != null) masterValue.text = $"{Mathf.RoundToInt(GameSettings.MasterVolume * 100f)}%";
        if (musicValue != null) musicValue.text = $"{Mathf.RoundToInt(GameSettings.MusicVolume * 100f)}%";
    }
}
