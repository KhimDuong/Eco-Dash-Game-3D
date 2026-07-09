🎮 TÊN GAME: ECO-DASH: BIỆT ĐỘI GIẢI CỨU XANH
Thể loại: 2D Top-down Adventure / Action RPG (Phiêu lưu hành động góc nhìn từ trên xuống giống Stardew Valley nhưng kết hợp yếu tố chiến đấu rảnh tay/vượt ải).
Phong cách đồ họa: Pixel Art tươi sáng, mộc mạc đặc trưng của dòng game nông trại, có sự tương phản rõ rệt giữa các mảng đất xám xịt do ô nhiễm và các bãi cỏ xanh ngát sau khi được giải cứu.
1. Cốt Truyện Game (Storyline)
Bối cảnh diễn ra tại một thị trấn giả tưởng mang tên Greenvale 2026. Nơi đây từng là một thung lũng xanh tươi thanh bình, nhưng hiện đang bị tàn phá nặng nề bởi nhà máy hóa chất hắc ám độc quyền "Black Smoke". Chúng xả thải bừa bãi, biến đất đai thành sa mạc cằn cỗi và tạo ra các "Quái vật Khói bụi", "Quái vật Rác thải nhựa" nhằm ép người dân phải di tản để chúng chiếm đất trục lợi.
Người chơi sẽ vào vai Greenie – một chú robot dọn rác nhỏ bé mang trong mình lõi năng lượng Hạt mầm vĩnh cửu. Greenie phải tự mình di chuyển qua các vùng đất bẩn, dọn sạch rác thải, chiến đấu với tay sai của tập đoàn độc ác để hồi sinh nguồn nước và cây trồng cho thung lũng.
2. Thiết Kế 2 Màn Chơi Tăng Dần Độ Khó (Levels)
Vì là góc nhìn Top-down di chuyển 4 hướng ($A-W-S-D$), cơ chế vượt ải sẽ tập trung vào việc né bẫy trên mặt đất và tìm đường đi:
🌆 Level 1: "Nông Trại Hoang Hóa" (The Barren Farm)
Bối cảnh: Khu vực ngoại ô thung lũng với nền đất xám xịt, cây cối héo úa, các ao hồ đổi sang màu tím độc hại.
Thách thức & Cạm bẫy: Các bãi bùn lầy hóa chất màu tím (đi vào sẽ bị giảm 50% tốc độ di chuyển), các đống đổ nát rỉ sét cản đường buộc người chơi phải đi đường vòng.
Kẻ địch AI đơn giản: Quái Rác Nhựa (Plastic Slime) – Những cục nhớt nhựa di chuyển tuần tra ngẫu nhiên quanh các đống rác (sử dụng thuật toán chọn vị trí ngẫu nhiên ngắt quãng). Nếu chạm vào người chơi sẽ gây sát thương.
Mục tiêu: Thu thập đủ 3 "Lõi Năng Lượng Sạch" ẩn giấu trong các rương gỗ cũ để thanh lọc nguồn nước, mở cổng dịch chuyển sang màn sau.
🏙️ Level 2: "Mê Cung Nhà Máy" (The Factory Maze)
Bối cảnh: Tiến sâu vào bên trong khuôn viên nhà máy cốt lõi của tập đoàn Black Smoke với các nền gạch sắt và hệ thống đường ống chằng chịt.
Thách thức & Cạm bẫy: Các tia laser độc hại quét qua lại theo chu kỳ thời gian, các bẫy hố ga mở ra đóng lại liên tục.
Kẻ địch AI thông minh (Advanced): Fly-Bot Ô Nhiễm – Drone cơ khí bay lơ lửng. Khi Greenie bước vào tầm nhìn (sử dụng hàm Physics2D.OverlapCircle hoặc Vector2.Distance), Drone sẽ tự động đuổi theo mục tiêu và bắn các quả cầu khói bụi độc hại về phía người chơi.
Trùm cuối (Boss Battle): Cỗ Máy Hủy Diệt "Mega-Smog" ở trung tâm nhà máy. Boss đứng yên ở giữa phòng nhưng liên tục xả đạn tỏa ra 8 hướng và tạo các vùng khí độc ngẫu nhiên trên sàn, đòi hỏi người chơi phải di chuyển linh hoạt để né đòn.
3. Cơ Chế Gameplay & Tính Năng Lấy Điểm Tuyệt Đối
⌨️ Cơ chế điều khiển & Giao diện (Basic)
Nút bấm: W-A-S-D để di chuyển nhân vật đi lên - trái - xuống - phải (Animator sẽ thay đổi Animation tương ứng với 4 hướng giống Stardew Valley). Phím J dùng để bắn đạn Hạt mầm giải cứu môi trường.
Giao diện UI : Hiển thị thanh máu (HP), số lượng rác thải đã gom được ở góc trên màn hình.
🎒 Vật phẩm hỗ trợ (Items)
Bình Nước Suối Tinh Khiết: Hồi phục ngay lập tức 2 vạch máu.
Nước Tăng Lực Mầm Xanh: Tăng 50% tốc độ chạy của robot trong vòng 8 giây.
🏪 Khu vực mua sắm (Shopping Area - Advanced)
Thiết kế một Scene hoặc một khu vực an toàn mang tên "Trạm Tái Chế Của Ông Bear".
Khi người chơi tích lũy đủ số chai lọ/rác nhặt được từ việc dọn dẹp hoặc đánh quái, họ có thể tương tác với NPC để mua nâng cấp vĩnh viễn: Tăng thanh máu tối đa, Tăng tốc độ di chuyển cơ bản, hoặc Nâng cấp súng bắn ra đạn mầm cây lan tỏa rộng hơn.
🤖 Chế độ Tự động chơi (Autoplay - Advanced) — ❌ ĐÃ LƯỢC BỎ / CUT (2026-06-09)
> **Quyết định:** Tính năng Autoplay sẽ KHÔNG được làm cho bản nộp này. Giữ lại mô tả bên dưới chỉ để tham khảo.
> **Decision:** Autoplay will NOT be implemented for this submission. Text below kept for reference only.

