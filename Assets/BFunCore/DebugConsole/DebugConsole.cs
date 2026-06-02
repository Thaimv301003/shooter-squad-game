// FILE: DebugConsole.cs
#if BFUN_INSTALLED_TRUE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BFunCoreKit;
using System.Collections.Concurrent;

namespace BFunCoreKit
{
    public class DebugConsole : Singleton<DebugConsole>
    {
        #region UI References
        [Header("Optimization Components")]
        [Tooltip("Kéo Component Canvas vào đây")]
        [SerializeField] private Canvas consoleCanvas;
        [Tooltip("Kéo Component CanvasGroup vào đây")]
        [SerializeField] private CanvasGroup consoleCanvasGroup;
        [Tooltip("Kéo Component GraphicRaycaster vào đây")]
        [SerializeField] private GraphicRaycaster consoleRaycaster;

        [Header("Inputs")]
        [SerializeField] private TMP_InputField commandInputField;

        [Header("Log UI")]
        [SerializeField] private RectTransform logContentArea;
        [SerializeField] private GameObject logEntryPrefab;
        [SerializeField] private ScrollRect logScrollRect;

        [Header("Filtering")]
        [SerializeField] private Toggle toggleLog;
        [SerializeField] private Toggle toggleWarning;
        [SerializeField] private Toggle toggleError;
        [SerializeField] private TMP_InputField searchInputField;

        [Header("Auto-Completion")]
        [SerializeField] private GameObject suggestionContainer;
        [SerializeField] private GameObject suggestionItemPrefab;

        [Header("Command History")]
        [Tooltip("Kéo HistoryContainer (có VerticalLayoutGroup) vào đây")]
        [SerializeField] private Transform historyContainer;
        [Tooltip("Kéo Button Prefab vào đây")]
        [SerializeField] private GameObject historyItemPrefab;
        [SerializeField] private int maxHistoryCount = 6;

        [Header("Settings")]
        [SerializeField] private int maxLogEntries = 200;
        private readonly ConcurrentQueue<LogEntry> _mainThreadLogQueue = new ConcurrentQueue<LogEntry>();
        #endregion

        #region Private Fields
        // Data
        private Queue<LogEntry> allLogEntries = new Queue<LogEntry>();
        private List<LogEntry> _filteredLogEntries = new List<LogEntry>();

        // Command Dictionary
        private Dictionary<string, MethodInfo> commands = new Dictionary<string, MethodInfo>();
        public IReadOnlyDictionary<string, MethodInfo> Commands => commands;

        // Pooling
        private Queue<LogEntryUI> availableUIPool = new Queue<LogEntryUI>();
        private List<LogEntryUI> activeUIList = new List<LogEntryUI>();

        // Auto-completion
        private List<GameObject> activeSuggestionItems = new List<GameObject>();
        private Queue<GameObject> suggestionItemPool = new Queue<GameObject>();

        // History Data
        private List<string> _commandHistory = new List<string>();
        private List<GameObject> _activeHistoryItems = new List<GameObject>();
        private Queue<GameObject> _historyItemPool = new Queue<GameObject>();

        // State
        private Coroutine _updateSuggestionsCoroutine;
        private Coroutine _scrollCoroutine;
        private bool _scrollRequested;
        private bool _showLog = true;
        private bool _showWarning = true;
        private bool _showError = true;
        private string _searchFilter = "";
        private static bool _isExecutingCommand = false;
        public static bool IsExecutingCommand => _isExecutingCommand;

        // Time Scale Logic
        private float _cachedTimeScale = 1f;

        // Layout Cache
        private VerticalLayoutGroup _layoutGroup;
        #endregion

        #region Unity Lifecycle
        public override void Awake()
        {
            base.Awake();

            // Lưu TimeScale gốc
            _cachedTimeScale = Time.timeScale;

            InitializePool();
            RegisterCommands();
            InitializeSuggestionPool();
            InitializeHistoryPool();
        }

