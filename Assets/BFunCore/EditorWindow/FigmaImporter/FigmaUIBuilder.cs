#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using TMPro;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BFunCoreKit.Figma
{
    public static class FigmaUIBuilder
    {
        public static async void FetchCanvasList(string url, string token, GUISetting setting)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(token)) {
                 Debug.LogError("[FigmaImporter] Cần điền Figma URL và Token!");
                 return;
            }
            string fileId = ExtractFileId(url);
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Figma Importer", "Downloading Canvas List...", 0.5f);
#endif
            try
            {
                string json = await SendFigmaApiRequestAsync($"https://api.figma.com/v1/files/{fileId}?depth=2", token);
                FigmaFileResponse response = JsonConvert.DeserializeObject<FigmaFileResponse>(json);
                if (response?.Document != null && response.Document.Children != null) {
                    setting.availableCanvases.Clear();
                    setting.availableCanvases.Add("All Canvases");
                    foreach (var page in response.Document.Children) {
                        if (page.Type == "CANVAS" && page.Name.StartsWith("Canvas")) {
                            setting.availableCanvases.Add(page.Name.Trim());
                        }
                    }
                    setting.targetCanvasToImport = "All Canvases";
#if UNITY_EDITOR
                    EditorUtility.SetDirty(setting);
                    AssetDatabase.SaveAssets();
#endif
                    Debug.Log($"[FigmaImporter] Đã cập nhật xong {setting.availableCanvases.Count - 1} Canvases!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FigmaImporter] Error: {e.Message}");
            }
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }

        public static async Task ImportFigmaUI(string url, string token, string imageSavePath, string targetCanvas)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(token))
            {
                Debug.LogError("[FigmaImporter] Cần điền Figma URL và Token!");
                return;
            }

            string fileId = ExtractFileId(url);
            if (string.IsNullOrEmpty(fileId))
            {
                Debug.LogError("[FigmaImporter] Link Figma không hợp lệ.");
                return;
            }

#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Figma Importer", $"Downloading Figma Document ({targetCanvas})...", 0.1f);
#endif

            try
            {
                string json = await SendFigmaApiRequestAsync($"https://api.figma.com/v1/files/{fileId}", token);
                FigmaFileResponse response = JsonConvert.DeserializeObject<FigmaFileResponse>(json);
                
                if (response?.Document != null)
                {
                    FigmaNode filteredDoc = new FigmaNode { Children = new List<FigmaNode>() };
                    if (response.Document.Children != null)
                    {
                        foreach (var page in response.Document.Children)
                        {
                            if (page.Type == "CANVAS" && page.Name.StartsWith("Canvas"))
                            {
                                if (targetCanvas != "All Canvases" && page.Name.Trim() != targetCanvas) continue;
                                filteredDoc.Children.Add(page);
                            }
                        }
                    }

                    var spritesMap = await DownloadImagesAsync(fileId, token, filteredDoc, imageSavePath);
                    BuildUI(filteredDoc, null, spritesMap, response.Styles);
                }
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                EditorUtility.ClearProgressBar();
#endif
                Debug.LogError($"[FigmaImporter] Error downloading: {e.Message}");
                return;
            }