Tích hợp một nút "Auto-Clean" trên màn hình UI. Khi bấm vào, nhân vật tự động chuyển sang trạng thái tự tìm đường (Pathfinding bằng thuật toán đơn giản hoặc tìm mục tiêu là Lõi Năng Lượng gần nhất).
Hệ thống tự động dùng tia Raycast2D quét xung quanh, nếu phát hiện có quái vật cản đường thì robot tự động quay mặt về phía quái và kích hoạt lệnh bắn đạn liên tục.
💡 Gợi ý cho bạn và Nhật khi làm Asset kiểu này:
Vì đổi sang phong cách Stardew Valley, khi hai bạn vẽ bản đồ bằng công cụ Tilemap trong Unity, bạn chỉ cần tạo một hệ thống Grid phẳng, xếp các ô gạch mặt đất (Grass/Dirt) và đặt các vật cản (Vách đá, Đường ống) có gắn linh kiện Tilemap Collider 2D là nhân vật tự động va chạm cực kỳ chuẩn xác, không sợ lỗi rơi xuống vực như các game đi cảnh màn hình ngang.
Kịch bản được tinh chỉnh như thế này đã chuẩn phom Top-down chưa bạn? Nếu cần, mình có thể chỉ bạn cách thiết lập hệ thống chuyển đổi Animation 4 hướng (Up, Down, Left, Right) cho nhân vật trong Unity nhé!

---

4. (MỞ RỘNG v2 — 2026-06-10) TUYẾN TRUYỆN & HỆ THỐNG NHIỆM VỤ
Toàn bộ cơ chế ở các phần trên (2 màn, vật phẩm, cửa hàng nâng cấp, boss) đã hoàn thành. Phần mở rộng này KHÔNG thêm màn mới — nó bổ sung một "lớp kể chuyện + nhiệm vụ" để 2 màn hiện có trở thành một chiến dịch có cốt truyện, thay vì các màn vượt ải rời rạc. Hiện tại người chơi gần như không hề thấy cốt truyện trong lúc chơi; phần này đưa câu chuyện ra trước mặt người chơi.