        private void InitializeHistoryPool()
        {
            if (historyItemPrefab == null || historyContainer == null) return;
            for (int i = 0; i < maxHistoryCount; i++)
            {
                var newItem = Instantiate(historyItemPrefab, historyContainer);
                newItem.SetActive(false);
                _historyItemPool.Enqueue(newItem);
            }
        }

        private void Start()
        {
            if (consoleCanvas == null) consoleCanvas = GetComponent<Canvas>();
            if (consoleCanvasGroup == null) consoleCanvasGroup = GetComponent<CanvasGroup>();
            if (consoleRaycaster == null) consoleRaycaster = GetComponent<GraphicRaycaster>();

            if (logContentArea != null) _layoutGroup = logContentArea.GetComponent<VerticalLayoutGroup>();

            CloseConsoleImmediate();

            if (toggleLog != null) toggleLog.isOn = _showLog;
            if (toggleWarning != null) toggleWarning.isOn = _showWarning;
            if (toggleError != null) toggleError.isOn = _showError;

            var earlyLogs = DebugConsoleInit.FlushBuffer();
            foreach (var earlyLog in earlyLogs) EnqueueLog(earlyLog);

            ApplyFiltersOnly();
            RefreshHistoryUI();
        }

        private void OnEnable()
        {
            Application.logMessageReceivedThreaded += HandleLog;

            if (commandInputField != null)
            {
                commandInputField.onSubmit.AddListener(ProcessCommand);
                commandInputField.onValueChanged.AddListener(OnCommandInputChanged);
            }

            if (toggleLog) toggleLog.onValueChanged.AddListener(OnFilterChanged);
            if (toggleWarning) toggleWarning.onValueChanged.AddListener(OnFilterChanged);
            if (toggleError) toggleError.onValueChanged.AddListener(OnFilterChanged);
            if (searchInputField) searchInputField.onValueChanged.AddListener(OnSearchFilterChanged);
        }

        private void OnDisable()
        {
            Application.logMessageReceivedThreaded -= HandleLog;

            if (commandInputField != null)
            {
                commandInputField.onSubmit.RemoveListener(ProcessCommand);
                commandInputField.onValueChanged.RemoveListener(OnCommandInputChanged);
            }

            if (toggleLog) toggleLog.onValueChanged.RemoveListener(OnFilterChanged);
            if (toggleWarning) toggleWarning.onValueChanged.RemoveListener(OnFilterChanged);
            if (toggleError) toggleError.onValueChanged.RemoveListener(OnFilterChanged);
            if (searchInputField) searchInputField.onValueChanged.RemoveListener(OnSearchFilterChanged);
        }
        #endregion

        #region Toggle Logic

        public void ShowConsole()
        {
            if (consoleCanvas == null) return;
            if (!consoleCanvas.enabled) OpenConsole();
        }

        public void HideConsole()
        {
            if (consoleCanvas == null) return;
            if (consoleCanvas.enabled) CloseConsoleImmediate();
        }

        public void ToggleConsole()
        {
            if (consoleCanvas == null) return;
            bool isShowing = consoleCanvas.enabled;

            if (isShowing) CloseConsoleImmediate();
            else OpenConsole();
        }

        private void OpenConsole()
        {
            // Pause Game
            Time.timeScale = 0f;

            consoleCanvas.enabled = true;
            if (consoleRaycaster != null) consoleRaycaster.enabled = true;
            if (consoleCanvasGroup != null)
            {
                consoleCanvasGroup.alpha = 0.98f;
                consoleCanvasGroup.blocksRaycasts = true;
                consoleCanvasGroup.interactable = true;
            }

            SyncLogsToUI();
            RefreshHistoryUI();

#if UNITY_EDITOR || UNITY_STANDALONE
            if (commandInputField != null) commandInputField.ActivateInputField();
#endif

            TriggerScrollToBottom();
        }

