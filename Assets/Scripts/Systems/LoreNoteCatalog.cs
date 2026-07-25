using System.Collections.Generic;

/// <summary>
/// Text for the 8 hidden lore notes (game-design §4.7.5) that flesh out Black
/// Smoke's plot, the village's past and Greenie's origin (M9, K6). A
/// <see cref="LoreNote"/> world pickup references an id here; the codex "Mẩu Nhật Ký"
/// tab shows the full text of found notes. 4 valley (stage 1) + 4 factory (stage 2);
/// the factory notes feed Cô Lan's quest. All text is Vietnamese.
/// </summary>
public static class LoreNoteCatalog
{
    public class Note { public string id; public int stageId; public string title; public string body; }

    static Dictionary<string, Note> byId;
    static List<Note> ordered;

    public static Note Get(string id)
    {
        EnsureLoaded();
        return byId.TryGetValue(id, out var n) ? n : null;
    }

    public static IReadOnlyList<Note> All { get { EnsureLoaded(); return ordered; } }
    public static int Count { get { EnsureLoaded(); return ordered.Count; } }

    static void EnsureLoaded()
    {
        if (byId != null) return;
        byId = new Dictionary<string, Note>();
        ordered = new List<Note>();

        // --- Stage 1: the valley ---
        Add("ln_valley_1", Codex.StageValley, "Nhật Ký Bà Tư",
            "Ngày xưa thung lũng này xanh mướt, lúa trĩu bông. Trẻ con tắm suối, cá lội đầy ao...");
        Add("ln_valley_2", Codex.StageValley, "Mảnh Giấy Cũ",
            "Họ đến với những chiếc xe tải đen. Nói là 'phát triển'. Rồi khói bắt đầu phủ kín trời.");
        Add("ln_valley_3", Codex.StageValley, "Bản Vẽ Greenie",
            "Dự án G.R.E.E.N.I.E: một robot dọn dẹp chạy bằng năng lượng sạch, gieo mầm sự sống nơi nó đi qua.");
        Add("ln_valley_4", Codex.StageValley, "Lời Nhắn Của Ông Sáu",
            "Tí bị nhiễm độc khi chơi gần ống xả. Ta cần thảo dược... làm ơn, ai đó giúp với.");

        // --- Stage 2: the factory ---
        Add("ln_factory_1", Codex.StageFactory, "Báo Cáo Nội Bộ",
            "Chỉ tiêu quý này: tăng 200% sản lượng. Chất thải cứ xả thẳng ra thung lũng — rẻ hơn xử lý.");
        Add("ln_factory_2", Codex.StageFactory, "Email Giám Đốc",
            "Giám đốc Khói Đen: 'Khi thung lũng chết hẳn, dân sẽ bỏ đi, đất sẽ là của ta. Cứ tiếp tục.'");
        Add("ln_factory_3", Codex.StageFactory, "Nhật Ký Công Nhân",
            "Mình từng làm ở đây. Khi biết họ làm gì với thung lũng, mình đã bỏ trốn. — Lan");
        Add("ln_factory_4", Codex.StageFactory, "Tài Liệu Mật",
            "Lò phản ứng khói lõi đang nạp đầy. Nếu nó kích hoạt, cả vùng sẽ chìm trong khói vĩnh viễn.");
    }

    static void Add(string id, int stageId, string title, string body)
    {
        var n = new Note { id = id, stageId = stageId, title = title, body = body };
        byId[id] = n;
        ordered.Add(n);
    }
}