🎬 Mạch truyện (các hồi)
- Mở đầu (Intro): Greenvale từng xanh tươi. Tập đoàn "Black Smoke" xả thải, thả quái vật, xua đuổi dân làng. Giữa đống đổ nát, một lõi Hạt Mầm vĩnh cửu kích hoạt chú robot Greenie.
- Hồi 1 — Nông Trại Hoang Hóa (Level 1): Greenie gặp Bà Tư — người nông dân già cố bám trụ, đang trốn trong nông trại. Bà nhờ Greenie tìm 3 Lõi Năng Lượng Sạch để thanh lọc giếng làng. Hoàn thành → đất hồi sinh, cổng dịch chuyển mở. Bà Tư cảnh báo: nguồn độc chảy ra từ nhà máy.
- Khúc nghỉ — Trạm Tái Chế Của Ông Bear: Ông Bear, thợ máy/tái chế già, dùng rác Greenie thu được để nâng cấp; cộc cằn nhưng tốt bụng.
- Hồi 2 — Mê Cung Nhà Máy (Level 2): Một công nhân bỏ trốn cảnh báo về laser, drone và cỗ máy Mega-Smog. Greenie tìm 2 thẻ từ, mở cửa khu trung tâm, hạ gục boss.
- Kết (Ending): Cỗ máy nổ tung, nước trong trở lại, cây cối đâm chồi, dân làng quay về. Greenie ở lại canh giữ Greenvale xanh.

👥 Nhân vật
- Greenie — robot dọn dẹp, nhân vật người chơi.
- Bà Tư — nông dân già ở Hồi 1; giao nhiệm vụ thanh lọc giếng và hé lộ về nhà máy.
- Ông Bear — chủ trạm tái chế; nâng cấp + vài câu thoại tính cách.
- Công nhân bỏ trốn — NPC cảnh báo đầu Hồi 2 (tùy chọn).
- Giám đốc Black Smoke — phản diện giấu mặt; câu khiêu khích trước trận boss (tùy chọn, qua bảng chữ).

🧭 Hệ thống nhiệm vụ (Objective tracker)
Một khung mục tiêu trên màn hình: tiêu đề nhiệm vụ hiện tại + danh sách mục tiêu phụ có dấu tích, ví dụ "Tìm Lõi Năng Lượng (1/3)", "Mở cổng dịch chuyển". Cập nhật tự động từ các sự kiện sẵn có của GameManager (cores / keycards / boss), không cần kiểm tra liên tục.

💬 Thoại & cắt cảnh
- Hộp thoại NPC: nhấn E để nói chuyện; bảng hiện tên người nói + lời thoại, nhấn E/Space để qua dòng; tạm dừng di chuyển/bắn khi đang thoại.
- Bảng kể chuyện (story slides): chuỗi slide (chữ + ảnh tùy chọn) cho phần Mở đầu và Kết; nhấn để tiếp tục rồi vào scene kế.

🗺️ Luồng scene mới
MainMenu → Intro (slides) → Level 1 → Level 2 → Boss → Ending (slides) → Menu/Credits. (Trạm Ông Bear vẫn mở từ Menu.)

📝 Thoại mẫu (gợi ý, có thể chỉnh)
- Bà Tư: "Ôi, một con robot biết dọn rác sao? Tốt quá... Giếng làng nhiễm độc hết rồi. Tìm cho ta 3 Lõi Năng Lượng Sạch giấu trong mấy cái rương cũ nhé cháu!"
- Bà Tư (xong màn): "Nước trong lại rồi! Nhưng gốc rễ nằm ở cái nhà máy Black Smoke kia kìa, cháu ơi..."
- Ông Bear: "Hừm, tha rác tới cho ta hả nhóc? Đưa đây, ta 'độ' lại cho. Đồ tái chế cả đấy, đừng chê!"
- Công nhân bỏ trốn: "Quay lại đi! Trong đó toàn laser với drone... và 'nó' — Mega-Smog!"

