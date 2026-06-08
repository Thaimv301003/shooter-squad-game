using UnityEngine;
using Watermelon;
using Watermelon.SquadShooter;
using Watermelon.LevelSystem;

public class CheatManager : MonoBehaviour
{
    private void Update()
    {
        // Nhận phím cho cả Input System cũ và mới để chắc chắn ăn phím
        bool isCPressed = Input.GetKeyDown(KeyCode.C) || (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.cKey.wasPressedThisFrame);
        bool isHPressed = Input.GetKeyDown(KeyCode.H) || (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.hKey.wasPressedThisFrame);
        bool isVPressed = Input.GetKeyDown(KeyCode.V) || (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.vKey.wasPressedThisFrame);
        bool isKPressed = Input.GetKeyDown(KeyCode.K) || (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.kKey.wasPressedThisFrame);

        // 1. Phím C: Hack Coins (Tiền vàng)
        if (isCPressed)
        {
            Debug.Log("👉 Bấm C: Thêm 1,000,000 Coins!");
            CurrencyController.Add(CurrencyType.Coins, 1000000);
        }

        // 2. Phím H: Hack 1,000 thẻ súng (Weapon Cards) cho tất cả các loại súng
        if (isHPressed)
        {
            Debug.Log("👉 Bấm H: Thêm 1,000 thẻ súng cho tất cả các loại súng!");
            if (WeaponsController.Weapons != null)
            {
                foreach (var weapon in WeaponsController.Weapons)
                {
                    WeaponsController.AddCard(weapon, 1000);
                }
            }
        }

        // 3. Phím V: Mở khóa tất cả các loại súng lập tức
        if (isVPressed)
        {
            Debug.Log("👉 Bấm V: Mở khóa/Nâng cấp tất cả các súng!");
            WeaponsController.UnlockAllWeaponsDev();
        }

        // 4. Phím K: Đặt level người chơi thành 99 (để mở khóa tất cả nhân vật bị giới hạn level)
        if (isKPressed)
        {
            Debug.Log("👉 Bấm K: Đặt Level nhân vật thành 99 (Mở khóa tất cả các nhân vật)!");
            ExperienceController.SetLevelDev(99);
        }
    }

    private int selectedWorldIndex = -1;
    private int selectedLevelIndex = -1;

