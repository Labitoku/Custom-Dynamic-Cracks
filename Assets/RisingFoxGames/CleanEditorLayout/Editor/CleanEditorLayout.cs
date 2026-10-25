#if UNITY_EDITOR_WIN
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

namespace CleanEditorLayout
{
    internal static class CleanEditorLayout
    {
        #region variables
            private static IntPtr cleanEditorWindowHandle, toolbarWindowHandle, mainWindowHandle;
            private static List<IntPtr> handlesList = new List<IntPtr>();
            private static List<IntPtr> fullscreenEditorHandlesList = new List<IntPtr>();
            private static StringBuilder sharedStringBuilder = new StringBuilder(256);
            public static bool maximizePlaymodeOnUpdate, dropDownActive, fixOnUpdate, wasRunning;
            public static string consoleMessage = "";
            public static LogType logType;
        #endregion
        
        [InitializeOnLoadMethod]
        private static void OnLoad() {
            if(Preferences.IsAssetDisabled) return;
            EditorApplication.update += OnUpdate;
            EditorApplication.playModeStateChanged += OnPlaymodeChange;
            EditorApplication.pauseStateChanged += OnPauseChange;
            Application.logMessageReceived += OnLogMessageReceived;
        }
        private static void OnUpdate() {
            if(Preferences.IsAssetDisabled) return;

            GetEditorSubWindows();
            if(cleanEditorWindowHandle != IntPtr.Zero) {
                if(!EditorWindow.HasOpenInstances<Toolbar>()) {
                    Toolbar.OpenToolbar();
                }
                if(fixOnUpdate) {
                    Toolbar.SwitchMode(true);
                    fixOnUpdate = false;
                }
                if(maximizePlaymodeOnUpdate) {
                    Maximize(true);
                    maximizePlaymodeOnUpdate = false;
                }
                if(!IsWindowInFront(toolbarWindowHandle, cleanEditorWindowHandle)
                || fullscreenEditorHandlesList.Count == 1 && IsWindowInFront(toolbarWindowHandle, fullscreenEditorHandlesList[0]))
                    SetZOrder();
            }
            else
                Toolbar.CloseToolbar();
                
            OnFocusChange();
            OnClearConsole();
            OnProgressChanged();
        }
        private static void OnPlaymodeChange(PlayModeStateChange change) {

        // get playmode type value
            var playModeViewType = typeof(Editor).Assembly.GetType("UnityEditor.PlayModeView");
            var playModeViews = Resources.FindObjectsOfTypeAll(playModeViewType);
            if(playModeViews.Length == 0) return;
            var playModeView = playModeViews[0];
            var enterPlayModeBehaviorType = playModeViewType.GetNestedTypes()[0];
            var field = playModeViewType.GetField("m_EnterPlayModeBehavior", BindingFlags.NonPublic | BindingFlags.Instance);
            var value = field.GetValue(playModeView);
            var playModeType = Enum.GetName(enterPlayModeBehaviorType, value);


        // update playmode icon
            if(change == PlayModeStateChange.EnteredPlayMode) {
                Preferences.PlaymodeIconFix = false;

                if(playModeType == "PlayFocused" && EditorWindow.focusedWindow != null)  // minor fix to focus game window
                    Preferences.LastFocusedWindow = EditorWindow.focusedWindow.GetType().AssemblyQualifiedName;
            }
            else if(change == PlayModeStateChange.EnteredEditMode) {
                Preferences.PlaymodeIconFix = true;

                RepaintToolbar();
            }

        // maximize on play
            if(Win32.GetActiveWindow() == GetMainWindow()) return;
            if(EditorPrefs.GetString("Fullscreen.FullscreenOnPlay") == "{\"savedValue\":true}") return;
            
            if(playModeType != "PlayMaximized") return;
            
            if(change == PlayModeStateChange.EnteredPlayMode && !Preferences.IsMaximized)
                maximizePlaymodeOnUpdate = true; // maximize later in OnUpdate
            else if(change == PlayModeStateChange.EnteredEditMode && Preferences.IsMaximized)
                Maximize();

        // fix not detecting logs
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceived += OnLogMessageReceived;

        }
        private static void OnPauseChange(PauseState state) {
            RepaintToolbar();
        }
        private static void OnFocusChange() {
            if(EditorWindow.focusedWindow != null) {
                if(EditorWindow.focusedWindow.GetType().AssemblyQualifiedName != Preferences.LastFocusedWindow && Win32.GetActiveWindow() == cleanEditorWindowHandle)
                    Preferences.LastFocusedWindow = EditorWindow.focusedWindow.GetType().AssemblyQualifiedName;
                    
                if(EditorWindow.focusedWindow is not Dropdown)
                    dropDownActive = false;
            }
        }
        private static void OnClearConsole() {
            #if UNITY_2023_1_OR_NEWER
                ConsoleWindowUtility.GetConsoleLogCounts(out int error, out int warn, out int log);
            #else
                var type = typeof(Editor).Assembly.GetType("UnityEditor.LogEntries");
                var method = type.GetMethod("GetCountsByType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                object[] counts = { 0, 0, 0 };
                method.Invoke(null, counts);
                int error = (int)counts[0], warn = (int)counts[1], log = (int)counts[2];
            #endif

            if(error == 0 && warn == 0 && log == 0 && consoleMessage != "" && (EditorPrefs.GetBool("CEL_HasConsoleLogLeft") || EditorPrefs.GetBool("CEL_HasConsoleLogRight"))) {
                consoleMessage = "";
                RepaintToolbar();
            }
        }
        private static void OnLogMessageReceived(string logString, string stackTrace, LogType type) {
            if(!Preferences.showError && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) { return; }
            if(!Preferences.showWarn && type == LogType.Warning) { return; }
            if(!Preferences.showLog && type == LogType.Log) { return; }
            if(logString.Contains("Cannot save invalid window CleanEditorLayout.Toolbar")) return;
            if(!EditorPrefs.GetBool("CEL_HasConsoleLogLeft") && !EditorPrefs.GetBool("CEL_HasConsoleLogRight")) return;

            consoleMessage = logString;
            logType = type;
            RepaintToolbar();
        }
        private static void OnProgressChanged() {
            if(wasRunning == Progress.running) return;
            if(!EditorPrefs.GetBool("CEL_HasBackgroundTasks")) return;
            
            wasRunning = Progress.running;
            RepaintToolbar();
        }

        private static void SetZOrder() {
            for(int i = 0; i < handlesList.Count; i++)
                Win32.SetWindowPos(handlesList[i], Win32.HWND_TOP, 0, 0, 0, 0, Win32.SWP_NOACTIVATE | Win32.SWP_NOMOVE | Win32.SWP_NOSIZE);
        }
        private static bool IsWindowInFront(IntPtr mainHandle, IntPtr otherHandle) {
            IntPtr current = Win32.GetTopWindow(IntPtr.Zero);

            while(current != IntPtr.Zero) {
                if(current == mainHandle)
                    return true;
                if(current == otherHandle)
                    return false;

                current = Win32.GetWindow(current, Win32.GW_HWNDNEXT);
            }
            return false;
        }
        public static void GetEditorSubWindows() {
            IntPtr activeWindow = Win32.GetActiveWindow();
            if(activeWindow == IntPtr.Zero) return;
            mainWindowHandle = activeWindow;
            for(int i = 0; i < 20; i++) {
                IntPtr owner = Win32.GetWindow(mainWindowHandle, Win32.GW_OWNER);
                if(owner == IntPtr.Zero || owner == mainWindowHandle) break;
                mainWindowHandle = owner;
            }

            handlesList.Clear();
            fullscreenEditorHandlesList.Clear();
            string windowTitle, className;
            toolbarWindowHandle = IntPtr.Zero;
            cleanEditorWindowHandle = IntPtr.Zero;
            IntPtr handleOwner = IntPtr.Zero;
            IntPtr saveLayoutDialogHandle = IntPtr.Zero;
            IntPtr mainSubWindowHandleOld = cleanEditorWindowHandle;

            Win32.EnumWindows((handle, lParam) => {
                handleOwner = Win32.GetWindow(handle, Win32.GW_OWNER);
                windowTitle = GetWindowTitle(handle);
                className = GetWindowClassName(handle);
                if(className == "UnityPopupWndClass" && handleOwner == mainWindowHandle && windowTitle.Contains("Toolbar")) {
                    toolbarWindowHandle = handle;
                }
                else if(className.Contains("Unity") && handleOwner == mainWindowHandle) {
                    if(Win32.IsZoomed(handle))
                        cleanEditorWindowHandle = handle;
                    else if(windowTitle == "")
                        fullscreenEditorHandlesList.Add(handle);
                    else
                        handlesList.Add(handle);

                    if(windowTitle.Contains("Save Layout"))
                        saveLayoutDialogHandle = handle;
                }
                return true;
            }, IntPtr.Zero);

            if(saveLayoutDialogHandle != IntPtr.Zero) cleanEditorWindowHandle = mainSubWindowHandleOld;

            handlesList.Add(toolbarWindowHandle);
            handlesList.AddRange(fullscreenEditorHandlesList); // make sure fullscreen editor windows are in front
        }
        private static string GetWindowTitle(IntPtr handle) {
            sharedStringBuilder.Clear();
            Win32.GetWindowText(handle, sharedStringBuilder, sharedStringBuilder.Capacity);
            return sharedStringBuilder.ToString();
        }
        private static string GetWindowClassName(IntPtr handle) {
            sharedStringBuilder.Clear();
            Win32.GetClassName(handle, sharedStringBuilder, sharedStringBuilder.Capacity);
            return sharedStringBuilder.ToString();
        }
        public static IntPtr GetMainWindow() {
            return mainWindowHandle;
        }
        public static IntPtr GetCleanEditorWindow() {
            return cleanEditorWindowHandle;
        }
        public static IntPtr GetToolbarWindow() {
            return toolbarWindowHandle;
        }
        public static void RepaintToolbar() {
            if(EditorWindow.HasOpenInstances<Toolbar>())
                EditorWindow.GetWindow<Toolbar>("Toolbar", false).Repaint();
        }


        private static void Maximize(bool playmode = false) {
            var activeWindow = Win32.GetActiveWindow();
            var mainWindow = GetMainWindow();
            if((activeWindow == mainWindow || Preferences.IsAssetDisabled) && EditorWindow.focusedWindow != null) {
                EditorWindow.focusedWindow.maximized = !EditorWindow.focusedWindow.maximized;
                Menu.SetChecked("Tools/CleanEditorLayout/Maximize View", EditorWindow.focusedWindow.maximized);
            }
            else if(EditorWindow.HasOpenInstances<Toolbar>() && !Preferences.IsAssetDisabled) {
                var unmaximizedLayoutPath = GetUnmaximizedLayoutPath();
                var allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>().ToList();

                if(!Preferences.IsMaximized) {
                    // save unmaximized layout
                    Type windowLayout = typeof(Editor).Assembly.GetType("UnityEditor.WindowLayout");
                    if(windowLayout != null) {
                        Toolbar.CloseToolbar();
                        var saveWindowLayout = windowLayout.GetMethod("SaveWindowLayout", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null);
                        saveWindowLayout.Invoke(null, new object[] { unmaximizedLayoutPath });
                        Toolbar.OpenToolbar();
                    }

                    // close all other tabs
                    Type type = null;
                    if(playmode)
                        type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                    else if(!string.IsNullOrEmpty(Preferences.LastFocusedWindow))
                        type = Type.GetType(Preferences.LastFocusedWindow);

                    if(type == null || type == typeof(Toolbar) || type == typeof(Dropdown)) return;

                    foreach(var window in allWindows)
                        if(window != null && window.GetType() != type && window.GetType() != typeof(Dropdown) && window.GetType() != typeof(Toolbar) && window.GetType().ToString() != "UnityEditor.MainToolbarWindow")
                            window.Close();

                    Preferences.IsMaximized = true;
                    Menu.SetChecked("Tools/CleanEditorLayout/Maximize View", true);
                }
                else {
                    // load unmaximized layout and focus previously maximized tab
                    if(File.Exists(unmaximizedLayoutPath))
                        EditorUtility.LoadWindowLayout(unmaximizedLayoutPath);
                        
                    if(!string.IsNullOrEmpty(Preferences.LastFocusedWindow) && Type.GetType(Preferences.LastFocusedWindow) != null)
                        EditorWindow.GetWindow(Type.GetType(Preferences.LastFocusedWindow)).Focus();

                    Preferences.IsMaximized = false;
                    Menu.SetChecked("Tools/CleanEditorLayout/Maximize View", false);
                }
            }
            dropDownActive = false;
        }
        private static string GetDefaultLayoutPath() {
            string toolPath = Directory.GetDirectories(Application.dataPath, "CleanEditorLayout", SearchOption.AllDirectories)[0];
            return toolPath + "/Layouts/DefaultLayout.wlt";
        }
        private static string GetUnmaximizedLayoutPath() {
            string toolPath = Directory.GetDirectories(Application.dataPath, "CleanEditorLayout", SearchOption.AllDirectories)[0];
            return toolPath + "/Layouts/UnmaximizedLayout.wlt";
        }
        private static string GetLastNativeLayoutPath() {
            string toolPath = Directory.GetDirectories(Application.dataPath, "CleanEditorLayout", SearchOption.AllDirectories)[0];
            return toolPath + "/Layouts/LastNativeLayout.wlt";
        }
        private static string GetLastCleanEditorLayoutPath() {
            string toolPath = Directory.GetDirectories(Application.dataPath, "CleanEditorLayout", SearchOption.AllDirectories)[0];
            return toolPath + "/Layouts/LastCleanEditorLayout.wlt";
        }

       
        [MenuItem("Tools/CleanEditorLayout/Load Default Layout", false, 0)]
        private static void LoadDefaultLayout() {
            // save last layout created in default unity
            if(!EditorWindow.HasOpenInstances<Toolbar>()) {
                var lastNativeLayoutPath = GetLastNativeLayoutPath();
                Type windowLayout = typeof(Editor).Assembly.GetType("UnityEditor.WindowLayout");
                if(windowLayout != null) {
                    var saveWindowLayout = windowLayout.GetMethod("SaveWindowLayout", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null);
                    saveWindowLayout.Invoke(null, new object[] { lastNativeLayoutPath });
                }
            }

            // load the default "clean editor" layout
            var defaultLayout = GetDefaultLayoutPath();
            if(File.Exists(defaultLayout))
                EditorUtility.LoadWindowLayout(defaultLayout);
        }
        [MenuItem("Tools/CleanEditorLayout/Load Default Layout", true, 0)]
        private static bool LoadDefaultLayoutValidate() {
            return !Preferences.IsAssetDisabled;
        }
        [MenuItem("Tools/CleanEditorLayout/Maximize View", false, 30)]
        private static void ToggleMaximize() {
            if(!(EditorApplication.isPlaying && EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType().AssemblyQualifiedName.Contains("UnityEditor.GameView")))
                Maximize();
        }
        [MenuItem("Tools/CleanEditorLayout/Disable CleanEditorLayout", false, 45)]
        private static void DisableAsset() {
            Preferences.IsAssetDisabled = !Preferences.IsAssetDisabled;

            // save/load layouts (switch between last native layout and last "clean editor" layout)
            var lastNativeLayoutPath = GetLastNativeLayoutPath();
            var lastCleanEditorLayoutPath = GetLastCleanEditorLayoutPath();
            Type windowLayout = typeof(Editor).Assembly.GetType("UnityEditor.WindowLayout");
            var saveWindowLayout = windowLayout.GetMethod("SaveWindowLayout", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null);
            if(Preferences.IsAssetDisabled && File.Exists(lastNativeLayoutPath)) {
                if(windowLayout != null && EditorWindow.HasOpenInstances<Toolbar>()) {
                    Toolbar.CloseToolbar();
                    saveWindowLayout.Invoke(null, new object[] { lastCleanEditorLayoutPath });
                }
                EditorUtility.LoadWindowLayout(lastNativeLayoutPath);
            }
            else if(!Preferences.IsAssetDisabled && File.Exists(lastCleanEditorLayoutPath)) {
                if(windowLayout != null && !EditorWindow.HasOpenInstances<Toolbar>()) {
                    saveWindowLayout.Invoke(null, new object[] { lastNativeLayoutPath });
                }
                EditorUtility.LoadWindowLayout(lastCleanEditorLayoutPath);
            }

            Toolbar.CloseToolbar();
            Dropdown.CloseAllDropdowns();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }
        [MenuItem("Tools/CleanEditorLayout/Disable CleanEditorLayout", true, 45)]
        private static bool DisableAssetSetChecked() {
            Menu.SetChecked("Tools/CleanEditorLayout/Disable CleanEditorLayout", Preferences.IsAssetDisabled);
            return true;
        }
    }
}
#endif