---

5. (MỞ RỘNG v3 — 2026-06-12) CHUỖI NHIỆM VỤ TÌM NGƯỜI & VẬT PHẨM CỨU NGƯỜI
Phần mở rộng này nối tiếp lớp kể chuyện M7 ở mục 4. Nó KHÔNG thêm màn mới — nó thêm một "chuỗi nhiệm vụ" hoàn chỉnh kiểu Stardew Valley lên 2 màn sẵn có, gồm 2 cơ chế mới mà người chơi yêu cầu:
(1) Đi tìm một NPC cụ thể → nói chuyện để nhận mô tả & NHẬN nhiệm vụ → hoàn thành → QUAY LẠI gặp NPC để xác nhận → nhận một VẬT PHẨM cụ thể (cần dùng về sau).
(2) Dùng chính vật phẩm đó để CỨU một NPC trong game.

👤 Nhân vật (mới & mở rộng)
- Ông Sáu — ông lão bốc thuốc (thầy lang) trốn ở một GÓC KHUẤT của Nông Trại Hoang Hóa (Level 1). Khác với Bà Tư (tự bắt chuyện khi vào màn), người chơi phải tự đi TÌM Ông Sáu. Ông là người giao nhiệm vụ phụ và là người trao vật phẩm.
- Tí — cháu trai Ông Sáu, một thanh niên liều lĩnh đã chạy vào nhà máy. ĐÂY CHÍNH LÀ "công nhân bỏ trốn" ở M7, nay được đặt tên và có vai trò rõ ràng: ở Level 2, Tí bị khói độc đánh gục, nằm BẤT TỈNH — là NPC cần được cứu.

🧩 Vật phẩm mới
- Lá Thuốc (×3) — nhánh thảo dược mọc rải rác trong Nông Trại (giấu sau bãi bùn độc / gần quái), thu thập cho nhiệm vụ. Khác hẳn Lõi Năng Lượng và Rác — đây là vật phẩm nhiệm vụ riêng.
- Thuốc Giải Mầm Xanh — vật phẩm nhiệm vụ do Ông Sáu bào chế. Giữ trong túi đồ của Greenie, MANG XUYÊN MÀN sang Level 2, dùng (nhấn E) để cứu Tí. Chỉ có một liều.

🧭 Các bước trong chuỗi nhiệm vụ (đúng vòng lặp người chơi yêu cầu)
1. TÌM GẶP: Đi tìm Ông Sáu ở góc khuất Nông Trại. Nhấn E → ông kể: cháu trai Tí đã dại dột chạy vào nhà máy Black Smoke, chắc chắn đã trúng khói độc; ông quá già yếu không đi nổi.
2. NHẬN NHIỆM VỤ: ông xin Greenie hái 3 Lá Thuốc để bào chế thuốc giải. Khung mục tiêu hiện: "Hái Lá Thuốc (0/3)". 3 Lá Thuốc xuất hiện/được tính sau khi nhận nhiệm vụ.
3. HOÀN THÀNH: thu đủ 3/3 → mục tiêu đổi thành "Mang lá thuốc về cho Ông Sáu".
4. QUAY LẠI XÁC NHẬN: gặp lại Ông Sáu khi đã có 3/3 → ông sắc thuốc và TRAO Thuốc Giải Mầm Xanh. Mục tiêu mới: "Tìm và cứu Tí trong nhà máy".
5. DÙNG VẬT PHẨM CỨU NGƯỜI: sang Level 2, tìm thấy Tí nằm bất tỉnh ở lối vào nhà máy. Nhấn E khi ĐANG GIỮ Thuốc Giải → Greenie cho Tí uống → Tí tỉnh lại (thuốc bị tiêu hao).
   - Nếu CHƯA có thuốc giải: nhấn E chỉ hiện "Cậu ấy bất tỉnh vì khói độc... mình cần thuốc giải!" — chưa cứu được.
