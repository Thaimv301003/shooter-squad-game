# Hướng dẫn sử dụng Figma Importer (BFunCore)

Công cụ này giúp đồng bộ trực tiếp thiết kế từ Figma vào project Unity. Để mọi thứ hoạt động chuẩn xác, cả Designer (trên Figma) và Dev (trên Unity) cần tuân thủ một số quy định sau:

---

## 1. Quy tắc thiết kế trên Figma (Naming Convention)

Script sẽ dựa vào **tên của từng Page và layer (Node)** trên Figma để xuất ra Component tương ứng trong Unity:

- **📄 Figma Pages (Điều hướng Canvas)**:
  - Đặt tên các Page trên Figma trùng với tên Canvas trong Unity (Ví dụ: `CanvasHome`, `CanvasGame`, `CanvasGlobal`).
  - Khi chạy tool, nó sẽ quét tên các Page này, tìm đúng file `.prefab` của Canvas đó ở trong thư mục hệ thống và tự động cập nhật ngầm thẳng vào Prefab. (Không cần phải kéo Prefab ra Scene).

- **🔰 `Panel_[Name]`**:
  - Dành cho các component chứa nội dung chính của một màn hình.
  - Script sẽ tự động thêm component `Panel.cs` từ hệ thống Panel của BFunCore vào GameObject sinh ra.
- **🔳 `Btn_[Name]`**:
  - Script tạo `Button` và tự động gắn `GUIButton.cs`.
  - Không bắt buộc phải có fill, nếu không có fill script sẽ tự tạo một Image trong suốt để nhận vùng bấm (Raycast Target).
- **🔤 `Txt_[Name]`**:
  - Sinh ra `TextMeshProUGUI`.
  - Sẽ giữ được các thông số cơ bản: Font Size, Alignment (Horizontal, Vertical) và màu sắc (Color).
  - Font chữ sẽ được lấy từ **Default TMP Font** truyền vào ở bảng Setting. 
- **🖼️ `Img_[Name]` / `Bg_[Name]`**:
  - Sẽ tự động có thêm Component `Image` của Unity.
  - Sẽ kéo được thông số fill là Solid Color với Alpha tương ứng.
- **📐 Layout (Auto Layout):**
  - Mọi Frame hoặc Group thông thường mà **CÓ bật Auto Layout** trên Figma sẽ tự sinh ra `HorizontalLayoutGroup` hoặc `VerticalLayoutGroup`.
  - Các thông số như *Spacing*, *Padding* bên trái, phải, trên, dưới đều được copy nguyên vẹn mang qua.
- **📦 Các Folder/Group/Frame không có tiền tố trên**:
  - Sẽ tự động trở thành các GameObject rỗng gắn `RectTransform` bình thường, phục vụ cho mục đích gom nhóm và định dạng bố cục tương đối.

---

## 2. Cách thiết lập (Setup) trên Unity

### Bước 1: Lấy Token từ Figma
1. Mở web Figma hoặc App Figma, nhấn vào profile góc trên cùng bên trái.
2. Chọn **Settings > Personal Access Tokens**.
3. Bấm **Generate new token**, đặt tên cho Token (ví dụ: "Unity Importer"), copy mã Token đó lại.

### Bước 2: Chuẩn bị trong Unity
1. Tạo thư mục/chọn ra một **Canvas** hoặc **GameObject cha** đang mở trong Scene (hoặc bạn click vào nó trong Hierarchy). Chỗ này nhằm điều hướng sinh các panel con vào đúng chỗ đã chọn.
2. Mở ScriptableObject tên là **`GUI Setting`** (Dùng thanh công cụ `BFunCo > UI Setting` hoặc click trên project).
3. Tại mục `Figma Importer Config`, điền:
   - **Figma URL**: Nhấp chuột phải copy link trang thiết kế chứa design bạn muốn lấy.
   - **Figma Token**: Dán đoạn mã Token ở Bước 1 vào (Token này sẽ được lưu ở EditorPrefs nên lần sau không cần dán lại).
   - **Default TMP Font**: Kéo thả font chữ (.asset) TMP mặc định của game vào đây.

### Bước 3: Import!
- Bấm nút **"📥 Download & Import Figma UI"**.
- Chờ thanh loading tải xong. Data sẽ biến thành các cây UI tự động trên Scene của bạn, chuẩn từng Pixel và Layout!
