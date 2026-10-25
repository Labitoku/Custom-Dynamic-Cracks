#if UNITY_EDITOR_WIN
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using Debug = UnityEngine.Debug;
using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

namespace CleanEditorLayout
{
    public class Toolbar : EditorWindow
    {
        #region variables
            private static float spaceWidth = 2f;
            private static bool initializeDropdowns;
        #endregion

        private void Awake() {
            // on startup/layout change
            CleanEditorLayout.fixOnUpdate = true; // fix fullscreen window mode and toolbar scaling in next OnUpdate
            initializeDropdowns = true;
        }
        private void OnGUI() {
                // variables
                var cleanEditorMainWindow = CleanEditorLayout.GetCleanEditorWindow();
                var mainWindow = CleanEditorLayout.GetMainWindow();
                var defaultbackgroundColor = GUI.backgroundColor;
                var style = new GUIStyle(GUI.skin.label) { fixedWidth = 0, stretchWidth = false, fontSize = 12 };
                var buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = 12, margin = new RectOffset(0, 0, 0, 0) };
                var dropDownRect = new Rect(0, 0, position.width, position.height);
                var currentMonitor = Win32.MonitorFromWindow(cleanEditorMainWindow, Win32.MONITOR_DEFAULTTONEAREST);
                var dpi = Win32.GetDpiForMonitor(currentMonitor, Win32.MDT_EFFECTIVE_DPI, out uint dpiX, out uint dpiY);
                var editorScaling = EditorGUIUtility.pixelsPerPoint;
                var uiScaling = editorScaling / (dpiX / (float)96);
                var screenWidth = Win32.GetMonitorSize(currentMonitor).x / editorScaling;
                var toolbarHeight = Win32.GetSystemMetricsForDpi(Win32.SM_CYCAPTION, dpiX) / editorScaling;
                var monitorPosition = (Vector2)Win32.GetMonitorPosition(currentMonitor) / editorScaling;
                var mouseZeroFix = false;
                var buttonWidth = 0f;
                var currentOffset = 0f;
                var dropdownFontSize = Preferences.defaultMenuButtonFontSize;
                var dropdownPadding = Preferences.defaultMenuButtonPadding;

            // draw dropdown section ------------------------------------------------
                EditorGUI.DrawRect(dropDownRect, Preferences.GetToolbarColor());
                GUI.backgroundColor = Preferences.GetButtonColor();
                GUILayout.BeginHorizontal();
                GUILayout.Space(spaceWidth);
                currentOffset += spaceWidth;
                var dropdowns = Preferences.LoadDropdownEntries(Preferences.DropdownsListKey, initializeDropdowns);
                initializeDropdowns = false;
                var dropdownOverflow = DropdownOverflowSettings(dropdowns, style, screenWidth, uiScaling);
                dropdownPadding = Preferences.MenuButtonPadding;
                if(dropdownOverflow)
                    dropdownFontSize = Preferences.MenuButtonFontSize;
                var dropdownStyle = new GUIStyle(buttonStyle) { fontSize = dropdownFontSize };
                for(int i = 0; i < dropdowns.Count; i++) {
                    if(!dropdowns[i].enabled || dropdowns[i].notInProject) continue;
                    buttonWidth = (int)(style.CalcSize(new GUIContent(dropdowns[i].categoryName)).x+dropdownPadding);
                    currentOffset += buttonWidth;
                    var lastRect = GUILayoutUtility.GetLastRect();
                    var dropDownPosition = new Vector2(lastRect.x+monitorPosition.x, lastRect.y+monitorPosition.y+toolbarHeight);
                    var buttonPressed = GUILayout.Button(dropdowns[i].categoryName, dropdownStyle, GUILayout.Width(buttonWidth), GUILayout.Height(toolbarHeight));
                    if(buttonPressed || CleanEditorLayout.dropDownActive && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) && !mouseZeroFix) {
                        CleanEditorLayout.dropDownActive = true;
                        mouseZeroFix = true;
                        Dropdown.ShowDropdown(dropdowns[i].categoryName, dropdowns[i].subMenuEntries, dropDownPosition, buttonPressed);
                    }
                    else if(Shortcuts.shortcutActive && Shortcuts.currentShortcut == dropdowns[i].categoryName && lastRect.width != 1) {
                        CleanEditorLayout.dropDownActive = true;
                        mouseZeroFix = true;
                        Dropdown.ShowDropdown(Shortcuts.currentShortcut, dropdowns[i].subMenuEntries, dropDownPosition, buttonPressed);
                        Shortcuts.shortcutActive = false;
                    }
                    GUILayout.Space(spaceWidth);
                    currentOffset += spaceWidth;
                }


            // draw first customizable section ---------------------------------------
                int playButtonType = Preferences.PlayButtonDesignType;
                var playButtonWidth = playButtonType == 0 ? (int)(Preferences.PlayButtonWidthDefault/uiScaling) : (int)(Preferences.PlayButtonWidthSimple/uiScaling);
                var leftButtonEntries = Preferences.LoadButtonEntries(Preferences.LeftButtonsListKey);
                DrawCustomButtons(leftButtonEntries, buttonStyle, spaceWidth, toolbarHeight, ref currentOffset, true, screenWidth, playButtonWidth);