6. PHẦN THƯỞNG & TIẾP NỐI: Tí tỉnh dậy → cảm ơn Greenie, cảnh báo về laser/drone/Mega-Smog (lời cảnh báo của "công nhân bỏ trốn" ở M7 nay CHUYỂN sang đây), và trao một Thẻ Từ (hoặc chỉ chỗ giấu) giúp Greenie tiến vào khu trung tâm.

💚 Ý nghĩa, phần kết & cân bằng
- "Cần dùng về sau": vật phẩm lấy ở Level 1 nhưng chỉ dùng được ở Level 2 → buộc người chơi mang đồ xuyên màn, đúng chất RPG.
- Đây là nhiệm vụ phụ KHUYẾN KHÍCH MẠNH, KHÔNG chặn đường chính: nếu bỏ qua, người chơi vẫn phá đảo được (2 thẻ từ vẫn tự tìm như cũ) nhưng mất phần thưởng của Tí và mất cảnh đoàn tụ ở phần Kết — nên không bao giờ bị kẹt cứng (soft-lock).
- Phần Kết: nếu đã cứu Tí → thêm một slide đoàn tụ ấm áp giữa Tí và Ông Sáu khi dân làng trở về.

📝 Thoại mẫu (gợi ý, có thể chỉnh)
- Ông Sáu (gặp lần đầu): "Robot à? Cứu lão với... thằng Tí, cháu lão, dại dột chạy vào cái nhà máy quỷ đó rồi! Khói độc trong ấy... nó không trụ nổi đâu. Lão bào chế được thuốc giải, nhưng cần 3 nhánh Lá Thuốc ngoài đồng — chân cẳng lão yếu quá, đi không nổi nữa..."
- Ông Sáu (chưa đủ lá): "Mới được mấy nhánh thôi cháu... Lão cần đủ 3 nhánh Lá Thuốc mới sắc được thuốc."
- Ông Sáu (đủ 3 lá): "Đủ cả rồi! Để lão sắc thuốc... Đây, Thuốc Giải Mầm Xanh. Tìm thằng Tí, cho nó uống NGAY khi thấy nó, nghe chưa!"
- Greenie (gặp Tí bất tỉnh, chưa có thuốc): "Cậu ấy bất tỉnh vì khói độc... mình cần thuốc giải!"
- Tí (được cứu): "Kh... khụ! Mình... còn sống à? Cảm ơn cậu, robot! Cẩn thận — trong kia toàn laser với drone, và 'nó' — Mega-Smog! Cầm lấy cái thẻ từ này, mình giấu được một cái..."
- Ông Sáu & Tí (phần Kết, đoàn tụ): "— Thằng Tí! Cha bố anh, làm ông già này lo muốn chết!" / "— Con xin lỗi ông... Nhờ có Greenie cả đấy ạ."

---

6. (MỞ RỘNG v4 — 2026-06-15) LỚP NỘI DUNG "30 PHÚT" KIỂU STARDEW (vẫn GIỮ 2 MÀN)
Mục tiêu (Khiêm phụ trách): kéo dài một lượt chơi từ dưới 10 phút (đang bị "speedrun") lên KHOẢNG 30 PHÚT bằng cách làm sâu hệ thống — VẪN ĐÚNG 2 MÀN, không thêm màn mới. Học theo các vòng lặp của Stardew Valley (túi đồ, chế tạo, sổ tay sưu tầm, đi lại qua lại giữa các khu). NHỊP CHƠI thuần nội dung — KHÔNG có đồng hồ ngày/thể lực. Các hệ thống đã chốt: Trạm trung tâm + cổng 2 chiều, Túi đồ ô lưới xếp chồng, Bàn chế tạo, Nhiệm vụ phụ (DO NPC GIAO), Sổ tay sưu tầm + vòng lặp dọn sạch. (Đã cân nhắc và BỎ: thanh thiện cảm NPC, và bảng nhiệm vụ treo bounty lặp lại — nhiệm vụ phụ chỉ do NPC giao trực tiếp.)

