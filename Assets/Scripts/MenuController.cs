using UnityEngine;
using UnityEngine.SceneManagement; // Thư viện bắt buộc để quản lý chuyển màn

public class MenuController : MonoBehaviour
{
    [Tooltip("Optional settings overlay opened by the 'Cài đặt' button.")]
    [SerializeField] SettingsPanel settingsPanel;

    [Tooltip("Scene loaded by 'Bắt Đầu' — the M7 intro slides, which then load Level 1.")]
    [SerializeField] string startScene = "Intro_Story";

    // "Chơi Mới" (Start Fresh) — wipes ALL progress and starts a brand-new game.
    public void PlayGame()
    {
        SaveSystem.ResetNewGame(); // true reset: run + shop upgrades + scene progress

        // M7: start at the intro story slides (loaded by name so build-index order is
        // irrelevant); Intro_Story then loads Level1_BarrenFarm.
        SceneManager.LoadScene(startScene);
    }

    // "Tiếp Tục" (Continue) — resume the last gameplay scene with progress intact.
    // The hub (Trạm Tái Chế) is now reachable only via the in-game portals, not the
    // menu, so this replaces the old "Cửa Hàng" button.
    public void ContinueGame()
    {
        SceneManager.LoadScene(SceneProgress.HasSave ? SceneProgress.LastScene : startScene);
    }

    // Mở / đóng bảng cài đặt (âm lượng) — nút "Cài đặt"
    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.Open();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.Close();
    }

    // Hàm này dành cho nút Exit (nếu có)
    public void QuitGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit();
#if UNITY_EDITOR
        // In the editor, Application.Quit() does nothing — stop play mode instead
        // so the Quit button is testable.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