        private void CloseConsoleImmediate()
        {
            // Resume Game
            Time.timeScale = _cachedTimeScale;

            HideSuggestions();

            if (consoleCanvasGroup != null)
            {
                consoleCanvasGroup.alpha = 0f;
                consoleCanvasGroup.blocksRaycasts = false;
                consoleCanvasGroup.interactable = false;
            }
            if (consoleRaycaster != null) consoleRaycaster.enabled = false;
            if (consoleCanvas != null) consoleCanvas.enabled = false;
        }

        public void ClearLogs()
        {
            allLogEntries.Clear();
            _filteredLogEntries.Clear();
            foreach (var activeUI in activeUIList)
            {
                activeUI.gameObject.SetActive(false);
                availableUIPool.Enqueue(activeUI);
            }
            activeUIList.Clear();
        }

        private void TriggerScrollToBottom()
        {
            _scrollRequested = true;
        }

        private void LateUpdate()
        {
            if (_scrollRequested && consoleCanvas.enabled)
            {
                _scrollRequested = false;
                if (_scrollCoroutine != null) StopCoroutine(_scrollCoroutine);
                _scrollCoroutine = StartCoroutine(ScrollToBottomRoutine());
            }
        }

        private IEnumerator ScrollToBottomRoutine()
        {
            yield return new WaitForEndOfFrame();
            yield return null;

            if (logScrollRect != null)
            {
                logScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        #endregion

        #region Smart Sync & Optimization Logic

        private void SyncLogsToUI()
        {
            if (_layoutGroup != null) _layoutGroup.enabled = false;

            int currentVisualCount = activeUIList.Count;
            int targetDataCount = _filteredLogEntries.Count;

            // Tối ưu: Chỉ tắt/bật những cái cần thiết thay vì xóa hết
            if (currentVisualCount > targetDataCount)
            {
                for (int i = currentVisualCount - 1; i >= targetDataCount; i--)
                {
                    var ui = activeUIList[i];
                    ui.gameObject.SetActive(false);
                    availableUIPool.Enqueue(ui);
                    activeUIList.RemoveAt(i);
                }
            }

            for (int i = 0; i < targetDataCount; i++)
            {
                LogEntryUI ui;
                if (i < activeUIList.Count)
                {
                    ui = activeUIList[i];
                }
                else
                {
                    ui = GetUIFromPool();
                    activeUIList.Add(ui);
                }
                ui.Setup(_filteredLogEntries[i]);
            }

            if (_layoutGroup != null)
            {
                _layoutGroup.enabled = true;
                // Chỉ rebuild nếu thực sự cần thiết, hoặc dùng Coroutine để delay 1 frame
                LayoutRebuilder.ForceRebuildLayoutImmediate(logContentArea);
            }

            TriggerScrollToBottom();
        }

        private void UpdateLogUI()
        {
            foreach (var ui in activeUIList)
            {
                ui.gameObject.SetActive(false);
                availableUIPool.Enqueue(ui);
            }
            activeUIList.Clear();

            foreach (var logData in _filteredLogEntries)
            {
                LogEntryUI uiToShow = GetUIFromPool();
                uiToShow.Setup(logData);
                activeUIList.Add(uiToShow);
            }
        }
        #endregion

        #region Core Log Logic
        private void InitializePool()
        {
            if (logEntryPrefab == null || logContentArea == null) return;
            for (int i = 0; i < Mathf.Min(20, maxLogEntries); i++)
            {
                var newUI = Instantiate(logEntryPrefab, logContentArea).GetComponent<LogEntryUI>();
                newUI.gameObject.SetActive(false);
                availableUIPool.Enqueue(newUI);
            }
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            // CHỈ làm duy nhất việc này. Không chạm vào bất cứ thứ gì khác.
            _mainThreadLogQueue.Enqueue(new LogEntry(logString, stackTrace, type));
        }

        private void Update()
        {
            // Lấy log ra và xử lý trên Main Thread
            while (_mainThreadLogQueue.TryDequeue(out var log))
            {
                ProcessLogEntry(log);
            }
        }

        private void ProcessLogEntry(LogEntry newLog)
        {
            // Mọi logic thêm vào List, check Filter, và Update UI Canvas 
            // đều phải nằm tập trung ở đây.
            EnqueueLog(newLog);

            if (DoesLogMatchFilters(newLog))
            {
                _filteredLogEntries.Add(newLog);

                if (consoleCanvas != null && consoleCanvas.enabled)
                {
                    AddSingleLogToUI(newLog);
                }
            }
        }

        public void HandleCommandLog(string logString)
        {
            // Đẩy vào queue để Main Thread (Update) xử lý chung một chỗ
            _mainThreadLogQueue.Enqueue(new LogEntry(logString, "", LogType.Log, true));
        }

        private void AddSingleLogToUI(LogEntry log)
        {
            if (activeUIList.Count >= maxLogEntries)
            {
                var recycledUI = activeUIList[0];
                activeUIList.RemoveAt(0);
                recycledUI.Setup(log);
                recycledUI.transform.SetAsLastSibling();
                activeUIList.Add(recycledUI);
            }
            else
            {
                var newUI = GetUIFromPool();
                newUI.Setup(log);
                activeUIList.Add(newUI);
            }

            TriggerScrollToBottom();
        }

        private void EnqueueLog(LogEntry logEntry)
        {
            allLogEntries.Enqueue(logEntry);
            if (allLogEntries.Count > maxLogEntries)
            {
                var removedLog = allLogEntries.Dequeue();
                if (_filteredLogEntries.Count > 0 && _filteredLogEntries[0] == removedLog)
                {
                    _filteredLogEntries.RemoveAt(0);
                }
            }
        }

        private LogEntryUI GetUIFromPool()
        {
            LogEntryUI ui;
            if (availableUIPool.Count > 0) ui = availableUIPool.Dequeue();
            else ui = Instantiate(logEntryPrefab, logContentArea).GetComponent<LogEntryUI>();

            ui.gameObject.SetActive(true);
            ui.transform.SetAsLastSibling();
            return ui;
        }

        private void ApplyFiltersOnly()
        {
            _filteredLogEntries.Clear();
            foreach (var logEntry in allLogEntries)
            {
                if (DoesLogMatchFilters(logEntry))
                    _filteredLogEntries.Add(logEntry);
            }
        }

        private void OnFilterChanged(bool isOn)
        {
            _showLog = toggleLog.isOn;
            _showWarning = toggleWarning.isOn;
            _showError = toggleError.isOn;
            ApplyFiltersOnly();
            if (consoleCanvas.enabled) SyncLogsToUI();
        }

        private void OnSearchFilterChanged(string newText)
        {
            _searchFilter = newText;
            ApplyFiltersOnly();
            if (consoleCanvas.enabled) SyncLogsToUI();
        }

        private bool DoesLogMatchFilters(LogEntry logEntry)
        {
            bool typeMatch = false;
            if (_showLog && (logEntry.Type == LogType.Log)) typeMatch = true;
            if (_showWarning && (logEntry.Type == LogType.Warning)) typeMatch = true;
            if (_showError && (logEntry.Type == LogType.Error || logEntry.Type == LogType.Exception || logEntry.Type == LogType.Assert)) typeMatch = true;
            if (!typeMatch) return false;
            return string.IsNullOrEmpty(_searchFilter) || logEntry.Message.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        #endregion

        #region Command Processing
        private void ProcessCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;
            HideSuggestions();

            // Tách lệnh trước để kiểm tra tồn tại
            var parts = Regex.Matches(input, @"[^\s""]+|""([^""]*)""")
                             .Cast<Match>()
                             .Select(m => m.Value.Trim('"'))
                             .ToArray();
            if (parts.Length == 0) return;

            string commandId = parts[0].ToLower();
            string[] args = parts.Skip(1).ToArray();

            HandleLog($"> {input}", "", LogType.Log);

            // Kiểm tra lệnh có tồn tại không
            if (commands.TryGetValue(commandId, out MethodInfo commandMethod))
            {
                // --- CHỈ LƯU HISTORY NẾU LỆNH TỒN TẠI (ĐÚNG CÚ PHÁP LỆNH) ---
                AddCommandToHistory(input);

                try
                {
                    _isExecutingCommand = true;
                    var parameters = commandMethod.GetParameters();
                    if (args.Length != parameters.Length)
                    {
                        HandleLog($"Error: Yêu cầu {parameters.Length} tham số, nhập {args.Length}.", "", LogType.Error);
                        return;
                    }

                    object[] convertedArgs = new object[args.Length];
                    for (int i = 0; i < args.Length; i++)
                    {
                        Type paramType = parameters[i].ParameterType;
                        try
                        {
                            if (paramType.IsEnum) convertedArgs[i] = Enum.Parse(paramType, args[i], true);
                            else convertedArgs[i] = Convert.ChangeType(args[i], paramType);
                        }
                        catch (FormatException)
                        {
                            HandleLog($"Sai kiểu dữ liệu tham số {i + 1}", "", LogType.Error);
                            return;
                        }
                    }
                    commandMethod.Invoke(null, convertedArgs);
                }
                catch (Exception e) { HandleLog($"Lỗi: {e.InnerException?.Message ?? e.Message}", "", LogType.Exception); }
                finally { _isExecutingCommand = false; }
            }
            else
            {
                // Nếu lệnh không tồn tại -> Báo lỗi và KHÔNG lưu vào History
                HandleLog($"Lệnh không tồn tại.", "", LogType.Error);
            }

            commandInputField.text = "";

#if UNITY_EDITOR || UNITY_STANDALONE
            commandInputField.ActivateInputField();
#else
            commandInputField.DeactivateInputField(); 
#endif

            TriggerScrollToBottom();
        }

        private void RegisterCommands()
        {
            commands.Clear();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var type in assembly.GetTypes())
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        var attr = method.GetCustomAttribute<DebugCommand>();
                        if (attr != null && !commands.ContainsKey(attr.CommandId.ToLower()))
                            commands.Add(attr.CommandId.ToLower(), method);
                    }
        }
        #endregion