🏠 6.1 Trạm Trung Tâm & đi lại 2 chiều (thay cổng 1 chiều)
- Trạm Tái Chế Của Ông Bear trở thành TRẠM TRUNG TÂM nối hai màn — chính nhờ đi lại qua lại mà lượt chơi mới dài ra. Trạm có thêm: Cổng Nexus, cửa hàng (sẵn có), Bàn Chế Tạo, và bàn Sổ Tay. Dân làng được giải cứu sẽ DẦN TỤ VỀ trạm (cảm giác thung lũng hồi sinh, giống "trung tâm cộng đồng" của Stardew).
- Luồng mới: MainMenu → Mở đầu → TRẠM ⇄ Màn 1 (Nông Trại) ; TRẠM ⇄ Màn 2 (Nhà Máy) → Boss → Kết.
- Mỗi màn có một Cổng Về Trạm (luôn dùng được) → đây CHÍNH LÀ chiều đi ngược còn thiếu (Màn 2 → Trạm → Màn 1).
- Cổng tới Màn 2 ở Trạm đang HỎNG, cần nạp 3 Mảnh Cổng để kích hoạt (mục tiêu giữa game, buộc người chơi tham gia nội dung phụ). Mảnh Cổng lấy từ: thanh lọc giếng Màn 1, trùm nhỏ Màn 1, và nhiệm vụ làm sạch ao của Ông Tài.

🎒 6.2 Túi đồ & vật phẩm mở rộng (hệ thống của Khiêm)
- Túi đồ Ô LƯỚI XẾP CHỒNG (kiểu Stardew/Minecraft), mở bằng I / Tab, kèm thanh dùng nhanh phím 1–4 cho đồ tiêu hao. Vật phẩm nhặt được sẽ VÀO TÚI (xếp chồng) thay vì dùng ngay; đồ tiêu hao dùng từ túi/thanh nhanh. Túi GIỮ NGUYÊN qua các màn.
- Đồ tiêu hao: Bình Nước Suối (+2 máu), Nước Tăng Lực Mầm Xanh (+50% tốc 8s), Lá Chắn Mầm (khiên tạm), Bom Hạt Giống (ném → nổ AoE vừa DỌN RÁC vừa gây sát thương), Bình Hồi Phục Lớn (hồi đầy máu, chế tạo).
- Nguyên liệu (xếp chồng tới 99): Chai Nhựa, Mảnh Kim Loại, Lá Thuốc, Tinh Thể Năng Lượng (hiếm) — rơi từ rác/quái/điểm thu hoạch, dùng cho chế tạo + nhiệm vụ.
- Đồ quan trọng/nhiệm vụ: Thẻ Từ, Thuốc Giải Mầm Xanh, Mảnh Cổng (×3), vật phẩm theo nhiệm vụ.

🔨 6.3 Bàn Chế Tạo (ở Trạm)
- Đổi nguyên liệu → đồ tiêu hao/đạn/vật liệu nâng cấp. Công thức MỞ KHÓA dần qua nhiệm vụ/NPC. Ví dụ: 3 Chai Nhựa → Bình Nước Suối; 2 Lá Thuốc + 1 Chai Nhựa → Nước Tăng Lực; 5 Mảnh Kim Loại → Lá Chắn Mầm; 3 Mảnh Kim Loại + 2 Chai Nhựa → Bom Hạt Giống; 4 Tinh Thể Năng Lượng → 1 Mảnh Cổng (đường cày phòng khi thiếu mảnh từ nhiệm vụ → không kẹt cứng).

