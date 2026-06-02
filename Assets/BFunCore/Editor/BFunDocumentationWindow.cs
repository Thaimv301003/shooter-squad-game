#if UNITY_EDITOR
using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace BFunCoreKit.Editor
{
    public class BFunDocumentationWindow : OdinMenuEditorWindow
    {
        [MenuItem("BFun/Documentation (Wiki)", priority = -100)]
        [MenuItem("BFun/Documentation (Wiki)", priority = -100)]
        private static void OpenWindow()
        {
            // 1. Lấy instance của window (Utility = true để cửa sổ nổi lên trên giống tool selector)
            var window = GetWindow<BFunDocumentationWindow>(true, "BFun Wiki", true);
            window.titleContent = new GUIContent("BFun Wiki");

            // 2. Định nghĩa kích thước cố định
            float width = 1350;
            float height = 900;
            Vector2 fixedSize = new Vector2(width, height);

            // 3. --- KHÓA KÍCH THƯỚC TẠI ĐÂY ---
            window.minSize = fixedSize;
            window.maxSize = fixedSize;
            // ----------------------------------

            // 4. Căn giữa màn hình Editor
            Rect mainWindow = EditorGUIUtility.GetMainWindowPosition();
            float x = mainWindow.x + (mainWindow.width - width) * 0.5f;
            float y = mainWindow.y + (mainWindow.height - height) * 0.5f;
            window.position = new Rect(x, y, width, height);

            window.Show();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();
            tree.Config.DrawSearchToolbar = true;
            tree.DefaultMenuStyle.IconSize = 28.00f;
            tree.Config.DefaultMenuStyle.Height = 36;

            // --- 1. START HERE ---
            tree.Add("1. Start Here/About", new AboutPage());
            tree.Add("1. Start Here/Cheat Sheet", new CheatSheetPage());
            tree.Add("1. Start Here/Project Structure", new ProjectStructPage());

            // --- 2. UI SYSTEM (LÕI) ---
            tree.Add("2. UI System/1. UI Architecture", new UIArchitecturePage());
            tree.Add("2. UI System/2. Canvas & Addressables", new UICanvasPage());
            tree.Add("2. UI System/3. Panel & Animation", new UIPanelPage());
            tree.Add("2. UI System/4. Color & Font", new UIStylePage());
            tree.Add("2. UI System/5. The Flow", new UIWorkflowPage());

            // --- 3. LOGIC SYSTEM ---
            tree.Add("3. Logic System/1. Game Flow", new GameManagersPage());
            tree.Add("3. Logic System/2. Data Binding", new BindingPage());
            tree.Add("3. Logic System/3. Localization", new LocalizationPage());
            tree.Add("3. Logic System/4. Audio System", new AudioPage());
            tree.Add("3. Logic System/5. Graphics & Profiling", new GraphicsPage());

            // --- 4. TOOLS ---
            tree.Add("4. Tools/1. Package Library", new PackagePage());
            tree.Add("4. Tools/2. Bug Report", new BugReportPage());
            tree.Add("4. Tools/3. Debug Console", new ConsolePage());
            tree.Add("4. Tools/4. Ads Manager", new AdsPage());
            tree.Add("4. Tools/5. Smart Build", new BuildPage());

            // --- 5. UTILS ---
            tree.Add("5. Utilities/Code Helpers", new UtilsPage());

            return tree;
        }

        public class AboutPage
        {
            [Title("Code better together")]
            [LabelText("About – BFunCoreKit")]
            [CodeSnippet]
            public string Globals =
$@"Version {GameManager.BFUN_VERSION}

BFunCoreKit là bộ package được thiết kế nhằm hỗ trợ Developer phát triển Game một cách nhanh chóng, tối ưu và hiện đại hơn. Bộ công cụ tập trung cung cấp những tiện ích cốt lõi, các giải pháp thực tiễn và những workflow hiệu quả nhất, giúp rút ngắn thời gian sản xuất mà vẫn đảm bảo chất lượng dự án.

Không áp đặt khuôn mẫu hay cấu trúc cố định, BFunCoreKit tạo điều kiện để mỗi Developer giữ được phong cách và cá tính riêng trong sản phẩm của mình. Đây là “bộ nền tảng” linh hoạt giúp bạn xây dựng đúng cách — nhưng vẫn tự do sáng tạo theo định hướng riêng.

BFunCoreKit được phát triển bởi BFun Dev Team, với mục tiêu mang đến bộ công cụ thân thiện, chuẩn hoá và tối ưu nhất cho cộng đồng làm game.
";

        }

            // =========================================================================
            // 1. START HERE
            // =========================================================================
            public class CheatSheetPage
        {
            [Title("Cheat Sheet - Phím tắt & Điều hướng")]
            [InfoBox("Các thao tác nhanh giúp tăng tốc độ làm việc.")]

            [CodeSnippet]
            public string Globals =
@"
<color=#13fc03>~ (Backquote) </color>
Editor
-> Mở BFun Manager. Cửa sổ cấu hình trung tâm.

Runtime
-> Mở Debug Console. Xem Log, FPS, RAM ngay trên máy thật.

<color=#13fc03>SHIFT + S </color>
-> Mở Scene Quick Selector -> Nhập số (VD: 0,1,2....). Chuyển đổi giữa các Scene (Loading, Home, Game) tức thì.

<color=#13fc03>SHIFT + A </color>
-> Mở Prefab Quick Selector -> Nhập số (VD: 0,1,2....). Truy cập nhanh vào kho UI Canvas (Addressables) để chỉnh sửa giao diện.
.";

            [CodeSnippet]
            public string ManagerNavigation =
@"Khi đang mở BFun Manager (~), bấm các phím số để chuyển tab nhanh:
<color=#13fc03>1 </color>-> Project
<color=#13fc03>2 </color>-> Ads Setting
<color=#13fc03>3 </color>-> Binding Setting
...
<color=#13fc03>= (Dấu bằng)</color> -> Tab cuối cùng (UI Setting)";

            [CodeSnippet]
            public string UI =
@"Phím <color=#13fc03>N</color>
-> Tác dụng: Tạo Panel Mới (Popup/Dialog).
-> Tự động tạo Panel, căn lề (Stretch), đặt tên chuẩn, xếp ngang hàng ngăn nắp.";

            [CodeSnippet]
            public string Runtime =
@"<color=#13fc03>DOUBLE TAP (3 ngón tay)</color>
-> Mở Debug Console. Xem Log, FPS, RAM ngay trên máy thật.";
        }

        public class ProjectStructPage
        {
            [Title("Cấu trúc Thư mục")]
            [CodeSnippet]
            public string Folders =
@"<color=#13fc03>Assets/AddressablePrefabs/Canvas/</color>
 -> Nơi chứa toàn bộ Prefab UI (Home, Game, Popup...). Đây là nơi duy nhất chứa UI thực thể.
 -> Vì nếu đã sử dụng Addressable thì ko thể nào để ở thư mục Resources được nên phải để ra thư mục riêng như này.

<color=#13fc03>Assets/BfunCore/</color>
 -> Nơi chứa toàn bộ Logic Code (Hạn chế thay đổi, di chuyển, sửa file).

<color=#13fc03>Assets/Resources/</color>
 -> Chứa các file Config (ScriptableObjects).
 -> Lý do nằm trong Resources: Để code có thể load đồng bộ (Resources.Load) ngay khi khởi động.
    - Project.asset: Config version, package name.
    - Sound Setting.asset: List âm thanh.
    - UI Setting.asset: List Canvas.
    - Ads Setting.asset: Config ID quảng cáo.";
        }

        // =========================================================================
        // 2. UI SYSTEM
        // =========================================================================
        public class UIArchitecturePage
        {
            [Title("Tổng quan Kiến trúc UI")]
            [InfoBox("Hệ thống được thiết kế để quản lý bộ nhớ hiệu quả và tách biệt logic.")]

            [CodeSnippet]
            public string CanvasBase =
@"<color=#13fc03>Đại diện:</color> CanvasHome, CanvasGame, CanvasGlobal.
Vị trí: Root GameObject của Prefab.

<color=#13fc03>Nhiệm vụ:</color>
1. Singleton: Là điểm truy cập toàn cục (CanvasHome.Instance).
2. Addressables: Được load bất đồng bộ, giúp giảm RAM khởi động game.
3. Canvas Scaler: Chịu trách nhiệm tính toán Aspect Ratio, xử lý vùng an toàn (Safe Area) cho các màn hình tai thỏ.";

            [CodeSnippet]
            public string UIPanel =
@"<color=#13fc03>Đại diện:</color> Script 'UIPanel' (Con trực tiếp của Canvas).

<color=#13fc03>Nhiệm vụ:</color>
1. Quản lý Panel con: Nắm giữ danh sách tất cả Popup/Dialog trong màn hình đó.
2. Enum Generator: Có nút 'Save' để quét tên các Panel con và sinh ra file Enum (VD: PanelHome.Shop).
3. Type-Safety: Giúp code gọi UI bằng Enum thay vì string, tránh lỗi gõ sai.";

            [CodeSnippet]
            public string Panel =
@"<color=#13fc03>Đại diện:</color> Script 'Panel.cs' gắn trên từng Popup (VD: DailyReward).

<color=#13fc03>Nhiệm vụ:</color>
1. View: Chứa các Image, Text, Button thực tế.
2. Animation: Chứa cấu hình LitMotion để chạy hiệu ứng Show/Hide.
3. Audio: Chứa cấu hình âm thanh khi mở/đóng.
4. Events: Có thể gắn các sự kiện (OnInit, OnDoneEffect) để logic game biết khi nào popup mở xong.";
        }

        public class UICanvasPage
        {
            [Title("Canvas & Automation")]

            [LabelText("Cơ chế Tự động hóa")]
            [CodeSnippet]
            public string Auto =
@"Việc tạo thủ công một Canvas rất <color=#13fc03>phức tạp và tốn thời gian</color> (Tạo Prefab, Script, Enum, Addressables...).
BFun Manager <color=#13fc03>tự động hóa</color> 100% quy trình này:

1. Clone Template: Copy 'CanvasBase.prefab' chuẩn.
2. Generate Code: Tự viết script C# kế thừa CanvasBase<T>.
3. Generate Enum: Tự tạo file Enum rỗng để chờ add Panel.
4. Addressables: Tự động tìm Group 'UI', thêm Prefab vào và gán Label.
5. Compile: Tự động refresh AssetDatabase để nhận code mới.

-> Dev chỉ cần nhập tên và chờ 5 giây.";
        }

        public class UIPanelPage
        {
            [Title("Panel & Animation")]
            [InfoBox("Sử dụng LitMotion - Thư viện tween hiệu năng cao nhất hiện nay.")]

            [LabelText("Cấu hình Animation Mặc định")]
            [CodeSnippet]
            public string Default =
@"Khi tạo mới một <color=#13fc03>Panel (phím N)</color>, hệ thống tự động gắn sẵn 2 file LitMotion:
1. Show Animation
2. Close Animation

-Lúc khởi tạo người dùng cần chọn LayerSpawnPoint là layer để spawn Panel này vào và chỉnh sửa Order để sắp xếp thứ tự hiện.

-Thêm vào đó là setup Animation Show và Close vì lúc đầu sẽ chưa có Animation gì.

-Hỗ trợ thêm cà việc Save & Load Animation để dễ dàng sử dụng lại.

-> Popup mới tạo ra đã có thể bật tắt mượt mà ngay lập tức.";

            [LabelText("Effect Group & Customization")]
            [CodeSnippet]
            public string Custom =
@"Bạn có thể tùy biến sâu hơn thông qua 'Effect Group':
- Delay: Chờ X giây mới chạy (VD: Popup hiện ra sau khi pháo hoa nổ).
- Sequence: Chạy nối tiếp (Fade nền đen trước -> Popup bay vào sau).
- Option: Một Panel có thể có nhiều kiểu hiện.
  VD: Popup 'Win'
  - Option 'Normal': Hiện ra bình thường.
  - Option 'Fast': Hiện ra tức thì (khi user skip).
  Code gọi: ShowPanel(PanelGame.Win, ""Fast"");";
        }

        public class UIStylePage
        {
            [Title("Styling System")]
            [InfoBox("Quản lý giao diện tập trung (Color & Font).")]

            [LabelText("Cách sử dụng")]
            [CodeSnippet]
            public string Usage =
@"1. <color=#13fc03>Color Setting:</color>
   - Vào BFun Manager -> Color Setting.
   - Định nghĩa các mã màu (Primary, Secondary, Text, Warning...).
   - Trong Prefab UI: KHÔNG chỉnh màu thủ công ở Image.
   - Gắn script 'GUIColorBinder' -> Chọn Style mong muốn.
   -> Lợi ích: Muốn đổi theme game từ Sáng sang Tối, chỉ cần sửa Color Setting 1 lần.

2. <color=#13fc03>Font Setting:</color>
   - Vào BFun Manager -> Font Setting.
   - Định nghĩa Style (H1, H2, Body...). Set Font Asset, Size.
   - Trong Prefab UI: Gắn 'GUITextBinder' (có sẵn trong Prefab GUIText).
   -> Lợi ích: Đổi font chữ toàn game trong 1 click (Cực quan trọng khi làm Localization).";
        }

        public class UIWorkflowPage
        {
            [Title("Quy trình tạo UI Mới")]
            [InfoBox("Tuân thủ quy trình này để hệ thống tự động hóa hoạt động chính xác.")]

            [CodeSnippet]
            [LabelText("Bước 1. Tạo Canvas (Trong BFun Manager)")]
            public string S1 =
@"1. Mở Manager (<color=#13fc03>~</color>) -> Tab 'UI Setting'.
2. Bấm 'Create New Canvas'. Nhập tên (VD: CanvasShop).
-> Hệ thống tự tạo Prefab, Script, Enum và đăng ký Addressables.";

            [CodeSnippet]
            [LabelText("Bước 2. Layout (Trong Prefab Mode)")]
            public string S2 =
@"1. Mở Prefab 'CanvasShop' vừa tạo (<color=#13fc03>Shift+A</color>).
2. Bấm phím '<color=#13fc03>N</color>' (Phím tắt tạo Panel).
   -> Tự động tạo Panel mới, Reset RectTransform, Căn lề.
3. Dựng UI:
   - Dùng Prefab 'GUIText': Đã gắn sẵn GUITextBinder để chỉnh Font/Lang.
   - Dùng Prefab 'GUIButton': Đã gắn sẵn logic Button cơ bản.
   - Kéo thả hình ảnh vào Image.";

            [CodeSnippet]
            [LabelText("Bước 3. Lưu & Đăng ký")]
            public string S3 =
@"1. Bấm <color=#13fc03>Ctrl + S</color> (Lưu Prefab).
   -> Script 'PrefabSaveDetector' sẽ chạy ngầm.
   -> Nó phát hiện bạn đang lưu 1 Canvas.
   -> Nó tự động quét các Panel con và cập nhật file Enum 'PanelShop.cs'.
(Không cần bấm nút thủ công, nhưng nút vẫn còn đó nếu cần).";

            [CodeSnippet]
            [LabelText("Bước 4. Code Logic")]
            public string S4 =
@"<color=#6A9955>// Gọi mở</color>
<color=#569CD6>yield</color> <color=#569CD6>return</color> <color=#4EC9B0>CanvasShop</color>.Instance.ShowPanel(<color=#4EC9B0>PanelShop</color>.ItemPopup);

<color=#6A9955>// Gọi đóng</color>
<color=#569CD6>yield</color> <color=#569CD6>return</color> <color=#4EC9B0>CanvasShop</color>.Instance.ClosePanel(<color=#4EC9B0>PanelShop</color>.ItemPopup);

<color=#6A9955>// Gọi option</color>
<color=#569CD6>yield</color> <color=#569CD6>return</color> <color=#4EC9B0>CanvasShop</color>.Instance.ShowPanel(<color=#4EC9B0>PanelShop</color>.ItemPopup, <color=#4EC9B0>ItemPopupPanelShowOption</color>.Default);
";
        }

        // =========================================================================
        // 3. LOGIC SYSTEM
        // =========================================================================

        public class GameManagersPage
        {
            [Title("Game Flow Managers")]
            [InfoBox("Hai class quan trọng nhất điều khiển luồng game.")]

            [LabelText("1. LoadManager (Scene Management)")]
            [CodeSnippet]
            public string LoadMgr =
@"Quy trình chuyển cảnh chuẩn:
1. Unload Scene cũ -> Giải phóng RAM.
2. Gọi Resources.UnloadUnusedAssets() -> Dọn rác triệt để.
3. Load Scene mới (Async).";

            [LabelText("Code Example")]
            [CodeSnippet]
            public string loadExample =
@"
<color=#4EC9B0>StartCoroutine</color>(<color=#4EC9B0>LoadManager</color>.LoadScene(""GameScene"", <color=#4EC9B0>LoadSceneMode</color>.Single));
";

            [LabelText("2. GUIManager (Canvas Management)")]
            [CodeSnippet]
            public string GUIMgr =
@"Singleton quản lý việc hiển thị các Canvas Addressables.
Hàm quan trọng: SwitchCanvas.

Logic:
1. Nhận yêu cầu chuyển sang Canvas mới (VD: Từ Home sang Game).
2. Release (Hủy) Canvas cũ để trả lại RAM.
3. InstantiateAsync Canvas mới từ Addressables.
4. Đảm bảo Camera UI được gán đúng.";

            [LabelText("Code Example")]
            [CodeSnippet]
            public string guiExample =
@"
<color=#6A9955>// Sử dụng Coroutine để giải quyết bất đồng bộ</color>

<color=#6A9955>// Load background</color>
<color=#569CD6>yield return </color><color=#4EC9B0>GUIManager</color>.Instance.ShowBackground();

<color=#6A9955>// Đổi canvas</color>
<color=#569CD6>yield return </color><color=#4EC9B0>GUIManager</color>.Instance.SwitchCanvas(<color=#4EC9B0>CanvasName</color>.CanvasGame);

<color=#6A9955>// Đóng background</color>
<color=#569CD6>yield return </color><color=#4EC9B0>GUIManager</color>.Instance.CloseBackground();
";
        }

        public class BindingPage
        {
            [Title("Data Binding (MVVM)")]
            [InfoBox("Tách biệt Logic (Model) và Giao diện (View).")]

            [LabelText("Quy trình Setup")]
            [CodeSnippet]
            public string Flow =
@"Bước 1: Vào BFun Manager -> Binding Setting.
    -> Kéo script Logic (VD: UserProfile) vào list.
    -> Việc này giúp DataBinder biết class nào tồn tại để hiển thị trong menu.

Bước 2: Thiết kế UI bằng Prefab chuẩn:
    -> GUIText: Cho các dòng chữ hiển thị thông số (Vàng, Level).
    -> GUIButton: Cho các nút bấm gọi hàm (Play, Buy).
    -> Slider/InputField: Cho các thanh trượt/nhập liệu.

Bước 3: Config Inspector:
    -> Trên component DataBinder, chọn Class và Member (Biến/Hàm) muốn liên kết.";

            [LabelText("Các chế độ Update")]
            [CodeSnippet]
            public string Modes =
@"1. <color=#13fc03>OnEnable</color>: Update 1 lần khi bật. Dùng cho text tĩnh (Tên nhân vật).
2. <color=#13fc03>Update</color>: Update mỗi frame. Dùng cho thanh HP, đồng hồ đếm ngược (Cần mượt).
3. <color=#13fc03>OnEvent</color> (Khuyên dùng): Update khi có sự kiện.";

            [LabelText("Code Example: OnEvent Pattern")]
            [CodeSnippet]
            public string EventCode =
@"<color=#569CD6>public</color> <color=#569CD6>class</color> <color=#4EC9B0>UserData</color> : <color=#4EC9B0>MonoBehaviour</color>
{
    <color=#6A9955>// 1. Property dữ liệu</color>
    <color=#569CD6>public</color> <color=#569CD6>int</color> Gold => _gold;
    
    <color=#6A9955>// 2. Event tương ứng (Tên phải là On + Name + Changed)</color>
    <color=#569CD6>public</color> <color=#569CD6>event</color> <color=#4EC9B0>Action</color> OnGoldChanged; 

    <color=#569CD6>public</color> <color=#569CD6>void</color> AddGold(<color=#569CD6>int</color> val) {
        _gold += val;
        OnGoldChanged?.Invoke(); <color=#6A9955>// Bắn event -> UI tự cập nhật</color>
    }
}";
        }

        public class LocalizationPage
        {
            [Title("Localization")]
            [InfoBox("QUAN TRỌNG: Bắt buộc sử dụng Prefab GUIText thì mới dịch được tự động.")]

            [LabelText("Quy trình Dịch thuật")]
            [CodeSnippet]
            public string Flow =
@"Sau khi setup xong các Canvas với việc sử dụng Prefab GUIText:

<color=#13fc03>Bước 1:</color> Vào BFun Manager -> Localization Setting.
<color=#13fc03>Bước 2:</color> Bấm nút 'Open Localization Dashboard'.
<color=#13fc03>Bước 3:</color> Trên Dashboard, bấm '<color=#13fc03>Scan All</color>'.
    -> Tool sẽ quét toàn bộ Prefab GUIText và Code trong dự án.
    -> Tìm các chuỗi text chưa có Key và tự sinh Key (VD: 'HELLO_WORLD').
<color=#13fc03>Bước 4:</color> Bấm 'Copy Prompt & Data'.
    -> Paste vào ChatGPT/Excel để dịch sang các ngôn ngữ khác.
<color=#13fc03>Bước 5:</color> Paste kết quả dịch vào ô Import -> Bấm Import.";

            [LabelText("Example 1: Translate tĩnh (Text cứng)")]
            [CodeSnippet]
            public string Static =
@"<color=#6A9955>// Chỉ cần sử dụng Prefab GUIText đã có sẵn GUITextBinder.</color>
<color=#6A9955>// Khi chạy game, nó tự tìm Key và hiển thị text theo ngôn ngữ máy.</color>
<color=#6A9955>// VD: Key ""START_GAME"" -> Hiển thị ""Bắt đầu"" (nếu máy là Tiếng Việt).</color>";

            [LabelText("Example 2: Translate động (Code)")]
            [CodeSnippet]
            public string Dynamic =
@"
<color=#6A9955>// Example không có biến phụ</color>
<color=#569CD6>string</color> msg = <color=#4EC9B0>LocalizationManager</color>.Translate(""Hello"");

<color=#4EC9B0>BFun</color>.Log(msg); 
<color=#6A9955>// Output (Vi): ""Xin Chào!""</color>

<color=#6A9955>// Example có biến phụ</color>
<color=#569CD6>void</color> ShowReward(<color=#569CD6>int</color> gold, <color=#569CD6>int</color> gem)
{
    <color=#6A9955>// Truyền tham số vào {0}, {1}</color>
    <color=#569CD6>string</color> msg = <color=#4EC9B0>LocalizationManager</color>.Translate(""You received {0} golds and {1} gems!"", gold, gem);
    
    <color=#4EC9B0>BFun</color>.Log(msg); 
    <color=#6A9955>// Output (Vi): ""Bạn nhận được 100 Vàng và 5 Đá quý!""</color>
}

<color=#6A9955>// Khi bấm Scan All ở config Localization sẽ tự động tìm những text để dịch thông qua hàm Translate này.</color>
";
        }

        public class AudioPage
        {
            [Title("Audio System")]

            [LabelText("Cấu trúc & Workflow")]
            [CodeSnippet]
            public string Struct =
@"1. Audio Listener: Quản lý âm lượng tổng.
2. Audio Mixer:
   - Chia kênh: Music (Nhạc nền), SFX (Tiếng động).
   - Cần thiết để làm tính năng Setting (Tắt nhạc/Tắt tiếng) trong game.

3. Workflow thêm âm thanh:
   - Mở 'Sound Setting'.
   - Kéo thả AudioClip vào list.
   - Hệ thống TỰ ĐỘNG sinh Enum 'SoundName'.
   -> Code gọi: PlaySound(SoundName.Fire).";

            [LabelText("Code Example")]
            [CodeSnippet]
            public string audioExample =
@"
<color=#6A9955>// Play sound</color>
<color=#4EC9B0>SoundManager</color>.Instance.PlaySound(<color=#4EC9B0>Soundname</color>.Fire);
";

            [LabelText("Lưu ý quan trọng")]
            [CodeSnippet]
            public string Note =
@"Nếu bạn tạo AudioSource thủ công trong Scene (không qua code):
-> BẮT BUỘC phải gán 'Output' của AudioSource đó vào Mixer Group (Music hoặc SFX).
-> Nếu không, âm thanh đó sẽ không chịu ảnh hưởng của thanh Volume trong Setting game (Tắt tiếng vẫn kêu).";
        }

        public class GraphicsPage
        {
            [Title("Graphics & Profiling")]

            [LabelText("Hỗ trợ Render Pipeline")]
            [CodeSnippet]
            public string Intro =
@"Hệ thống hỗ trợ cả <color=#13fc03>Built-in</color> Render Pipeline và <color=#13fc03>URP</color>.
Tự động nhận diện pipeline đang dùng để điều chỉnh setting phù hợp.";

            [LabelText("Thông số tối ưu (Graphics Setting)")]
            [CodeSnippet]
            public string Param =
@"1. Render Scale (Resolution Scaling):
   - Quan trọng nhất. Giảm độ phân giải render (VD: 0.7x) giúp tăng FPS cực mạnh trên máy yếu mà UI vẫn nét (vì UI render overlay).

2. Shadow Quality:
   - Tắt bóng hoặc giảm khoảng cách vẽ bóng (Shadow Distance) để giảm tải GPU.

3. Texture Quality:
   - Giảm kích thước texture (Half/Quarter) để tiết kiệm VRAM, tránh crash trên máy RAM 2GB.";

            [LabelText("Logic Phân loại thiết bị")]
            [CodeSnippet]
            public string Code =
@"<color=#6A9955>// DeviceProfiler.cs</color>
<color=#569CD6>if</color> (ram <= 3072) <color=#569CD6>return</color> Low; <color=#6A9955>// Máy < 3GB RAM -> Cấu hình thấp</color>
<color=#569CD6>if</color> (ram >= 6144) <color=#569CD6>return</color> High; <color=#6A9955>// Máy > 6GB RAM -> Cấu hình cao</color>
<color=#569CD6>return</color> Medium;";
        }

        // =========================================================================
        // 4. TOOLS
        // =========================================================================
        public class PackagePage
        {
            [Title("Package Library (Internal)")]
            [InfoBox("Kho thư viện dùng chung cho team.")]

            [LabelText("Yêu cầu & Cách dùng")]
            [CodeSnippet]
            public string Usage =
@"1. Đăng nhập: Bắt buộc dùng Gmail đuôi @bfun... để có quyền truy cập Google Drive chung.
2. Cơ chế: Tool kết nối tới folder Drive của team.
3. Cài đặt: Bấm Install để tải và import package vào dự án. Tool tự check xem package đã có chưa.
4. Thêm Package mới: Chỉ cần upload file .unitypackage lên folder Drive đó. Tool của mọi người sẽ tự hiện nút Install mới.
5. Xóa: Tool KHÔNG hỗ trợ Remove (do cấu trúc thư mục mỗi người khác nhau). Hãy xóa thủ công trong Project window.";
        }

        public class BugReportPage
        {
            [Title("Bug Report System")]

            [LabelText("Thư viện lỗi chung")]
            [CodeSnippet]
            public string Desc =
@"Đây là một Bug Tracker thu nhỏ tích hợp trong Unity.
- Tester/Dev gặp lỗi -> Mở tool -> Nhập mô tả -> Gửi.
- Hệ thống tự upload ảnh lên Drive, ghi log vào Google Sheet.
- Ai cũng có thể mở tool lên để xem danh sách lỗi hiện tại.
- Có nút Preview để xem ảnh lỗi ngay trong Editor mà không cần tải về.";
        }

        public class ConsolePage
        {
            [Title("Debug Console")]

            [LabelText("Phím tắt & Lệnh")]
            [CodeSnippet]
            public string Usage =
@"Phím tắt:
- PC: Phím `
- Mobile: Double Tap (3 ngón tay).

Lợi ích:
- Check FPS, RAM thực tế trên build.
- Xem log lỗi runtime.
- Chạy lệnh Cheat (Hack tiền, qua màn) để test nhanh.
- Có 2 cách thêm command 1 là thêm bằng setting DebugConsole trong cửa sổ Manager 2 là code bằng cách sử dụng [DebugCommand()]";

            [LabelText("Cách tạo lệnh Cheat mới")]
            [CodeSnippet]
            public string Add =
@"<color=#6A9955>// Chỉ cần thêm Attribute [DebugCommand]</color>
[<color=#4EC9B0>DebugCommand</color>(""add_gold"", ""Hack 999 Vàng"")]
<color=#569CD6>public</color> <color=#569CD6>static</color> <color=#569CD6>void</color> CheatGold() {
    <color=#4EC9B0>UserProfile</color>.Gold += 999;
}";
        }

        public class AdsPage
        {
            [Title("Ads Manager")]

            [LabelText("Quick Setup")]
            [CodeSnippet]
            public string Setup =
@"Đây là chức năng cài đặt nhanh 'One-Click'.
Thay vì phải cài tay từng quảng cáo hoặc phải chọn từng loại một thif tool này sẽ hỗ trợ cài nhanh";
        }

        public class BuildPage
        {
            [Title("Smart Build")]

            [LabelText("Tính năng")]
            [CodeSnippet]
            public string Func =
@"1. Nút Build nhanh: Bấm vào icon Android/iOS trong Manager.
2. Tự động Keystore: Điền mật khẩu 1 lần, tool tự nhớ cho lần sau.
3. Auto Switch: Chọn Build Debug (APK) hoặc Release (AAB) dễ dàng.
4. Version Injection: Tự động ghi thông tin Version vào file Resource để hiển thị trong game.";
        }

        // =========================================================================
        // 5. UTILITIES
        // =========================================================================
        public class UtilsPage
        {
            [Title("Code Helpers")]
            [LabelText("Helper Class")]
            [CodeSnippet]
            public string Func =
@"Bao gồm các lớp Helper giúp việc Code nhanh và thuận tiện hơn";
        

            [CodeSnippet]
            [LabelText("WaitCache (Tối ưu Coroutine)")]
            public string Wait =
@"<color=#6A9955>// Dùng cái này:</color>
<color=#569CD6>yield</color> <color=#569CD6>return</color> <color=#4EC9B0>WaitCache</color>.Get(1f);

<color=#6A9955>// Thay vì:</color>
<color=#569CD6>yield</color> <color=#569CD6>return</color> <color=#569CD6>new</color> WaitForSeconds(1f); <color=#6A9955>// Tạo rác bộ nhớ</color>";

            [CodeSnippet]
            [LabelText("BFun.Log (Logging chuẩn)")]
            public string Log =
@"<color=#4EC9B0>BFun</color>.Log(""Info"");
<color=#4EC9B0>BFun</color>.LogWarning(""Warning"");
<color=#4EC9B0>BFun</color>.LogEvent(""Level_Start""); <color=#6A9955>// Tự bắn Analytics</color>";
        }
    }

    // =========================================================================
    // 🔹 CUSTOM ATTRIBUTE & DRAWER
    // =========================================================================
    public class CodeSnippetAttribute : Attribute { }

    public class CodeSnippetDrawer : OdinAttributeDrawer<CodeSnippetAttribute, string>
    {
        private static GUIStyle _codeStyle;
        private static Texture2D _codeBgTexture;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            // 1. Setup Style (Giữ nguyên như cũ)
            if (_codeStyle == null || _codeBgTexture == null)
            {
                _codeBgTexture = new Texture2D(1, 1);
                _codeBgTexture.SetPixel(0, 0, new Color(0.117f, 0.117f, 0.117f, 1f)); // Dark VS Code
                _codeBgTexture.Apply();

                _codeStyle = new GUIStyle(EditorStyles.textArea); // Vẫn mượn base từ TextArea để lấy wordwrap
                _codeStyle.richText = true;
                _codeStyle.normal.background = _codeBgTexture;
                _codeStyle.active.background = _codeBgTexture;
                _codeStyle.focused.background = _codeBgTexture;
                _codeStyle.hover.background = _codeBgTexture;

                // Chỉnh lại màu chữ khi ở trạng thái Normal (SelectableLabel dùng state Normal)
                _codeStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 1f);

                _codeStyle.fontSize = 13;
                _codeStyle.padding = new RectOffset(14, 14, 14, 14);
                _codeStyle.margin = new RectOffset(0, 0, 10, 10);
                // Fallback font nếu không có Consolas
                _codeStyle.font = Font.CreateDynamicFontFromOSFont("Consolas", 13) ?? EditorStyles.standardFont;
                _codeStyle.wordWrap = true;
            }

            // 2. Vẽ Tiêu đề
            if (label != null) SirenixEditorGUI.Title(label.text, null, TextAlignment.Left, false);

            string content = this.ValueEntry.SmartValue;

            // 3. Tính toán chiều cao
            // Lưu ý: SelectableLabel cần chiều cao chính xác hơn TextArea một chút để không bị cắt chữ
            float width = GUIHelper.CurrentWindow.position.width - 50;
            float height = _codeStyle.CalcHeight(new GUIContent(content), width);

            // 4. [QUAN TRỌNG] Dùng SelectableLabel thay vì TextArea
            // - SelectableLabel: Cho phép bôi đen copy, KHÔNG cho sửa, không hiện con trỏ nhấp nháy.
            // - Cộng thêm 5f vào height để tránh bị cắt dòng cuối cùng do padding
            EditorGUILayout.SelectableLabel(content, _codeStyle, GUILayout.Height(Mathf.Max(height + 5f, 40)));
        }
    }
}
#endif