        #region History Logic

        private void AddCommandToHistory(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return;

            // Xóa cũ để đưa lên đầu
            if (_commandHistory.Contains(cmd))
            {
                _commandHistory.Remove(cmd);
            }

            _commandHistory.Insert(0, cmd);

            if (_commandHistory.Count > maxHistoryCount)
            {
                _commandHistory.RemoveAt(_commandHistory.Count - 1);
            }

            RefreshHistoryUI();
        }

        private void RefreshHistoryUI()
        {
            if (historyContainer == null || historyItemPrefab == null) return;

            // Xóa item cũ về pool
            foreach (var item in _activeHistoryItems)
            {
                item.SetActive(false);
                _historyItemPool.Enqueue(item);
            }
            _activeHistoryItems.Clear();

            // Tạo item mới từ pool
            foreach (var cmd in _commandHistory)
            {
                GameObject item;
                if (_historyItemPool.Count > 0)
                {
                    item = _historyItemPool.Dequeue();
                }
                else
                {
                    item = Instantiate(historyItemPrefab, historyContainer);
                }

                item.SetActive(true);
                item.transform.SetAsLastSibling();

                var textComp = item.GetComponentInChildren<TMP_Text>();
                var btnComp = item.GetComponent<Button>();

                if (textComp != null) textComp.text = cmd;

                if (btnComp != null)
                {
                    btnComp.onClick.RemoveAllListeners();
                    string capturedCmd = cmd;
                    btnComp.onClick.AddListener(() => OnHistoryItemClicked(capturedCmd));
                }

                _activeHistoryItems.Add(item);
            }
        }