📋 6.4 Nhiệm vụ phụ (DO NPC GIAO — không có bảng nhiệm vụ)
- Nhiệm vụ phụ cốt truyện (nhiều bước, do NPC giao trực tiếp) — ngoài nhiệm vụ Thuốc Giải (mục 5) còn thêm: (2) "Người Bạn Nhỏ Của Bé Mây" (Màn 1: tìm thú cưng robot của Bé Mây ở góc ô nhiễm → công thức + nguyên liệu); (3) "Làm Sạch Ao Độc" (Ông Tài, Màn 1: dọn rác quanh ao → 1 Mảnh Cổng); (4) "Tin Tức Từ Bên Trong" (Cô Lan, Màn 2: thu 3 Mẩu Nhật Ký → mở lối tắt + công thức Mảnh Cổng + lộ âm mưu Giám đốc); (5) "Tái Chế Nâng Cao" (Ông Bear, Trạm: nộp 10 Kim Loại + 10 Chai Nhựa → mở công thức nâng cao).

📖 6.5 Sưu tầm, Sổ Tay & vòng lặp dọn sạch (gồm hiệu ứng của Anh)
- Cơ chế của Anh thành VÒNG LẶP CHÍNH: phá rác sẽ DỌN SẠCH một vùng đất quanh đó (hiệu ứng hồi sinh tỏa tròn) ĐỒNG THỜI (a) tăng "Độ Sạch Thung Lũng" (%) của màn, (b) có thể rơi nguyên liệu. Dọn rác = vừa đẹp mắt, vừa tiến trình, vừa ra tài nguyên.
- Sổ Tay Greenie (mở từ túi/menu) gồm 3 mục: Hồ Sơ Quái (mỗi loại quái 1 trang, mở khi lần đầu hạ), Mẩu Nhật Ký (8–10 mẩu giấu khắp 2 màn → hé lộ âm mưu Black Smoke, quá khứ làng, nguồn gốc Greenie — THÊM CỐT TRUYỆN mà không cần màn mới), Độ Sạch (% mỗi màn; đạt 50%/100% được thưởng).
- Trùm nhỏ Màn 1 — Slime Chúa: cục slime khổng lồ canh một Mảnh Cổng trong khu rừng ô nhiễm, thêm một pha chiến đấu và lý do để chế đồ trước.

👥 6.6 NPC mới & tuyến truyện
- Bé Mây (Màn 1, trẻ con, nhiệm vụ tìm thú cưng), Ông Tài (Màn 1, ông lão câu cá ở ao độc, nhiệm vụ làm sạch ao → Mảnh Cổng), Cô Lan (cựu công nhân Black Smoke/người đưa tin, tuyến nhật ký Màn 2 hé lộ Giám đốc). Giúp xong, các NPC DỌN VỀ TRẠM, làm trạm đông vui dần.
- Truyện sâu thêm mà không cần scene mới: trạm hồi sinh dần; tuyến Mẩu Nhật Ký vạch mặt phản diện; Mảnh Cổng tạo mục tiêu giữa game "sửa cổng hỏng".

⏱️ 6.7 Ngân sách ~30 phút (ước lượng)
Mở đầu + làm quen Trạm ~2' · Màn 1 chính (3 lõi → thanh lọc giếng) ~5' · Màn 1 phụ (lá thuốc, thú cưng Bé Mây, làm sạch ao, Slime Chúa, dọn tới 50%) ~7' · Vòng lặp ở Trạm (chế tạo, mua, nộp nhiệm vụ) ~4' · Màn 2 chính (2 thẻ từ + nạp 3 Mảnh Cổng + boss) ~7' · Màn 2 phụ (Cô Lan, cứu Tí, nhật ký, dọn) ~4' · Kết (+ đoàn tụ nếu cứu Tí) ~2' → TỔNG ~31 phút (dọn 100% + cày thêm nguyên liệu là phần chơi lại).

🎨 Phân công liên quan: Anh — hiệu ứng phá rác dọn đất (vòng lặp 6.5); Khang — sprite NPC/biểu tượng vật phẩm/giao diện túi-chế tạo, nhạc & SFX; Tùng — tester/PO kiểm tra ngân sách thời gian & checklist tính năng; Khiêm — túi đồ + mở rộng nội dung.