#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }

        private static async Task<string> SendFigmaApiRequestAsync(string url, string token)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("X-Figma-Token", token);
                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (request.responseCode == 429)
                {
#if UNITY_EDITOR
                    EditorUtility.ClearProgressBar();
#endif
                    throw new Exception("Figma đang giới hạn lượt tải (Lỗi 429 - Too Many Requests). Quá trình bị Cancel, vui lòng đợi vài phút rồi thử lại!");
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"HTTP Error {request.responseCode}: {request.error}");
                }

                return request.downloadHandler.text;
            }
        }

        private static async Task<Dictionary<string, Sprite>> DownloadImagesAsync(string fileId, string token, FigmaNode document, string basePath)
        {
            Dictionary<string, Sprite> result = new Dictionary<string, Sprite>();
            List<FigmaNode> imageNodes = new List<FigmaNode>();
            GatherImageNodes(document, imageNodes);

            if (imageNodes.Count == 0) return result;

            string idsParam = string.Join(",", imageNodes.Select(n => n.Id));

#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Figma Importer", "Fetching Image URLs...", 0.3f);
#endif

            try
            {
                string imgJson = await SendFigmaApiRequestAsync($"https://api.figma.com/v1/images/{fileId}?ids={idsParam}&format=png&scale=1", token);
                var imgResponse = JsonConvert.DeserializeObject<FigmaImageResponse>(imgJson);
                if (imgResponse != null && imgResponse.Images != null && string.IsNullOrEmpty(imgResponse.Error))
                {
                    if (!Directory.Exists(basePath))
                    {
                        Directory.CreateDirectory(basePath);
                    }

                    int index = 0;
                    int total = imgResponse.Images.Count;

                    foreach (var kvp in imgResponse.Images)
                    {
                        string nodeId = kvp.Key;
                        string imgUrl = kvp.Value;
                        if (string.IsNullOrEmpty(imgUrl)) continue;

                        FigmaNode node = imageNodes.FirstOrDefault(n => n.Id == nodeId);
                        string nodeName = node != null ? node.Name : "Img_" + nodeId.Replace(":", "_");

                        string safeName = string.Join("_", nodeName.Split(Path.GetInvalidFileNameChars()));

#if UNITY_EDITOR
                        EditorUtility.DisplayProgressBar("Figma Importer", $"Downloading Image {index + 1}/{total}: {safeName}", 0.3f + (0.5f * index / total));
#endif

                        using (UnityWebRequest dlReq = UnityWebRequestTexture.GetTexture(imgUrl))
                        {
                            var dlOp = dlReq.SendWebRequest();
                            while (!dlOp.isDone) await Task.Yield();

                            if (dlReq.result == UnityWebRequest.Result.Success)
                            {
                                string filePath = Path.Combine(basePath, safeName + ".png");
                                File.WriteAllBytes(filePath, dlReq.downloadHandler.data);
                            }
                        }
                        index++;
                    }

#if UNITY_EDITOR
                    AssetDatabase.Refresh();

                    foreach (var node in imageNodes)
                    {
                        string safeName = string.Join("_", node.Name.Split(Path.GetInvalidFileNameChars()));
                        string relativePath = basePath + "/" + safeName + ".png"; 

                        TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                        if (importer != null)
                        {
                            bool needSave = false;
                            if (importer.textureType != TextureImporterType.Sprite)
                            {
                                importer.textureType = TextureImporterType.Sprite;
                                needSave = true;
                            }
                            if (importer.spriteImportMode != SpriteImportMode.Single)
                            {
                                importer.spriteImportMode = SpriteImportMode.Single;
                                needSave = true;
                            }
                            if (importer.alphaIsTransparency != true)
                            {
                                importer.alphaIsTransparency = true;
                                needSave = true;
                            }

                            if (needSave)
                            {
                                importer.SaveAndReimport();
                            }

                            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
                            if (s != null)
                            {
                                result[node.Id] = s;
                            }
                        }
                    }
#endif
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FigmaImporter] Error downloading images: {e.Message}");
            }

            return result;
        }

        private static void GatherImageNodes(FigmaNode node, List<FigmaNode> outNodes)
        {
            if (node == null) return;
            if (node.Name.StartsWith("Img_") || node.Name.StartsWith("Bg_") || node.Name.StartsWith("Btn_"))
            {
                outNodes.Add(node);
            }
            if (node.Children != null)
            {
                foreach (var child in node.Children) GatherImageNodes(child, outNodes);
            }
        }

        private static string ExtractFileId(string url)
        {
            var parts = url.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                if ((parts[i] == "file" || parts[i] == "design") && i + 1 < parts.Length)
                {
                    return parts[i + 1];
                }
            }
            return null;
        }

        private static GUIManager FindGUIManager()
        {
#if UNITY_EDITOR
            GUIManager manager = UnityEngine.Object.FindObjectOfType<GUIManager>();
            if (manager != null) return manager;

            string[] guids = AssetDatabase.FindAssets("t:Prefab GUIManager");
            foreach (string guid in guids)
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (go != null && go.GetComponent<GUIManager>() != null) return go.GetComponent<GUIManager>();
            }
            
            guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("GUI") || path.Contains("Manager"))
                {
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null)
                    {
                        manager = go.GetComponent<GUIManager>();
                        if (manager != null) return manager;
                    }
                }
            }