    // Vẽ giao diện Menu Hack lên góc màn hình Game để click chuột trực tiếp
    private void OnGUI()
    {
        // Thiết lập phong cách cho nút bấm to và dễ nhìn
        GUI.skin.button.fontSize = 14;
        
        // Khởi tạo chỉ số màn chơi từ file Save nếu chưa thiết lập
        var levelSave = SaveController.GetSaveObject<LevelSave>("level");
        if (levelSave != null && selectedWorldIndex == -1)
        {
            selectedWorldIndex = levelSave.WorldIndex;
            selectedLevelIndex = levelSave.LevelIndex;
        }

        // Vẽ khung Cheat Menu ở góc trên bên trái
        GUILayout.BeginArea(new Rect(15, 15, 230, 485));
        GUILayout.Box("🔧 DEV CHEAT MENU", GUILayout.Width(220));

        if (GUILayout.Button("💰 +1,000,000 Coins (Phím C)", GUILayout.Height(40), GUILayout.Width(220)))
        {
            CurrencyController.Add(CurrencyType.Coins, 1000000);
            Debug.Log("👉 Cheat: Cộng 1,000,000 Coins thành công!");
        }

        if (GUILayout.Button("🔫 +1,000 Thẻ Súng (Phím H)", GUILayout.Height(40), GUILayout.Width(220)))
        {
            if (WeaponsController.Weapons != null)
            {
                foreach (var weapon in WeaponsController.Weapons)
                {
                    WeaponsController.AddCard(weapon, 1000);
                }
                Debug.Log("👉 Cheat: Cộng 1,000 thẻ súng cho mọi súng thành công!");
            }
        }

        if (GUILayout.Button("🔓 Unlock Toàn Bộ Súng (Phím V)", GUILayout.Height(40), GUILayout.Width(220)))
        {
            WeaponsController.UnlockAllWeaponsDev();
            Debug.Log("👉 Cheat: Mở khóa tất cả súng thành công!");
        }

        if (GUILayout.Button("⭐ Hack Cấp Độ 99 (Phím K)", GUILayout.Height(40), GUILayout.Width(220)))
        {
            ExperienceController.SetLevelDev(99);
            Debug.Log("👉 Cheat: Đặt Level thành 99 thành công!");
        }

        // --- HỆ THỐNG CHỌN MÀN CHƠI (LEVEL SELECTION) ---
        GUILayout.Space(10);
        GUILayout.Box("🗺️ CHỌN MÀN CHƠI (LEVEL TEST)", GUILayout.Width(220));

        // Dòng chọn World
        GUILayout.BeginHorizontal();
        GUILayout.Label("World: " + (selectedWorldIndex + 1), GUILayout.Width(100));
        if (GUILayout.Button("-", GUILayout.Width(55)))
        {
            selectedWorldIndex = Mathf.Max(0, selectedWorldIndex - 1);
            selectedLevelIndex = 0;
        }
        if (GUILayout.Button("+", GUILayout.Width(55)))
        {
            int maxWorlds = LevelController.LevelsDatabase != null ? LevelController.LevelsDatabase.GetWorldsAmount() : 1;
            selectedWorldIndex = Mathf.Min(maxWorlds - 1, selectedWorldIndex + 1);
            selectedLevelIndex = 0;
        }
        GUILayout.EndHorizontal();

        // Dòng chọn Level
        GUILayout.BeginHorizontal();
        GUILayout.Label("Level: " + (selectedLevelIndex + 1), GUILayout.Width(100));
        if (GUILayout.Button("-", GUILayout.Width(55)))
        {
            selectedLevelIndex = Mathf.Max(0, selectedLevelIndex - 1);
        }
        if (GUILayout.Button("+", GUILayout.Width(55)))
        {
            int maxLevels = 1;
            if (LevelController.LevelsDatabase != null)
            {
                var world = LevelController.LevelsDatabase.GetWorld(selectedWorldIndex);
                if (world != null && world.Levels != null)
                {
                    maxLevels = world.Levels.Length;
                }
            }
            selectedLevelIndex = Mathf.Min(maxLevels - 1, selectedLevelIndex + 1);
        }
        GUILayout.EndHorizontal();

        // Nút Load Màn Chơi đã chọn
        if (GUILayout.Button("🚀 LOAD LEVEL ĐÃ CHỌN", GUILayout.Height(35), GUILayout.Width(220)))
        {
            if (levelSave != null)
            {
                levelSave.WorldIndex = selectedWorldIndex;
                levelSave.LevelIndex = selectedLevelIndex;
                SaveController.Save(true);
                
                // Tải lại Scene hiện tại để cập nhật màn chơi mới
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                Debug.Log($"👉 Cheat: Đang tải World {selectedWorldIndex + 1} - Level {selectedLevelIndex + 1}...");
            }
        }

        // --- NÚT RESET SAVE & RESTART GAME ---
        GUILayout.Space(8);
        if (GUILayout.Button("🔄 RESET SAVE & RESTART", GUILayout.Height(35), GUILayout.Width(220)))
        {
            SaveController.DeleteSaveFile();
            PlayerPrefs.DeleteAll();
            
            // Reset các giá trị lựa chọn local
            selectedWorldIndex = 0;
            selectedLevelIndex = 0;

            // Load lại Scene hiện tại để bắt đầu lại game sạch sẽ
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            Debug.Log("👉 Cheat: Đã dọn sạch file Save và tải lại màn chơi từ đầu!");
        }

        GUILayout.EndArea();
    }
}