        private void OnHistoryItemClicked(string fullCommand)
        {
            if (commandInputField == null) return;

            var parts = fullCommand.Split(' ');
            if (parts.Length == 0) return;

            string cmdKey = parts[0].ToLower();
            string textToFill = fullCommand;

            // Nếu lệnh có tham số -> chỉ điền tên lệnh để người dùng nhập mới
            if (commands.TryGetValue(cmdKey, out MethodInfo methodInfo))
            {
                var parameters = methodInfo.GetParameters();
                if (parameters.Length > 0)
                {
                    textToFill = cmdKey + " ";
                }
            }

            commandInputField.text = textToFill;
            commandInputField.MoveTextEnd(false);
            commandInputField.ActivateInputField(); // Bắt buộc hiện bàn phím để nhập tiếp
        }

        #endregion

        #region Auto-Completion
        private void InitializeSuggestionPool()
        {
            if (suggestionItemPrefab == null || suggestionContainer == null) return;
            for (int i = 0; i < 10; i++)
            {
                var newItem = Instantiate(suggestionItemPrefab, suggestionContainer.transform);
                newItem.SetActive(false);
                suggestionItemPool.Enqueue(newItem);
            }
        }

        private void OnCommandInputChanged(string currentInput)
        {
            if (_updateSuggestionsCoroutine != null) StopCoroutine(_updateSuggestionsCoroutine);
            if (string.IsNullOrWhiteSpace(currentInput)) { HideSuggestions(); return; }
            _updateSuggestionsCoroutine = StartCoroutine(UpdateSuggestionsAfterFrame(currentInput));
        }