#endif
            return null;
        }

        private static void BuildUI(FigmaNode document, TMP_FontAsset defaultFont, Dictionary<string, Sprite> spritesMap, Dictionary<string, FigmaStyleDef> responseStyles)
        {            
            if (document.Children == null) return;

            int processedCanvases = 0;
            GUIManager manager = FindGUIManager();

            foreach (var pageNode in document.Children)
            {
                if (pageNode.Type == "CANVAS" && pageNode.Name.StartsWith("Canvas"))
                {
                    string canvasName = pageNode.Name.Trim();
                    string prefabPath = GlobalConst.CanvasFolder + "/" + canvasName + ".prefab";

#if UNITY_EDITOR
                    GameObject checkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (checkPrefab == null)
                    {
                        Debug.LogWarning($"[FigmaImporter] Found page '{canvasName}' but prefab does not exist at {prefabPath}. Skipping.");
                        continue;
                    }

                    GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                    if (prefabRoot != null)
                    {
                        UIPanel uiPanel = prefabRoot.GetComponentInChildren<UIPanel>(true);
                        if (uiPanel != null)
                        {
                            if (pageNode.Children != null)
                            {
                                foreach (var panelNode in pageNode.Children)
                                {
                                    if (panelNode.Name.StartsWith("Panel_") && (panelNode.Type == "FRAME" || panelNode.Type == "COMPONENT" || panelNode.Type == "SECTION" || panelNode.Type == "GROUP"))
                                    {
                                        ProcessFigmaPanel(panelNode, uiPanel, defaultFont, spritesMap, responseStyles, manager);
                                    }
                                }
                            }
                            
                            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                            processedCanvases++;
                        }
                        else
                        {
                            Debug.LogError($"[FigmaImporter] Prefab {canvasName} missing UIPanel component!");
                        }
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                    }
#endif
                }
            }

#if UNITY_EDITOR
            if (processedCanvases > 0)
            {
                EditorUtility.DisplayDialog("Figma Importer", $"Đã Import Thành Công vào {processedCanvases} Canvas!\nẢnh được lưu tại: {GlobalConst.CanvasFolder}", "OK");
                AssetDatabase.Refresh();
            }
            else
            {
                EditorUtility.DisplayDialog("Figma Importer", "Không tìm thấy Page nào đúng chuẩn tên Canvas* trong Figma file hiện tại.", "OK");
            }
#endif
        }

        private static void ProcessFigmaPanel(FigmaNode panelNode, UIPanel uiPanel, TMP_FontAsset defaultFont, Dictionary<string, Sprite> spritesMap, Dictionary<string, FigmaStyleDef> responseStyles, GUIManager guiManager)
        {
            string unityPanelName = $"---------> {panelNode.Name} <---------";
            
            Transform existingPanel = uiPanel.transform.Find(unityPanelName);
            if (existingPanel != null)
            {
                GameObject.DestroyImmediate(existingPanel.gameObject);
            }
            
            GameObject panelGo = null;
#if UNITY_EDITOR
            var fInfo = typeof(UIPanel).GetField("basePanelPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            GameObject basePrefab = fInfo?.GetValue(uiPanel) as GameObject;

            if (basePrefab != null)
            {
                panelGo = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, uiPanel.transform);
                panelGo.name = unityPanelName;
            }
#endif
            if (panelGo == null)
            {
                panelGo = new GameObject(unityPanelName);
                panelGo.transform.SetParent(uiPanel.transform, false);
                panelGo.AddComponent<Panel>();
            }

            RectTransform rt = panelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            BFunHelper.SetRectTop(rt, 0);
            BFunHelper.SetRectBottom(rt, 0);
            
            int siblingIndex = rt.GetSiblingIndex();
            int valuePos = 2500 * (siblingIndex + 1);
            BFunHelper.SetRectRight(rt, -valuePos);
            BFunHelper.SetRectLeft(rt, valuePos);

            if (panelNode.Children != null)
            {
                foreach (var child in panelNode.Children)
                {
                    CreateNodeGameObject(child, rt, defaultFont, spritesMap, responseStyles, panelNode.AbsoluteBoundingBox, guiManager);
                }
            }
        }

        private static void CreateNodeGameObject(FigmaNode node, Transform parent, TMP_FontAsset defaultFont, Dictionary<string, Sprite> spritesMap, Dictionary<string, FigmaStyleDef> responseStyles, FigmaBoundingBox parentBox, GUIManager guiManager)
        {
            GameObject go = null;

#if UNITY_EDITOR
            if (node.Name.StartsWith("Btn_") && guiManager != null && guiManager.buttonPrefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(guiManager.buttonPrefab, parent);
                go.name = node.Name;
            }
            else if ((node.Name.StartsWith("Txt_") || node.Type == "TEXT") && guiManager != null && guiManager.textPrefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(guiManager.textPrefab, parent);
                go.name = node.Name;
            }
#endif
            if (go == null)
            {
                go = new GameObject(node.Name);
                go.transform.SetParent(parent, false);
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();

            SetupRectTransform(rt, node, parentBox);
            SetupComponents(go, node, defaultFont, spritesMap, responseStyles);
            SetupAutoLayout(go, node);

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    CreateNodeGameObject(child, rt, defaultFont, spritesMap, responseStyles, node.AbsoluteBoundingBox, guiManager);
                }
            }
        }

        private static void SetupRectTransform(RectTransform rt, FigmaNode node, FigmaBoundingBox parentBox)
        {
            if (node.AbsoluteBoundingBox == null) return;

            float width = node.AbsoluteBoundingBox.Width;
            float height = node.AbsoluteBoundingBox.Height;
            float x = node.AbsoluteBoundingBox.X;
            float y = node.AbsoluteBoundingBox.Y;

            rt.sizeDelta = new Vector2(width, height);
            
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);

            float relX = parentBox != null ? x - parentBox.X : 0;
            float relY = parentBox != null ? y - parentBox.Y : 0;
            
            rt.anchoredPosition = new Vector2(relX + width * 0.5f, -(relY + height * 0.5f));

            if (node.Constraints != null && parentBox != null && parentBox.Width > 0 && parentBox.Height > 0)
            {
                Vector2 anchorMin = new Vector2(0, 1);
                Vector2 anchorMax = new Vector2(0, 1);

                string h = node.Constraints.Horizontal;
                if (h == "RIGHT") { anchorMin.x = 1; anchorMax.x = 1; }
                else if (h == "CENTER") { anchorMin.x = 0.5f; anchorMax.x = 0.5f; }
                else if (h == "LEFT_RIGHT" || h == "LEFT_AND_RIGHT" || h == "SCALE") { anchorMin.x = 0; anchorMax.x = 1; }

                string v = node.Constraints.Vertical;
                if (v == "BOTTOM") { anchorMin.y = 0; anchorMax.y = 0; }
                else if (v == "CENTER") { anchorMin.y = 0.5f; anchorMax.y = 0.5f; }
                else if (v == "TOP_BOTTOM" || v == "TOP_AND_BOTTOM" || v == "SCALE") { anchorMin.y = 0; anchorMax.y = 1; }

                if (anchorMin != new Vector2(0, 1) || anchorMax != new Vector2(0, 1))
                {
                    Vector2 parentSize = new Vector2(parentBox.Width, parentBox.Height);
                    Vector2 offsetMin = rt.offsetMin;
                    Vector2 offsetMax = rt.offsetMax;
                    
                    Vector2 anchorDiffMin = anchorMin - new Vector2(0, 1);
                    Vector2 anchorDiffMax = anchorMax - new Vector2(0, 1);
                    
                    rt.anchorMin = anchorMin;
                    rt.anchorMax = anchorMax;
                    
                    rt.offsetMin = offsetMin - new Vector2(parentSize.x * anchorDiffMin.x, parentSize.y * anchorDiffMin.y);
                    rt.offsetMax = offsetMax - new Vector2(parentSize.x * anchorDiffMax.x, parentSize.y * anchorDiffMax.y);
                }
            }
        }

        private static void ApplyColorBinder(GUIColorBinder binder, Dictionary<string, string> nodeStyles, string styleKey, Color fallbackColor, Dictionary<string, FigmaStyleDef> responseStyles)
        {
            bool foundColorStyle = false;
            if (nodeStyles != null && nodeStyles.TryGetValue(styleKey, out string colorStyleId))
            {
                if (responseStyles != null && responseStyles.TryGetValue(colorStyleId, out FigmaStyleDef styleDef))
                {
                    var colorTypeField = binder.GetType().GetField("colorType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (colorTypeField != null)
                    {
                        try 
                        {
                            object enumVal = Enum.Parse(colorTypeField.FieldType, styleDef.Name, true);
                            colorTypeField.SetValue(binder, enumVal);
                            foundColorStyle = true;
                        } 
                        catch {}
                    }
                }
            }
            
            if (!foundColorStyle)
            {
                var colorTypeField = binder.GetType().GetField("colorType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (colorTypeField != null)
                {
                    try 
                    {
                        object enumDefault = Enum.Parse(colorTypeField.FieldType, "Default", true);
                        colorTypeField.SetValue(binder, enumDefault);
                    } 
                    catch {}
                }

                var initColorField = binder.GetType().GetField("initColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (initColorField != null)
                {
                    initColorField.SetValue(binder, fallbackColor);
                }
            }
        }

        private static void SetupComponents(GameObject go, FigmaNode node, TMP_FontAsset defaultFont, Dictionary<string, Sprite> spritesMap, Dictionary<string, FigmaStyleDef> responseStyles)
        {
            if (node.Name.StartsWith("Img_") || node.Name.StartsWith("Bg_") || node.Type == "RECTANGLE" || node.Type == "VECTOR" || node.Type == "FRAME")
            {
                Image img = go.GetComponent<Image>();
                if (img == null) img = go.AddComponent<Image>();
                
                img.color = GetColorFromFills(node.Fills, Color.white);
                img.raycastTarget = false;

                GUIColorBinder imgColorBinder = go.GetComponent<GUIColorBinder>();
                if (imgColorBinder != null) 
                {
                    ApplyColorBinder(imgColorBinder, node.Styles, "fill", img.color, responseStyles);
                }
                
                if (spritesMap != null && spritesMap.ContainsKey(node.Id))
                {
                    img.sprite = spritesMap[node.Id];
                    img.color = Color.white; 
                }
            }

            if (node.Name.StartsWith("Btn_"))
            {
                Image img = go.GetComponent<Image>();
                if (img == null)
                {
                    img = go.AddComponent<Image>();
                    img.color = new Color(1, 1, 1, 0); 
                }
                
                if (spritesMap != null && spritesMap.ContainsKey(node.Id))
                {
                    img.sprite = spritesMap[node.Id];
                    img.color = Color.white;
                }
                
                img.raycastTarget = true;

                if (go.GetComponent<Button>() == null) go.AddComponent<Button>();
                if (go.GetComponent<GUIButton>() == null) go.AddComponent<GUIButton>();
            }

            if (node.Name.StartsWith("Txt_") || node.Type == "TEXT")
            {
                TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
                if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = node.Characters;
                if (defaultFont != null && tmp.font == null) tmp.font = defaultFont;
                
                tmp.raycastTarget = false; 

                // Removed automatic stretch full code since it breaks text positioning

                GUITextBinder textBinder = go.GetComponent<GUITextBinder>();
                if (textBinder != null)
                {
                    bool foundTextStyle = false;
                    if (node.Styles != null && node.Styles.TryGetValue("text", out string textStyleId))
                    {
                        if (responseStyles != null && responseStyles.TryGetValue(textStyleId, out FigmaStyleDef styleDef))
                        {
                            var textTypeField = textBinder.GetType().GetField("textType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (textTypeField != null)
                            {
                                try 
                                {
                                    object enumVal = Enum.Parse(textTypeField.FieldType, styleDef.Name, true);
                                    textTypeField.SetValue(textBinder, enumVal);
                                    foundTextStyle = true;
                                } 
                                catch {}
                            }
                        }
                    }
                    
                    if (!foundTextStyle)
                    {
                        textBinder.ignoreFont = true;
                        textBinder.ignoreFontSize = true;
                        textBinder.ignoreFontStyle = true;
                    }
                }

                if (node.Style != null)
                {
                    tmp.fontSize = node.Style.FontSize;
                    Color c = GetColorFromFills(node.Style.Fills, Color.black);
                    
                    GUIColorBinder tmpColorBinder = go.GetComponent<GUIColorBinder>();
                    if (tmpColorBinder != null) 
                    {
                        ApplyColorBinder(tmpColorBinder, node.Styles, "fill", c, responseStyles);
                    } 
                    else 
                    {
                        tmp.color = c;
                    }

                    if (node.Style.FontWeight >= 700) tmp.fontStyle |= FontStyles.Bold;
                    if (node.Style.Italic) tmp.fontStyle |= FontStyles.Italic;
                    
                    string h = node.Style.TextAlignHorizontal ?? "LEFT";
                    string v = node.Style.TextAlignVertical ?? "CENTER";

                    if (v == "TOP")
                    {
                        if (h == "RIGHT") tmp.alignment = TextAlignmentOptions.TopRight;
                        else if (h == "CENTER") tmp.alignment = TextAlignmentOptions.Top;
                        else tmp.alignment = TextAlignmentOptions.TopLeft;
                    }
                    else if (v == "BOTTOM")
                    {
                        if (h == "RIGHT") tmp.alignment = TextAlignmentOptions.BottomRight;
                        else if (h == "CENTER") tmp.alignment = TextAlignmentOptions.Bottom;
                        else tmp.alignment = TextAlignmentOptions.BottomLeft;
                    }
                    else
                    {
                        if (h == "RIGHT") tmp.alignment = TextAlignmentOptions.Right;
                        else if (h == "CENTER") tmp.alignment = TextAlignmentOptions.Center;
                        else tmp.alignment = TextAlignmentOptions.Left;
                    }
                }

                tmp.enableWordWrapping = false;
                tmp.overflowMode = TextOverflowModes.Overflow;
            }
        }

        private static void SetupAutoLayout(GameObject go, FigmaNode node)
        {
            if (string.IsNullOrEmpty(node.LayoutMode) || node.LayoutMode == "NONE") return;

            HorizontalOrVerticalLayoutGroup layoutGroup = null;

            if (node.LayoutMode == "HORIZONTAL")
                layoutGroup = go.AddComponent<HorizontalLayoutGroup>();
            else if (node.LayoutMode == "VERTICAL")
                layoutGroup = go.AddComponent<VerticalLayoutGroup>();

            if (layoutGroup != null)
            {
                layoutGroup.spacing = node.ItemSpacing;
                layoutGroup.padding = new RectOffset(
                    (int)node.PaddingLeft,
                    (int)node.PaddingRight,
                    (int)node.PaddingTop,
                    (int)node.PaddingBottom
                );
                layoutGroup.childControlWidth = false;
                layoutGroup.childControlHeight = false;
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childForceExpandHeight = false;
            }
        }

        private static Color GetColorFromFills(System.Collections.Generic.List<FigmaPaint> fills, Color fallback)
        {
            if (fills == null || fills.Count == 0) return fallback;
            foreach (var f in fills)
            {
                if (f.Type == "SOLID" && f.Color != null && (f.Visible ?? true))
                {
                    return new Color(f.Color.R, f.Color.G, f.Color.B, f.Color.A);
                }
            }
            return fallback;
        }
    }
}
#endif