            // draw play button section ----------------------------------------------
                if(playButtonType == 0) { // default play button design

                    // play button
                    if(EditorApplication.isPlaying || !Preferences.PlaymodeIconFix) {
                        var keepHighlighted = true;
                        keepHighlighted = GUILayout.Toggle(keepHighlighted, EditorGUIUtility.IconContent("PlayButton On"), buttonStyle, GUILayout.Width((int)(playButtonWidth/3-(spaceWidth*0.66f))), GUILayout.Height(toolbarHeight));
                        if(!keepHighlighted) {
                            Preferences.PlaymodeIconFix = true;
                            ExitPlaymode();
                        }
                    }
                    else {
                        if(GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("PlayButton").image, "Play"), buttonStyle, GUILayout.Width((int)(playButtonWidth/3-(spaceWidth*0.66f))), GUILayout.Height(toolbarHeight))) {
                            Preferences.PlaymodeIconFix = false;
                            EnterPlaymode();
                        }
                    }
                    
                    // pause button
                    GUILayout.Space(spaceWidth);
                    var buttonIconName = !EditorApplication.isPlaying && Preferences.PlaymodeIconFix ? "PauseButton" : "PauseButton On";
                    EditorApplication.isPaused = GUILayout.Toggle(EditorApplication.isPaused, EditorGUIUtility.IconContent(buttonIconName), buttonStyle, GUILayout.Width((int)(playButtonWidth/3-(spaceWidth*0.66f))), GUILayout.Height(toolbarHeight));
                    if(EditorApplication.isPaused) Resume();

                    // step button
                    GUILayout.Space(spaceWidth);
                    if(!EditorApplication.isPlaying)
                        GUI.enabled = false;
                    if(GUILayout.Button(EditorGUIUtility.IconContent("StepButton On"), buttonStyle, GUILayout.Width((int)(playButtonWidth/3-(spaceWidth*0.66f))), GUILayout.Height(toolbarHeight))) {
                        EditorApplication.delayCall += () => EditorApplication.Step();
                        if(!EditorApplication.isPaused)
                            EditorApplication.isPaused = true;
                    }
                    GUI.enabled = true;
                }
                else if(playButtonType == 1) { // simple play button design

                    // play button
                    if(EditorApplication.isPlaying || !Preferences.PlaymodeIconFix) {
                        GUIContent guiContent;
                        if(Preferences.DisplayModeType == 0)
                            guiContent = new GUIContent("Stop");
                        else if(Preferences.DisplayModeType == 1)
                            guiContent = EditorGUIUtility.IconContent("PlayButton On");
                        else
                            guiContent = new GUIContent("Stop", EditorGUIUtility.IconContent("PlayButton On").image);

                        if(Preferences.HighlightStop) {
                            var keepHighlighted = true;
                            keepHighlighted = GUILayout.Toggle(keepHighlighted, guiContent, buttonStyle, GUILayout.Width(playButtonWidth), GUILayout.Height(toolbarHeight));
                            if(!keepHighlighted) {
                                Preferences.PlaymodeIconFix = true;
                                ExitPlaymode();
                            }
                        }
                        else {
                            if(GUILayout.Button(guiContent, buttonStyle, GUILayout.Width(playButtonWidth), GUILayout.Height(toolbarHeight))) {
                                Preferences.PlaymodeIconFix = true;
                                EditorApplication.ExitPlaymode();
                            }
                        }
                    }
                    else {
                        GUIContent guiContent;
                        if(Preferences.DisplayModeType == 0)
                            guiContent = new GUIContent("Play");
                        else if(Preferences.DisplayModeType == 1)
                            guiContent = EditorGUIUtility.IconContent("PlayButton");
                        else
                            guiContent = new GUIContent("Play", EditorGUIUtility.IconContent("PlayButton").image);
                            
                        if(GUILayout.Button(guiContent, buttonStyle, GUILayout.Width(playButtonWidth), GUILayout.Height(toolbarHeight))) {
                            Preferences.PlaymodeIconFix = false;
                            EnterPlaymode();
                        }
                    }
                }
                GUILayout.Space(spaceWidth);


            // draw second customizable section --------------------------------------
                var rightButtonEntries = Preferences.LoadButtonEntries(Preferences.RightButtonsListKey);
                DrawCustomButtons(rightButtonEntries, buttonStyle, spaceWidth, toolbarHeight, ref currentOffset);
            

            // draw window button section --------------------------------------------
                buttonWidth = (int)(45/uiScaling);
                buttonStyle.fontSize = 18;
                if(GUILayout.Button("-", buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(toolbarHeight))) MinimizeEditor();
                GUILayout.Space(spaceWidth);
                buttonStyle.fontSize = 12;
                if(GUILayout.Button("❐", buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(toolbarHeight))) SwitchMode(false);
                GUILayout.Space(spaceWidth);
                buttonStyle.fontSize = 18;
                buttonStyle.contentOffset = new Vector2(0, -1f);
                if(GUILayout.Button("×", buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(toolbarHeight))) QuitEditor();
                GUILayout.EndHorizontal();
                GUI.backgroundColor = defaultbackgroundColor;

            // double click windowed mode
                Event e = Event.current;
                if(e.type == EventType.MouseDown && e.button == 0)
                    if(e.clickCount == 2)
                        SwitchMode(false);

            // minor fix
                if(HasOpenInstances<Dropdown>())
                    Repaint();
        
        }

        public static void OpenToolbar() {
            CloseToolbar();
            var toolbar = CreateInstance<Toolbar>();
            toolbar.ShowPopup();
            CleanEditorLayout.GetEditorSubWindows();
            var cleanEditorMainWindow = CleanEditorLayout.GetCleanEditorWindow();
            var toolbarWindow = CleanEditorLayout.GetToolbarWindow();
            var currentMonitor = Win32.MonitorFromWindow(cleanEditorMainWindow, Win32.MONITOR_DEFAULTTONEAREST);
            var primaryMonitor = Win32.MonitorFromWindow(IntPtr.Zero, Win32.MONITOR_DEFAULTTOPRIMARY);
            Win32.GetDpiForMonitor(currentMonitor, Win32.MDT_EFFECTIVE_DPI, out uint dpiCurrent, out uint dpiY);
            Win32.GetDpiForMonitor(primaryMonitor, Win32.MDT_EFFECTIVE_DPI, out uint dpiPrimary, out uint dpiY2);
            var currentScaling = dpiCurrent / 96f;
            var primaryScaling = dpiPrimary / 96f;
            var editorScaling = EditorGUIUtility.pixelsPerPoint;
            float uiScaling;
            if(Mathf.Round(currentScaling / primaryScaling * 100f) == Mathf.Round(editorScaling * 100f)
            || Mathf.Round(primaryScaling / currentScaling * 100f) == Mathf.Round(editorScaling * 100f))
                uiScaling = editorScaling;
            else
                uiScaling = currentScaling / primaryScaling * editorScaling;
            var toolbarHeight = Win32.GetSystemMetricsForDpi(Win32.SM_CYCAPTION, dpiCurrent) / uiScaling;
            var width = Win32.GetMonitorSize(currentMonitor).x / uiScaling;
            toolbar.minSize = new Vector2(width, toolbarHeight);
            toolbar.maxSize = new Vector2(width, toolbarHeight);
            var monitorPosition = (Vector2)Win32.GetMonitorPosition(currentMonitor);
            Win32.SetWindowPos(toolbarWindow, IntPtr.Zero, (int)monitorPosition.x, (int)monitorPosition.y, 0, 0, Win32.SWP_NOZORDER | Win32.SWP_NOSIZE);  // move toolbar to active screen
        }
        public static void CloseToolbar() {
            #if UNITY_6000_3_OR_NEWER
                if(HasOpenInstances<Toolbar>())
                    foreach(var toolbar in Resources.FindObjectsOfTypeAll<Toolbar>())
                        toolbar.Close();
            #else
                if(HasOpenInstances<Toolbar>())
                    GetWindow<Toolbar>().Close();
            #endif
        }
        private static void DrawCustomButtons(List<Preferences.ButtonEntryData> buttonEntries, GUIStyle buttonStyle, float spaceWidth, float toolbarHeight, ref float currentOffset, bool isLeftButtons = false, float screenWidth = 0, float playButtonWidth = 0) {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12, margin = new RectOffset(0, 0, 0, 0), normal = { textColor = GUI.skin.button.normal.textColor }, hover = { textColor = GUI.skin.button.normal.textColor } };
            
            // alignment if no buttons
            if(buttonEntries.Count == 0) {
                if(!isLeftButtons)
                    GUILayout.FlexibleSpace();
                else
                    GUILayout.Space((int)(screenWidth/2-(currentOffset+playButtonWidth/2)));
                return;
            }

            // reorder in order left, center, right
            var sortedButtonEntries = new List<Preferences.ButtonEntryData>();
            buttonEntries.Sort((a, b) => GetSortOrder(a.alignment).CompareTo(GetSortOrder(b.alignment)));
            bool hasCenter = false, hasRight = false;
            bool leftDone = false, rightDone = false;
            float widthLeft = 0, widthCentered = 0, widthRight = 0;
            int GetSortOrder(Preferences.Alignment alignment) {
                if(alignment == Preferences.Alignment.Left) return 0;
                if(alignment == Preferences.Alignment.Center) { hasCenter = true; return 1; }
                if(alignment == Preferences.Alignment.Right) { hasRight = true; return 2; }
                return 0;
            }
            for(int i = 0; i < buttonEntries.Count; i++) {
                if(buttonEntries[i].alignment == Preferences.Alignment.Left && buttonEntries[i].enabled) widthLeft += buttonEntries[i].width+spaceWidth;
                if(buttonEntries[i].alignment == Preferences.Alignment.Center && buttonEntries[i].enabled) widthCentered += buttonEntries[i].width+spaceWidth;
                if(buttonEntries[i].alignment == Preferences.Alignment.Right && buttonEntries[i].enabled) widthRight += buttonEntries[i].width+spaceWidth;
            }
            float placedButtonsLength = currentOffset+widthLeft+widthCentered+widthRight+playButtonWidth/2;
            int centerSpace = (int)((screenWidth/2-placedButtonsLength)/2);
            
            // draw custom buttons
            for(int i = 0; i < buttonEntries.Count; i++) {

                // alignment
                if(buttonEntries[i].alignment == Preferences.Alignment.Right && !leftDone && (i==0 || buttonEntries[i-1].alignment != Preferences.Alignment.Right)
                || buttonEntries[i].alignment == Preferences.Alignment.Center && !leftDone && (i==0 || buttonEntries[i-1].alignment != Preferences.Alignment.Center)) {
                    if(!isLeftButtons) {
                        GUILayout.FlexibleSpace();
                        currentOffset += GUILayoutUtility.GetLastRect().width;
                    }
                    else {
                        if(buttonEntries[i].alignment == Preferences.Alignment.Right) {
                            for(int s = i; s < buttonEntries.Count; s++) {
                                if(!buttonEntries[s].enabled) continue;
                                currentOffset += buttonEntries[s].width;
                                currentOffset += spaceWidth;
                            }
                            GUILayout.Space((int)(screenWidth/2-(currentOffset+playButtonWidth/2)));
                        }
                        else if(buttonEntries[i].alignment == Preferences.Alignment.Center) {
                            if(placedButtonsLength%2==0)
                                GUILayout.Space(centerSpace+spaceWidth/2);
                            else
                                GUILayout.Space(centerSpace);
                        }
                    }
                    leftDone = true;
                }
                
                // draw
                if(buttonEntries[i].enabled) {
                    if(!Preferences.IsUnitySixOrHigher()) { // version fix
                        if(buttonEntries[i].displayMode == Preferences.DisplayMode.Both) {
                            if(buttonEntries[i].label == "Layout") buttonEntries[i].label = "GridLayoutGroup Icon";
                            if(buttonEntries[i].iconBoth == "Layout") buttonEntries[i].iconBoth = "GridLayoutGroup Icon";
                        }
                        else if(buttonEntries[i].displayMode == Preferences.DisplayMode.Icon)
                            if(buttonEntries[i].label == "Layout") buttonEntries[i].label = "GridLayoutGroup Icon";
                    }

                    if(buttonEntries[i].type == Preferences.Type.Button) {
                        GUIStyle currentStyle;
                        if(buttonEntries[i].backgroundMode == Preferences.BackgroundMode.Transparent)
                            currentStyle = labelStyle;
                        else
                            currentStyle = buttonStyle;
                        var buttonPos = new Vector2(GUILayoutUtility.GetLastRect().xMax, toolbarHeight);
                        bool buttonPressed = false;
                        if(buttonEntries[i].label == "Console Log") {
                            var isRed = CleanEditorLayout.logType == LogType.Error || CleanEditorLayout.logType == LogType.Exception || CleanEditorLayout.logType == LogType.Assert;
                            GUIStyle consoleLogStyle = new GUIStyle(labelStyle) {
                                alignment = TextAnchor.UpperLeft, wordWrap = false,
                                normal = { textColor = isRed ? Color.red : CleanEditorLayout.logType == LogType.Warning ? Color.yellow : Color.white },
                                hover = { textColor = isRed ? Color.red : CleanEditorLayout.logType == LogType.Warning ? Color.yellow : Color.white },
                                active = { textColor = isRed ? Color.red : CleanEditorLayout.logType == LogType.Warning ? Color.yellow : Color.white }
                            };
                            string logIcon;
                            if(CleanEditorLayout.consoleMessage != "") {
                                if(isRed)
                                    logIcon = "console.erroricon.sml";
                                else if(CleanEditorLayout.logType == LogType.Warning)
                                    logIcon = "console.warnicon.sml";
                                else
                                    logIcon = "console.infoicon.sml";
                            }
                            else
                                logIcon = "";

                            var buttonWidth = buttonEntries[i].width;
                            if(buttonEntries[i].alignment == Preferences.Alignment.Left && isLeftButtons) // cap console log width (left side)
                                buttonWidth = Mathf.Min(buttonEntries[i].width, (int)(screenWidth/2-(currentOffset+spaceWidth+playButtonWidth/2)));

                            if(buttonEntries[i].displayMode == Preferences.DisplayMode.Text)
                                buttonPressed = DrawButtonConsoleLog("", CleanEditorLayout.consoleMessage, toolbarHeight, buttonWidth, 2, currentStyle, consoleLogStyle);
                            else if(buttonEntries[i].displayMode == Preferences.DisplayMode.Icon) {
                                if(logIcon == "")
                                    buttonPressed = GUILayout.Button(new GUIContent("", buttonEntries[i].action), currentStyle, GUILayout.Width(buttonWidth), GUILayout.Height(toolbarHeight));
                                else
                                    buttonPressed = GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(logIcon).image, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            }
                            else
                                buttonPressed = DrawButtonConsoleLog(logIcon, CleanEditorLayout.consoleMessage, toolbarHeight, buttonWidth, 2, currentStyle, consoleLogStyle);
                        
                            if(buttonEntries[i].alignment == Preferences.Alignment.Left && isLeftButtons) // cap console log width (left side)
                                GUILayout.Space(Mathf.Max(0, buttonEntries[i].width-buttonWidth-spaceWidth/2));
                        }
                        else if(buttonEntries[i].action == "Quality Level") {
                            var finalLabel = buttonEntries[i].labelBoth == "Quality" ? QualitySettings.names[QualitySettings.GetQualityLevel()] : buttonEntries[i].labelBoth;

                            if(buttonEntries[i].displayMode == Preferences.DisplayMode.Text)
                                buttonPressed = GUILayout.Button(new GUIContent(finalLabel, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else if(buttonEntries[i].displayMode == Preferences.DisplayMode.Icon)
                                buttonPressed = GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(buttonEntries[i].label).image, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else
                                buttonPressed = DrawButtonBothMode(true, buttonEntries[i].label, finalLabel,  buttonEntries[i].iconBoth, toolbarHeight, buttonEntries[i].width, 2, currentStyle, labelStyle, buttonEntries[i].action, true, true);
                        }
                        else if(buttonEntries[i].action == "Background Tasks") {
                            string backgroundTasksIcon = Progress.running ? "WaitSpin00" : "Progress";

                            var finalIcon = buttonEntries[i].label == "Background Tasks" ? backgroundTasksIcon : buttonEntries[i].label;
                            var finalIconBoth = buttonEntries[i].iconBoth == "Background Tasks" ? backgroundTasksIcon : buttonEntries[i].iconBoth;

                            if(buttonEntries[i].displayMode == Preferences.DisplayMode.Text)
                                buttonPressed = GUILayout.Button(new GUIContent(buttonEntries[i].label, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else if(buttonEntries[i].displayMode == Preferences.DisplayMode.Icon)
                                buttonPressed = GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(finalIcon).image, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else
                                buttonPressed = DrawButtonBothMode(true, finalIcon, buttonEntries[i].labelBoth, finalIconBoth, toolbarHeight, buttonEntries[i].width, 2, currentStyle, labelStyle, buttonEntries[i].action);
                        }
                        else if(buttonEntries[i].action == "Debugger") {
                            string debuggerIcon;
                            if(CodeOptimization.Debug == CompilationPipeline.codeOptimization)
                                debuggerIcon = Debugger.IsAttached ? "d_DebuggerAttached@2x" : "d_DebuggerEnabled@2x";
                            else
                                debuggerIcon = "d_DebuggerDisabled@2x";

                            var finalIcon = buttonEntries[i].label == "Debugger" ? debuggerIcon : buttonEntries[i].label;
                            var finalIconBoth = buttonEntries[i].iconBoth == "Debugger" ? debuggerIcon : buttonEntries[i].iconBoth;

                            if(buttonEntries[i].displayMode == Preferences.DisplayMode.Text)
                                buttonPressed = GUILayout.Button(new GUIContent(buttonEntries[i].label, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else if(buttonEntries[i].displayMode == Preferences.DisplayMode.Icon)
                                buttonPressed = GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(finalIcon).image, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else
                                buttonPressed = DrawButtonBothMode(true, finalIcon, buttonEntries[i].labelBoth, finalIconBoth, toolbarHeight, buttonEntries[i].width, 2, currentStyle, labelStyle, buttonEntries[i].action);
                        }
                        else if(buttonEntries[i].action == "Cache Server") {
                            string cacheServerIcon;
                            if(!AssetDatabase.IsCacheServerEnabled())
                                cacheServerIcon = "d_CacheServerDisabled@2x";
                            else if(!AssetDatabase.IsConnectedToCacheServer())
                                cacheServerIcon = "d_CacheServerDisconnected@2x";
                            else
                                cacheServerIcon = "d_CacheServerConnected@2x";

                            var finalIcon = buttonEntries[i].label == "Cache Server" ? cacheServerIcon : buttonEntries[i].label;
                            var finalIconBoth = buttonEntries[i].iconBoth == "Cache Server" ? cacheServerIcon : buttonEntries[i].iconBoth;

                            if(buttonEntries[i].displayMode == Preferences.DisplayMode.Text)
                                buttonPressed = GUILayout.Button(new GUIContent(buttonEntries[i].label, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else if(buttonEntries[i].displayMode == Preferences.DisplayMode.Icon)
                                buttonPressed = GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(finalIcon).image, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else
                                buttonPressed = DrawButtonBothMode(true, finalIcon, buttonEntries[i].labelBoth, finalIconBoth, toolbarHeight, buttonEntries[i].width, 2, currentStyle, labelStyle, buttonEntries[i].action);
                        }
                        else if(buttonEntries[i].action == "Multiplayer Status") {
                            string finalLabel;
                            Texture finalIcon, finalIconBoth;

                            try {
                                string statusLabel;
                                Texture statusIcon;
                                #if UNITY_6000_3_OR_NEWER
                                    var managerType = Type.GetType("Unity.PlayMode.Editor.PlayModeManager, UnityEditor.PlayModeModule");
                                #else
                                    var managerType = Type.GetType("Unity.Multiplayer.PlayMode.Configurations.Editor.PlayModeManager, Unity.Multiplayer.PlayMode.Configurations.Editor");
                                #endif
                                var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(managerType);
                                var instance = singletonType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                                var config = managerType.GetProperty("ActivePlayModeConfig", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(instance);
                                var configType = config.GetType();
                                var configName = (string)configType.GetProperty("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(config);
                                
                                GUI.enabled = configName != "Default";
                                
                                #if UNITY_6000_3_OR_NEWER
                                    var type = Type.GetType("Unity.Multiplayer.PlayMode.Editor.ScenarioRunner, UnityEditor.MultiplayerModule");
                                #else
                                    var type = Type.GetType("Unity.Multiplayer.PlayMode.Scenarios.Editor.ScenarioRunner, Unity.Multiplayer.PlayMode.Scenarios.Editor");
                                #endif
                                var method = type.GetMethod("GetScenarioStatus", BindingFlags.Public | BindingFlags.Static);
                                var status = method.Invoke(null, null);
                                var statusType = status.GetType();
                                var state = statusType.GetField("State", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                var stateName = state.GetValue(status).ToString();
                                var currentStageField = statusType.GetField("CurrentStage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                var currentStage = currentStageField.GetValue(status).ToString();
                                var stageStateField = statusType.GetField("StageState", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                var stageState = stageStateField.GetValue(status).ToString();

                                switch(stateName) {
                                    case "Running":
                                        if(currentStage == "Run" && stageState == "Active") {
                                            statusLabel = "Running";
                                            statusIcon = GetMultiplayerIcon("CompletedTask"); // completed icon
                                        }
                                        else {
                                            statusLabel = currentStage == "Run" ? "Start" : currentStage;
                                            statusIcon = GetMultiplayerIcon("Loading"); // loading icon
                                        }
                                        break;
                                    case "Failed":
                                        statusLabel = stateName;
                                        statusIcon = GetMultiplayerIcon("Error"); // error icon
                                        break;
                                    default:
                                        statusLabel = stateName;
                                        statusIcon = GetMultiplayerIcon("Idle"); // idle icon
                                        break;
                                }

                                finalLabel = buttonEntries[i].labelBoth == "Multiplayer Status" ? statusLabel : buttonEntries[i].labelBoth;
                                finalIcon = buttonEntries[i].label == "Multiplayer Status" ? statusIcon : buttonEntries[i].label != "" ? EditorGUIUtility.IconContent(buttonEntries[i].label).image : null;
                                finalIconBoth = buttonEntries[i].iconBoth == "Multiplayer Status" ? statusIcon : buttonEntries[i].iconBoth != "" ? EditorGUIUtility.IconContent(buttonEntries[i].iconBoth).image : null;

                            }
                            catch(Exception) {                                    
                                finalLabel = buttonEntries[i].labelBoth == "Multiplayer Status" ? "Status" : buttonEntries[i].labelBoth;
                                finalIcon = buttonEntries[i].label == "Multiplayer Status" ? EditorGUIUtility.IconContent("PlayButton On").image : buttonEntries[i].label != "" ? EditorGUIUtility.IconContent(buttonEntries[i].label).image : null;
                                finalIconBoth = buttonEntries[i].iconBoth == "Multiplayer Status" ? EditorGUIUtility.IconContent("PlayButton On").image : buttonEntries[i].iconBoth != "" ? EditorGUIUtility.IconContent(buttonEntries[i].iconBoth).image : null;
                            }

                            if(buttonEntries[i].displayMode == Preferences.DisplayMode.Text)
                                buttonPressed = GUILayout.Button(new GUIContent(finalLabel, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else if(buttonEntries[i].displayMode == Preferences.DisplayMode.Icon)
                                buttonPressed = GUILayout.Button(new GUIContent(finalIcon, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else
                                buttonPressed = DrawButtonBothMode(true, buttonEntries[i].label, finalLabel, buttonEntries[i].iconBoth, toolbarHeight, buttonEntries[i].width, 2, currentStyle, labelStyle, buttonEntries[i].action, true, false, finalIcon, finalIconBoth);
                            GUI.enabled = true;
                        }
                        else if(buttonEntries[i].action == "Multiplayer Scenario") {
                            string finalLabel;
                            Texture finalIcon, finalIconBoth;

                            try {
                                string scenarioLabel;
                                Texture scenarioIcon;
                                #if UNITY_6000_3_OR_NEWER
                                    var managerType = Type.GetType("Unity.PlayMode.Editor.PlayModeManager, UnityEditor.PlayModeModule");
                                #else
                                    var managerType = Type.GetType("Unity.Multiplayer.PlayMode.Configurations.Editor.PlayModeManager, Unity.Multiplayer.PlayMode.Configurations.Editor");
                                #endif
                                var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(managerType);
                                var instance = singletonType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                                var config = managerType.GetProperty("ActivePlayModeConfig", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(instance);
                                var configType = config.GetType();
                                var isValidMethod = configType.GetMethod("IsConfigurationValid", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                var configIsValid = (bool)isValidMethod.Invoke(config, new object[] { null });
                                var configName = (string)configType.GetProperty("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(config);

                                scenarioLabel = configName;
                                if(configName == "Default")
                                    scenarioIcon = EditorGUIUtility.IconContent("UnityLogo").image;
                                else if(!configIsValid)
                                    scenarioIcon = EditorGUIUtility.IconContent("console.warnicon").image;
                                else
                                    scenarioIcon = GetMultiplayerIcon("PlayModeScenario");

                                finalLabel = buttonEntries[i].labelBoth == "Multiplayer Scenario" ? scenarioLabel : buttonEntries[i].labelBoth;
                                finalIcon = buttonEntries[i].label == "Multiplayer Scenario" ? scenarioIcon : buttonEntries[i].label != "" ? EditorGUIUtility.IconContent(buttonEntries[i].label).image : null;
                                finalIconBoth = buttonEntries[i].iconBoth == "Multiplayer Scenario" ? scenarioIcon : buttonEntries[i].iconBoth != "" ? EditorGUIUtility.IconContent(buttonEntries[i].iconBoth).image : null;
                            }
                            catch(Exception) {                                    
                                finalLabel = buttonEntries[i].labelBoth == "Multiplayer Scenario" ? "Scenario" : buttonEntries[i].labelBoth;
                                finalIcon = buttonEntries[i].label == "Multiplayer Scenario" ? EditorGUIUtility.IconContent("PlayButton On").image : buttonEntries[i].label != "" ? EditorGUIUtility.IconContent(buttonEntries[i].label).image : null;
                                finalIconBoth = buttonEntries[i].iconBoth == "Multiplayer Scenario" ? EditorGUIUtility.IconContent("PlayButton On").image : buttonEntries[i].iconBoth != "" ? EditorGUIUtility.IconContent(buttonEntries[i].iconBoth).image : null;
                            }

                            if(buttonEntries[i].displayMode == Preferences.DisplayMode.Text)
                                buttonPressed = GUILayout.Button(new GUIContent(finalLabel, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else if(buttonEntries[i].displayMode == Preferences.DisplayMode.Icon)
                                buttonPressed = GUILayout.Button(new GUIContent(finalIcon, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else
                                buttonPressed = DrawButtonBothMode(true, buttonEntries[i].label, finalLabel, buttonEntries[i].iconBoth, toolbarHeight, buttonEntries[i].width, 2, currentStyle, labelStyle, buttonEntries[i].action, true, false, finalIcon, finalIconBoth);
                        
                        }
                        else if(buttonEntries[i].action == "Multiplayer Roles") {
                            string finalLabel = buttonEntries[i].labelBoth;
                            Texture finalIcon, finalIconBoth;

                            try {
                                Texture rolesIcon;
                                var flagsType = Type.GetType("Unity.Multiplayer.MultiplayerRoleFlags, Unity.DedicatedServer.MultiplayerRoles");
                                var client = Convert.ToInt32(Enum.Parse(flagsType, "Client"));
                                var server = Convert.ToInt32(Enum.Parse(flagsType, "Server"));
                                var both = Convert.ToInt32(Enum.Parse(flagsType, "ClientAndServer"));
                                var managerType = Type.GetType("Unity.Multiplayer.Editor.EditorMultiplayerRolesManager, Unity.DedicatedServer.MultiplayerRoles.Editor");
                                var activeRoleMask = Convert.ToInt32(managerType.GetProperty("ActiveMultiplayerRoleMask", BindingFlags.Public | BindingFlags.Static).GetValue(null));

                                if(activeRoleMask == client)
                                    rolesIcon = EditorGUIUtility.IconContent("BuildSettings.Standalone.Small").image;
                                else if(activeRoleMask == server)
                                    rolesIcon = EditorGUIUtility.IconContent("BuildSettings.DedicatedServer.Small").image;
                                else if(activeRoleMask == 0)
                                    rolesIcon = EditorGUIUtility.IconContent("Warning@2x").image;
                                else
                                    rolesIcon = EditorGUIUtility.IconContent("ServerClient@2x").image;

                                finalIcon = buttonEntries[i].label == "Multiplayer Roles" ? rolesIcon : buttonEntries[i].label != "" ? EditorGUIUtility.IconContent(buttonEntries[i].label).image : null;
                                finalIconBoth = buttonEntries[i].iconBoth == "Multiplayer Roles" ? rolesIcon : buttonEntries[i].iconBoth != "" ? EditorGUIUtility.IconContent(buttonEntries[i].iconBoth).image : null;
                            }
                            catch(Exception) {
                                finalIcon = buttonEntries[i].label == "Multiplayer Roles" ? EditorGUIUtility.IconContent("PlayButton On").image : buttonEntries[i].label != "" ? EditorGUIUtility.IconContent(buttonEntries[i].label).image : null;
                                finalIconBoth = buttonEntries[i].iconBoth == "Multiplayer Roles" ? EditorGUIUtility.IconContent("PlayButton On").image : buttonEntries[i].iconBoth != "" ? EditorGUIUtility.IconContent(buttonEntries[i].iconBoth).image : null;
                            }
                            
                            if(buttonEntries[i].displayMode == Preferences.DisplayMode.Text)
                                buttonPressed = GUILayout.Button(new GUIContent(finalLabel, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else if(buttonEntries[i].displayMode == Preferences.DisplayMode.Icon)
                                buttonPressed = GUILayout.Button(new GUIContent(finalIcon, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else
                                buttonPressed = DrawButtonBothMode(true, buttonEntries[i].label, finalLabel, buttonEntries[i].iconBoth, toolbarHeight, buttonEntries[i].width, 2, currentStyle, labelStyle, buttonEntries[i].action, false, true, finalIcon, finalIconBoth);
                        }
                        else {
                            #if !UNITY_6000_3_OR_NEWER
                                if(buttonEntries[i].label == "AISparkle Icon")
                                    buttonEntries[i].label = "";
                            #endif
                            if(buttonEntries[i].displayMode == Preferences.DisplayMode.Text)
                                buttonPressed = GUILayout.Button(new GUIContent(buttonEntries[i].label, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else if(buttonEntries[i].displayMode == Preferences.DisplayMode.Icon)
                                buttonPressed = GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(buttonEntries[i].label).image, buttonEntries[i].action), currentStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                            else
                                buttonPressed = DrawButtonBothMode(true, buttonEntries[i].label, buttonEntries[i].labelBoth, buttonEntries[i].iconBoth, toolbarHeight, buttonEntries[i].width, 2, currentStyle, labelStyle, buttonEntries[i].action);
                        }
                        if(buttonPressed) {
                            if(buttonEntries[i].action == "")
                                Debug.LogError($"Assign an action to this button!");
                            else if(!actions.ContainsKey(buttonEntries[i].action))
                                Debug.LogError($"The action \"{buttonEntries[i].action}\" does not exist!");
                            else
                                actions[buttonEntries[i].action].Invoke(buttonPos);
                        }

                        currentOffset += buttonEntries[i].width;
                        GUILayout.Space(spaceWidth);
                        currentOffset += spaceWidth;
                    }
                    else if(buttonEntries[i].type == Preferences.Type.Text) {
                        if(buttonEntries[i].displayMode == Preferences.DisplayMode.Text)
                            GUILayout.Label(buttonEntries[i].label, labelStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                        else if(buttonEntries[i].displayMode == Preferences.DisplayMode.Icon)
                            GUILayout.Label(EditorGUIUtility.IconContent(buttonEntries[i].label), labelStyle, GUILayout.Width(buttonEntries[i].width), GUILayout.Height(toolbarHeight));
                        else
                            DrawButtonBothMode(false, buttonEntries[i].label, buttonEntries[i].labelBoth, buttonEntries[i].iconBoth, toolbarHeight, buttonEntries[i].width, 2, buttonStyle, labelStyle, buttonEntries[i].action);

                        currentOffset += buttonEntries[i].width;
                        GUILayout.Space(spaceWidth);
                        currentOffset += spaceWidth;
                    }
                    else {
                        GUILayout.Space(buttonEntries[i].width);
                        currentOffset += buttonEntries[i].width;
                        GUILayout.Space(spaceWidth);
                        currentOffset += spaceWidth;
                    }
                }

                // alignment
                if(buttonEntries[i].alignment == Preferences.Alignment.Left && !rightDone && (i == buttonEntries.Count-1 || buttonEntries[i+1].alignment != Preferences.Alignment.Left) && hasCenter
                || buttonEntries[i].alignment == Preferences.Alignment.Center && !rightDone && (i == buttonEntries.Count-1 || buttonEntries[i+1].alignment != Preferences.Alignment.Center)
                || buttonEntries[i].alignment != Preferences.Alignment.Right && !hasCenter && !hasRight && i == buttonEntries.Count-1) {
                    if(!isLeftButtons) {
                        GUILayout.FlexibleSpace();
                        currentOffset += GUILayoutUtility.GetLastRect().width;
                    }
                    else {
                        if(buttonEntries[i].alignment == Preferences.Alignment.Left)
                            GUILayout.Space((int)(screenWidth/2-(currentOffset+playButtonWidth/2)));
                        else if(buttonEntries[i].alignment == Preferences.Alignment.Center)
                            GUILayout.Space(centerSpace);
                    }
                    rightDone = true;
                }
            }
        }
        private static bool DrawButtonBothMode(bool isButton, string leftIcon, string text, string rightIcon, float height, float width, float spacing, GUIStyle buttonStyle, GUIStyle labelStyle, string action, bool hasChangingText = false, bool isCentered = true, Texture leftIconTexture = null, Texture rightIconTexture = null) {
            bool buttonPressed = false;
            float iconSizeLeft = 16;
            float iconSizeRight = 16;
            Vector2 textSize;
            float contentWidth;
            if(leftIcon.ToLower().Contains("dropdown")) iconSizeLeft = 14;
            if(rightIcon.ToLower().Contains("dropdown")) iconSizeRight = 14;

            if(hasChangingText) {
                if(isCentered)
                    labelStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, };
                else
                    labelStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleLeft, };

                textSize = new Vector2(width - iconSizeLeft - iconSizeRight - 6*spacing, labelStyle.CalcSize(new GUIContent(text)).y);
                if(leftIcon == "") textSize.x += iconSizeLeft + spacing;
                if(rightIcon == "") textSize.x += iconSizeRight + spacing;
                
                contentWidth = iconSizeLeft + spacing + textSize.x + spacing + iconSizeRight;
                if(leftIcon == "") contentWidth += iconSizeLeft + spacing;
                if(rightIcon == "") contentWidth -= iconSizeRight + spacing;
            }
            else {
                textSize = labelStyle.CalcSize(new GUIContent(text));
                if(text == "") textSize = Vector2.zero;
                
                contentWidth = iconSizeLeft + spacing + textSize.x + spacing + iconSizeRight;
                if(leftIcon == "") contentWidth += iconSizeLeft + spacing;
                if(rightIcon == "") contentWidth -= iconSizeRight + spacing;
            }
            
            Rect rect = GUILayoutUtility.GetRect(width, height);

            if(isButton) buttonPressed = GUI.Button(rect, new GUIContent("", action), buttonStyle);

            float startX = rect.x + (rect.width - contentWidth)/2;
            float yLabel = rect.y + (rect.height - textSize.y)/2-0.5f;
            float yLeft = rect.y + (rect.height - iconSizeLeft)/2-0.5f;
            float yRight = rect.y + (rect.height - iconSizeRight)/2-0.5f;
            if(leftIcon == "icon dropdown") yLeft += 0.5f;
            if(rightIcon == "icon dropdown") yRight += 0.5f;

            if(leftIconTexture != null)
                GUI.DrawTexture(new Rect(startX, yLeft, iconSizeLeft, iconSizeLeft), leftIconTexture);
            else if(leftIcon != "")
                GUI.DrawTexture(new Rect(startX, yLeft, iconSizeLeft, iconSizeLeft), EditorGUIUtility.IconContent(leftIcon).image);
            startX += iconSizeLeft + spacing;
            GUI.Label(new Rect(startX, yLabel, textSize.x, textSize.y), text, labelStyle);
            startX += textSize.x + spacing;
            if(rightIconTexture != null)
                GUI.DrawTexture(new Rect(startX, yLeft, iconSizeLeft, iconSizeLeft), rightIconTexture);
            else if(rightIcon != "")
                GUI.DrawTexture(new Rect(startX, yRight, iconSizeRight, iconSizeRight), EditorGUIUtility.IconContent(rightIcon).image);

            return buttonPressed;
        }
        private static bool DrawButtonConsoleLog(string leftIcon, string text, float height, float width, float spacing, GUIStyle buttonStyle, GUIStyle labelStyle) {
            bool buttonPressed;
            float sizeIconLeft = 14;
            float ySizeLabel = 14;

            if(leftIcon == "") {
                float contentWidth = width - spacing;
                Rect rect = GUILayoutUtility.GetRect(width, height);

                buttonPressed = GUI.Button(rect, GUIContent.none, buttonStyle);

                float x = rect.x + (rect.width - contentWidth) / 2;
                float yLabel = rect.y + (rect.height - ySizeLabel) / 2 - 0.5f;
                x += 2*spacing;
                GUI.Label(new Rect(x, yLabel, contentWidth - 2*spacing, ySizeLabel), text, labelStyle);
            }
            else {
                float contentWidth = width - sizeIconLeft + spacing;
                Rect rect = GUILayoutUtility.GetRect(width, height);

                buttonPressed = GUI.Button(rect, GUIContent.none, buttonStyle);

                float startX = rect.x + (rect.width - contentWidth) / 2;
                float yLabel = rect.y + (rect.height - ySizeLabel) / 2 - 0.5f;
                float yLeftIcon = rect.y + (rect.height - sizeIconLeft) / 2;
                if(leftIcon != "")
                    GUI.DrawTexture(new Rect(startX, yLeftIcon, sizeIconLeft, sizeIconLeft), EditorGUIUtility.IconContent(leftIcon).image);

                startX += sizeIconLeft + spacing;
                GUI.Label(new Rect(startX, yLabel, contentWidth - sizeIconLeft + spacing, ySizeLabel), text, labelStyle);
            }

            return buttonPressed;
        }
        public static Dictionary<string, Action<Vector2>> actions = new Dictionary<string, Action<Vector2>> {
            { "Console Log", (pos) => { ExecuteMenuItemByName("General/Console"); } },
            { "Account", (pos) => {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("My account"), false, () => Application.OpenURL("https://id.unity.com/de/account/edit"));
                    menu.AddItem(new GUIContent("Sign out"), false, () => ExecuteMenuItemByName("/Sign out"));
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Upgrade your Unity plan"), false, () => Application.OpenURL("https://unity.com/products"));
                    menu.DropDown(new Rect(pos.x, pos.y, 0, 0));
                }
            },
            { "Asset Store", (pos) => {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Asset Store Web"), false, () => Application.OpenURL("https://assetstore.unity.com"));
                    menu.AddItem(new GUIContent("My Assets"), false, () => ExecuteMenuItemByName("/Package Manager"));
                    menu.DropDown(new Rect(pos.x, pos.y, 0, 0));
                }
            },
            { "Layout", (pos) => {
                    var menu = new GenericMenu();
                    string[] subMenus = Unsupported.GetSubmenus("Window/Layouts");

                    foreach(string path in subMenus) {
                        string item = path.Replace("Window/Layouts/", "");

                        menu.AddItem(new GUIContent(item), false, () => {
                            EditorApplication.ExecuteMenuItem(path);
                        });
                    }
                    menu.DropDown(new Rect(pos.x, pos.y, 0, 0));
                }
            },
            { "Quality Level", (pos) => {
                    var menu = new GenericMenu();
                    string[] levels = QualitySettings.names;
                    int current = QualitySettings.GetQualityLevel();
                    for(int i = 0; i < levels.Length; i++) {
                        int level = i;
                        menu.AddItem(new GUIContent(levels[level]), level == current, () => { QualitySettings.SetQualityLevel(level, true); });
                    }
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Open Quality Settings..."), false, () => SettingsService.OpenProjectSettings("Project/Quality"));
                    menu.DropDown(new Rect(pos.x, pos.y, 0, 0));
                }
            },
            { "Multiplayer Status", (pos) => ShowMultiplayerStatusPopup(pos) },
            { "Multiplayer Scenario", (pos) => ShowMultiplayerScenarioPopup(pos) },
            { "Multiplayer Roles", (pos) => ShowMultiplayerRoles(pos) },
            { "Unity Version Control", (pos) => ExecuteMenuItemByName("/Unity Version Control") },
            { "Package Manager", (pos) => ExecuteMenuItemByName("/Package Manager") },
            { "Undo History", (pos) => ExecuteMenuItemByName("Undo History") },
            { "Search", (pos) => ExecuteMenuItemByName("/Search All...") },
            { "Progress Details", (pos) => ExecuteMenuItemByName("/Progress") },
            { "Background Tasks", (pos) => ExecuteMenuItemByName("/Progress") },
            { "Lighting", (pos) => ExecuteMenuItemByName("Window/Rendering/Lighting") },
            { "Debugger", (pos) => ShowDebuggerPopup(pos) },
            { "Cache Server", (pos) => ShowCacheServerPopup(pos) },
            { "AI", (pos) => ShowAIPopup(pos) },
            { "Maximize", (pos) => EditorApplication.ExecuteMenuItem("Tools/CleanEditorLayout/Maximize View") }
        };
        private static void ExecuteMenuItemByName(string name) {
            var hMenu = Win32.GetMenu(CleanEditorLayout.GetMainWindow());
            var menuCount = Win32.GetMenuItemCount(hMenu);
            var menuItemName = new System.Text.StringBuilder(256);
            for(int i = 0; i < menuCount; i++) {
                Win32.GetMenuString(hMenu, i, menuItemName, menuItemName.Capacity, Win32.MF_BYPOSITION);
                string[] subMenus = Unsupported.GetSubmenus(menuItemName.ToString());

                foreach(string path in subMenus) {
                    if(path.Contains(name)) {
                        EditorApplication.ExecuteMenuItem(path);
                        return;
                    }
                }
            }
        }
        private static void ShowAIPopup(Vector2 pos) {
            #if UNITY_6000_3_OR_NEWER
                var type = Type.GetType("Unity.AI.Toolkit.Accounts.Components.AIDropdownContent, Unity.AI.Toolkit.Accounts");
                if(type == null)
                    type = Type.GetType("UnityEditor.Toolbars.AIDropdownContent, UnityEditor.EditorToolbarModule");
                if(type == null) return;
                var instance = (PopupWindowContent)Activator.CreateInstance(type, true);
                PopupWindow.Show(new Rect(pos.x, pos.y, 0, 0), instance);
            #else
                Debug.LogError("This button is only available in Unity 6.3 and higher!");
            #endif
        }
        private static void ShowDebuggerPopup(Vector2 pos) {
            var type = Type.GetType("UnityEditor.ManagedDebuggerWindow, UnityEditor.CoreModule");
            if(type == null) {
                Debug.LogError($"The popup of the \"Debugger Button\" could not be opened.");
                return;
            }
            var constructor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(CodeOptimization) }, null);
            var instance = constructor.Invoke(new object[] { CompilationPipeline.codeOptimization }) as PopupWindowContent;
            PopupWindow.Show(new Rect(pos.x, pos.y, 0, 0), instance);
        }
        private static void ShowCacheServerPopup(Vector2 pos) {
            var type = Type.GetType("UnityEditor.CacheServerWindow, UnityEditor.CoreModule");
            if(type == null) {
                Debug.LogError($"The popup of the \"Cache Server Button\" could not be opened.");
                return;
            }
            var instance = Activator.CreateInstance(type) as PopupWindowContent;
            PopupWindow.Show(new Rect(pos.x, pos.y, 0, 0), instance);
        }
        private static void ShowMultiplayerStatusPopup(Vector2 pos) {
            #if UNITY_6000_3_OR_NEWER
                var type = Type.GetType("Unity.Multiplayer.PlayMode.Editor.PlaymodeStatusPopupContent, UnityEditor.MultiplayerModule");
            #else
                var type = Type.GetType("Unity.Multiplayer.PlayMode.Scenarios.Editor.Views.PlaymodeStatusPopupContent, Unity.Multiplayer.PlayMode.Scenarios.Editor");
            #endif
            if(type == null) {
                Debug.LogError($"The popup of the \"Multiplayer Status Button\" could not be opened. Do you have the \"Multiplayer Play Mode\" Package installed?");
                return;
            }
            var instance = Activator.CreateInstance(type) as PopupWindowContent;
            PopupWindow.Show(new Rect(pos.x, pos.y, 0, 0), instance);
        }
        private static void ShowMultiplayerScenarioPopup(Vector2 pos) {
            #if UNITY_6000_3_OR_NEWER
                var type = Type.GetType("Unity.PlayMode.Editor.PlaymodePopupContent, UnityEditor.PlayModeModule");
            #else
                var type = Type.GetType("Unity.Multiplayer.PlayMode.Configurations.Editor.Gui.PlaymodePopupContent, Unity.Multiplayer.PlayMode.Configurations.Editor");
            #endif
            if(type == null) {
                Debug.LogError($"The popup of the \"Multiplayer Scenario Button\" could not be opened. Do you have the \"Multiplayer Play Mode\" Package installed?");
                return;
            }
            var instance = Activator.CreateInstance(type) as PopupWindowContent;
            PopupWindow.Show(new Rect(pos.x, pos.y, 0, 0), instance);
        }
        private static void ShowMultiplayerRoles(Vector2 pos) {
            try {
                var flagsType = Type.GetType("Unity.Multiplayer.MultiplayerRoleFlags, Unity.DedicatedServer.MultiplayerRoles");
                var client = (int)Enum.Parse(flagsType, "Client");
                var server = (int)Enum.Parse(flagsType, "Server");
                var both = (int)Enum.Parse(flagsType, "ClientAndServer");

                var managerType = Type.GetType("Unity.Multiplayer.Editor.EditorMultiplayerRolesManager, Unity.DedicatedServer.MultiplayerRoles.Editor");
                var activeRoleMask = managerType.GetProperty("ActiveMultiplayerRoleMask", BindingFlags.Public | BindingFlags.Static);
                var activeRoleMaskValue = (int)activeRoleMask.GetValue(null);

                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Client"), activeRoleMaskValue == client, () => { activeRoleMask.SetValue(null, client); });
                menu.AddItem(new GUIContent("Server"), activeRoleMaskValue == server, () => { activeRoleMask.SetValue(null, server); });
                menu.AddItem(new GUIContent("Client and Server"), activeRoleMaskValue == both,  () => { activeRoleMask.SetValue(null, both); });
                menu.DropDown(new Rect(pos.x, pos.y, 0, 0));
            }
            catch (Exception) {
                Debug.LogError($"The popup of the \"Multiplayer Roles Button\" could not be opened. Do you have the \"Dedicated Server\" Package installed?");
            }
        }
        private static Texture2D GetMultiplayerIcon(string imageName) {
            #if UNITY_6000_3_OR_NEWER
                var iconsType = Type.GetType("Unity.Multiplayer.PlayMode.Editor.Icons, UnityEditor.MultiplayerModule");
            #else
                var iconsType = Type.GetType("Unity.Multiplayer.PlayMode.Scenarios.Editor.Icons, Unity.Multiplayer.PlayMode.Scenarios.Editor");
            #endif
            var enumType = iconsType.GetNestedType("ImageName", BindingFlags.Public | BindingFlags.NonPublic);
            var enumValue = Enum.Parse(enumType, imageName);

            var getImageMethod = iconsType.GetMethod("GetImage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return getImageMethod.Invoke(null, new object[] { enumValue }) as Texture2D;
        }
        private static Type FindAndDebugType(string namePart) {
            foreach(var asm in AppDomain.CurrentDomain.GetAssemblies()) {
                Type[] types;
                try {
                    types = asm.GetTypes();
                } catch(ReflectionTypeLoadException e) {
                    types = e.Types.Where(t => t != null).ToArray();
                }
                foreach(var t in types) {
                    if(t.FullName != null && t.FullName.Contains(namePart)) {
                        Debug.Log(t + " " + asm.FullName);
                        // return t;
                    }
                }
            }
            return null;
        }
        public static void OpenDropdownWithShortcut(string menuName) {
            Shortcuts.currentShortcut = menuName;
            Shortcuts.shortcutActive = true;
            CleanEditorLayout.RepaintToolbar();
        }
        private bool DropdownOverflowSettings(List<Preferences.DropdownEntryData> dropdowns, GUIStyle style, float screenWidth, float uiScaling) {
            var dropdownSectionWidth = 0;
            for(int i = 0; i < dropdowns.Count; i++) {
                if(!dropdowns[i].enabled) continue;
                dropdownSectionWidth += (int)(style.CalcSize(new GUIContent(dropdowns[i].categoryName)).x+Preferences.defaultMenuButtonPadding);
            }

            float availableSpace;
            if(Preferences.PlayButtonDesignType == 0)
                availableSpace = (screenWidth-Preferences.PlayButtonWidthDefault/uiScaling)/2-15;
            else
                availableSpace = (screenWidth-Preferences.PlayButtonWidthSimple/uiScaling)/2-15;
                
            return Preferences.useDropdownOverflowSettings = dropdownSectionWidth > availableSpace;
        }

        private static void EnterPlaymode() {
            try {
                var managerType = Type.GetType("Unity.Multiplayer.PlayMode.Configurations.Editor.PlayModeManager, Unity.Multiplayer.PlayMode.Configurations.Editor");
                var manager = typeof(ScriptableSingleton<>).MakeGenericType(managerType).GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                var startMethod = managerType.GetMethod("Start", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                startMethod.Invoke(manager, null);
            }
            catch (Exception) {
                EditorApplication.EnterPlaymode();
            }
        }
        private static void ExitPlaymode() {
            try {
                var managerType = Type.GetType("Unity.Multiplayer.PlayMode.Configurations.Editor.PlayModeManager, Unity.Multiplayer.PlayMode.Configurations.Editor");
                var manager = typeof(ScriptableSingleton<>).MakeGenericType(managerType).GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                var startMethod = managerType.GetMethod("Stop", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                startMethod.Invoke(manager, null);
            }
            catch (Exception) {
                EditorApplication.ExitPlaymode();
            }
        }
        private static void Resume() {
            try {
                var managerType = Type.GetType("Unity.Multiplayer.PlayMode.Configurations.Editor.PlayModeManager, Unity.Multiplayer.PlayMode.Configurations.Editor");
                var manager = typeof(ScriptableSingleton<>).MakeGenericType(managerType).GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                var startMethod = managerType.GetMethod("Resume", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                startMethod.Invoke(manager, null);
            }
            catch (Exception) {}
        }
        private static void MinimizeEditor() {
            if(Event.current != null && Event.current.button != 0)
                return;

            Win32.ShowWindow(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle, Win32.SW_MINIMIZE);
        }
        public static void SwitchMode(bool fullscreen) {
            if(Event.current != null && Event.current.button != 0)
                return;

            var cleanEditorMainWindow = CleanEditorLayout.GetCleanEditorWindow();
            var currentMonitor = Win32.MonitorFromWindow(cleanEditorMainWindow, Win32.MONITOR_DEFAULTTONEAREST);
            Vector2Int monitorPosition = Win32.GetMonitorPosition(currentMonitor);
            Vector2Int monitorSize = Win32.GetMonitorSize(currentMonitor);

            int width = monitorSize.x;
            int height = monitorSize.y;

            Win32.ShowWindow(cleanEditorMainWindow, Win32.SW_NORMAL);
            if(!fullscreen || !Preferences.IsUnitySixOrHigher()) {
                Win32.SetWindowLong(cleanEditorMainWindow, Win32.GWL_STYLE, Win32.GetWindowLong(cleanEditorMainWindow, Win32.GWL_STYLE) | Win32.WS_CAPTION);
                Win32.SetWindowPos(cleanEditorMainWindow, Win32.HWND_TOP, monitorPosition.x-8, monitorPosition.y+110, width+16, height-200, Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE | Win32.SWP_FRAMECHANGED);
            }
            if(fullscreen)
                Win32.ShowWindow(cleanEditorMainWindow, Win32.SW_MAXIMIZE);
        }
        private static void QuitEditor() {
            if(Event.current != null && Event.current.button != 0)
                return;

            if(EditorSceneManager.GetActiveScene().isDirty) {
                int result = EditorUtility.DisplayDialogComplex(
                    "Scene(s) Have Been Modified",
                    "Do you want to save the changes you made in the scenes:\n" + EditorSceneManager.GetActiveScene().name + "\nYour changes will be lost if you don't save them.",
                    "Save",
                    "Don't Save",
                    "Cancel"
                );
                switch(result) {
                    case 0:
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                        break;
                    case 1:
                        break;
                    case 2:
                        return;
                }
            }
            CloseToolbar();
            Dropdown.CloseAllDropdowns();
            EditorApplication.Exit(0);
        }
    }
}
#endif