        private IEnumerator UpdateSuggestionsAfterFrame(string currentInput)
        {
            yield return null;
            var matches = commands.Keys.Where(k => k.StartsWith(currentInput, StringComparison.OrdinalIgnoreCase)).OrderBy(k => k).ToList();
            if (matches.Any()) UpdateSuggestionsUI(matches); else HideSuggestions();
        }

        private void UpdateSuggestionsUI(List<string> suggestions)
        {
            if (suggestionContainer == null) return;
            foreach (var item in activeSuggestionItems) { item.SetActive(false); suggestionItemPool.Enqueue(item); }
            activeSuggestionItems.Clear();
            foreach (var s in suggestions)
            {
                var item = GetSuggestionItemFromPool();
                var txt = item.GetComponentInChildren<TMP_Text>();
                if (txt) txt.text = s;
                var click = item.GetComponent<SuggestionItemClick>();
                if (click) { click.OnClicked = null; click.OnClicked += OnSuggestionClicked; }
                activeSuggestionItems.Add(item);
            }
            suggestionContainer.SetActive(true);
        }

        private void OnSuggestionClicked(string command)
        {
            commandInputField.text = command;
            commandInputField.MoveTextEnd(false);
            HideSuggestions();
            commandInputField.ActivateInputField();
        }

        private void HideSuggestions()
        {
            if (suggestionContainer == null) return;
            if (_updateSuggestionsCoroutine != null) StopCoroutine(_updateSuggestionsCoroutine);
            foreach (var item in activeSuggestionItems) { item.SetActive(false); suggestionItemPool.Enqueue(item); }
            activeSuggestionItems.Clear();
            suggestionContainer.SetActive(false);
        }

        private GameObject GetSuggestionItemFromPool()
        {
            GameObject item;
            if (suggestionItemPool.Count > 0) item = suggestionItemPool.Dequeue();
            else item = Instantiate(suggestionItemPrefab, suggestionContainer.transform);
            item.SetActive(true);
            return item;
        }
        #endregion
    }
}
#endif