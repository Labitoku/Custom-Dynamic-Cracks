#if UNITY_EDITOR_WIN
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.IO;
using System.Collections.Generic;

namespace CleanEditorLayout
{
    internal static class Preferences
    {
        #region variables
            private enum ColorMode { Default, Custom }
            private enum PlayButtonDesign { Default, Simple }
            public enum DisplayMode { Text, Icon, Both }
            public enum Type { Button, Text, Space }
            public enum Alignment { Left, Center, Right }
            public enum BackgroundMode { Default, Transparent }
            private static GUIStyle labelStyle;
            private static Color defaultColorDark = new Color(0.1f, 0.1f, 0.1f, 1f);
            private static Color defaultColorLight = new Color(0.7f, 0.7f, 0.7f, 1f);
            private static Color defaultButtonColorDark = new Color(0.6f, 0.6f, 0.6f, 1f);
            private static Color defaultButtonColorLight = new Color(0.9f, 0.9f, 0.9f, 1f);
            public static bool useDropdownOverflowSettings;
            public static int defaultMenuButtonFontSize = 12;
            public static int defaultMenuButtonPadding = 10;
            private static int defaultDropdownOffsetTopBottom = 0;
            private static int defaultDropdownPaddingX = 0;
            private static int defaultDropdownPaddingY = 0;
            private static float defaultDropdownBrightness = 1.1f;
            private static ReorderableList leftButtonsList, rightButtonsList, dropdownsList;
            public static string LeftButtonsListKey = "CEL_LeftButtonsList", RightButtonsListKey = "CEL_RightButtonsList";
            public static string DropdownsListKey = "CEL_DropdownsList";
            public static int MenuButtonFontSize { get => EditorPrefs.GetInt("CEL_MenuButtonFontSize", defaultMenuButtonFontSize); }
            public static int MenuButtonPadding { get => EditorPrefs.GetInt("CEL_MenuButtonPadding", defaultMenuButtonPadding); }
            public static int DropdownPaddingX { get => EditorPrefs.GetInt("CEL_DropdownPaddingX", defaultDropdownPaddingX); }
            public static int DropdownOffsetTopBottom { get => EditorPrefs.GetInt("CEL_DropdownOffsetTopBottom", defaultDropdownOffsetTopBottom); }
            public static int DropdownPaddingY { get => EditorPrefs.GetInt("CEL_DropdownPaddingY", defaultDropdownPaddingY); }
            public static float DropdownBrightness { get => EditorPrefs.GetFloat("CEL_DropdownBrightness", defaultDropdownBrightness); }
            public static int PlayButtonDesignType { get => EditorPrefs.GetInt("CEL_PlayButtonDesign"); }
            public static int PlayButtonWidthDefault { get => EditorPrefs.GetInt("CEL_PlayButtonWidthDefault", 130); }
            public static int PlayButtonWidthSimple { get => EditorPrefs.GetInt("CEL_PlayButtonWidthSimple", 130); }
            public static int DisplayModeType { get => EditorPrefs.GetInt("CEL_DisplayMode", 2); }
            public static bool HighlightStop { get => EditorPrefs.GetBool("CEL_HighlightStop"); }
            public static bool IsMaximized { get => EditorPrefs.GetBool("CEL_IsMaximized"); set => EditorPrefs.SetBool("CEL_IsMaximized", value); }
            public static string LastFocusedWindow { get => EditorPrefs.GetString("CEL_LastFocusedWindow"); set => EditorPrefs.SetString("CEL_LastFocusedWindow", value); }
            public static bool IsAssetDisabled { get => EditorPrefs.GetBool("CEL_IsAssetDisabled"); set => EditorPrefs.SetBool("CEL_IsAssetDisabled", value); }
            public static bool PlaymodeIconFix { get => EditorPrefs.GetBool("CEL_PlaymodeStopped", true); set => EditorPrefs.SetBool("CEL_PlaymodeStopped", value); }
            public static bool showError { get => EditorPrefs.GetBool("CEL_ShowError", true); set => EditorPrefs.SetBool("CEL_ShowError", value); }
            public static bool showWarn { get => EditorPrefs.GetBool("CEL_ShowWarn", false); set => EditorPrefs.SetBool("CEL_ShowWarn", value); }
            public static bool showLog { get => EditorPrefs.GetBool("CEL_ShowLog", true); set => EditorPrefs.SetBool("CEL_ShowLog", value); }
        #endregion

        [InitializeOnLoadMethod]
        private static void OnLoad() {
            if(IsAssetDisabled) return;
            EditorApplication.update += UpdateOnEditorSkinChange;
            
            #if UNITY_6000_3_OR_NEWER
            QualitySettings.activeQualityLevelChanged += OnQualityLevelChanged;
            #endif
        }
        [MenuItem("Tools/CleanEditorLayout/Preferences", false, 10)]
        private static void OpenPreferences() {
            SettingsService.OpenUserPreferences("Preferences/Clean Editor Layout");
        }
        [SettingsProvider]
        private static SettingsProvider CustomEditorLayoutPreferences() {
            return new SettingsProvider("Preferences/Clean Editor Layout", SettingsScope.User) {
                guiHandler = (searchContext) => {
                    if(IsAssetDisabled) {
                        GUILayout.Space(10);
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        GUILayout.Label("Clean Editor Layout is disabled!");
                        EditorGUILayout.EndHorizontal();
                        return;
                    }
                    GUILayout.Space(10);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    EditorGUILayout.BeginVertical();

                    // colors section ----------------------------------------------

                        var labelToggle = LabelField("Colors", "CEL_ColorsToggle");
                        if(labelToggle) {
                            GUILayout.Space(5);
                            
                            ColorMode colorMode;
                            Color defaultColor = EditorGUIUtility.isProSkin ? defaultColorDark : defaultColorLight;
                            Color defaultButtonColor = EditorGUIUtility.isProSkin ? defaultButtonColorDark : defaultButtonColorLight;

                            EditorGUILayout.BeginHorizontal();
                            colorMode = (ColorMode)EnumField<ColorMode>("Mode", "CEL_ColorMode");
                            if(colorMode == ColorMode.Custom && GUILayout.Button("Reset", GUILayout.Width(50))) { 
                                bool confirm = EditorUtility.DisplayDialog("Reset Colors", "Do you really want to reset the colors to their default values?", "Reset", "Cancel");

                                if(confirm) {
                                    ResetColor("CEL_EditorColor", defaultColor);
                                    ResetColor("CEL_ToolbarColor", defaultColor);
                                    ResetColor("CEL_ButtonColor", defaultButtonColor);
                                    UpdateEditorColor();
                                    CleanEditorLayout.RepaintToolbar();
                                }
                            }
                            EditorGUILayout.EndHorizontal();

                            if(colorMode == ColorMode.Custom) {
                                GUILayout.Space(5);
                                if(IsUnitySixOrHigher()) {
                                    ColorField("Editor Color", "CEL_EditorColor", defaultColor);
                                    GUILayout.Space(1);
                                }
                                ColorField("Toolbar Color", "CEL_ToolbarColor", defaultColor);
                                GUILayout.Space(1);
                                ColorField("Button Color", "CEL_ButtonColor", defaultButtonColor);
                                GUILayout.Space(5);

                                EditorGUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();
                                
                                if(GUILayout.Button("Apply", GUILayout.Width(170))) {
                                    CleanEditorLayout.RepaintToolbar();
                                    UpdateEditorColor();
                                }

                                GUILayout.FlexibleSpace();
                                EditorGUILayout.EndHorizontal();
                            }
                            GUILayout.Space(5);
                        }

                    // dropdown section --------------------------------------------

                        GUILayout.Space(5);
                        labelToggle = LabelField("Dropdowns", "CEL_DropdownSectionToggle");
                        if(labelToggle) {
                            GUILayout.Space(5);
                            var stylingToggle = ToggleField("Styling Options", "CEL_DropdownStylingOptions", false);
                            if(stylingToggle) {
                                var oldLabelWidth = EditorGUIUtility.labelWidth;
                                EditorGUIUtility.labelWidth = 190;
                                EditorGUI.BeginChangeCheck();
                                IntSliderField("Menu Button Padding", "CEL_MenuButtonPadding", 4, 16, defaultMenuButtonPadding);
                                if(useDropdownOverflowSettings)
                                    IntSliderField("Menu Button Font Size", "CEL_MenuButtonFontSize", 10, 12, defaultMenuButtonFontSize);
                                if(EditorGUI.EndChangeCheck())
                                    CleanEditorLayout.RepaintToolbar();
                                IntSliderField("Dropdown Offset Top/Bottom", "CEL_DropdownOffsetTopBottom", 0, 10, defaultDropdownOffsetTopBottom);
                                IntSliderField("Dropdown Padding X", "CEL_DropdownPaddingX", 0, 10, defaultDropdownPaddingX);
                                IntSliderField("Dropdown Padding Y", "CEL_DropdownPaddingY", 0, 10, defaultDropdownPaddingY);
                                SliderField("Dropdown Brightness", "CEL_DropdownBrightness", 0.8f, 1.2f, defaultDropdownBrightness);
                                EditorGUIUtility.labelWidth = oldLabelWidth;
                            }
                            GUILayout.Space(5);
                            
                            ListField<DropdownEntryData>(ref dropdownsList, "CEL_DropdownsList");
                        }

                    // first customizable section ----------------------------------
                        
                        GUILayout.Space(5);
                        labelToggle = LabelField("Buttons Left", "CEL_LeftButtonSectionToggle");
                        if(labelToggle) {
                            GUILayout.Space(5);
                            ListField<ButtonEntryData>(ref leftButtonsList, "CEL_LeftButtonsList", true);
                            GUILayout.Space(5);
                        }

                    // play button section -----------------------------------------

                        GUILayout.Space(5);
                        labelToggle = LabelField("Play Buttons", "CEL_PlayButtonSectionToggle");
                        if(labelToggle) {
                            GUILayout.Space(5);
                            var playButtontype = (PlayButtonDesign)EnumField<PlayButtonDesign>("Play Button Design", "CEL_PlayButtonDesign");
                            
                            GUILayout.Space(5);
                            EditorGUI.BeginChangeCheck();
                            if(playButtontype == PlayButtonDesign.Default) {
                                IntField("Play Button Width", "CEL_PlayButtonWidthDefault", 130);
                            }
                            else if(playButtontype == PlayButtonDesign.Simple) {
                                IntField("Play Button Width", "CEL_PlayButtonWidthSimple", 130);
                                GUILayout.Space(1);
                                EnumField<DisplayMode>("Display Mode", "CEL_DisplayMode");
                                GUILayout.Space(1);
                                ToggleField("Highlight Stop Button", "CEL_HighlightStop", false);
                            }
                            GUILayout.Space(5);
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();

                            if(EditorGUI.EndChangeCheck())
                                CleanEditorLayout.RepaintToolbar();

                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                            GUILayout.Space(5);
                        }

                    // second customizable section ---------------------------------
                        
                        GUILayout.Space(5); 
                        labelToggle = LabelField("Buttons Right", "CEL_RightButtonSectionToggle");
                        if(labelToggle) {
                            GUILayout.Space(5);
                            ListField<ButtonEntryData>(ref rightButtonsList, "CEL_RightButtonsList", false);
                            GUILayout.Space(5);
                        }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(20);
                }
            };
        }

        private static void ColorField(string labelText, string key, Color defaultColor) {
            Color savedColor = LoadColor(key, defaultColor);
            Color newColor = EditorGUILayout.ColorField(labelText, savedColor);
            
            if(newColor != savedColor) {
                EditorPrefs.SetFloat(key + "_R", newColor.r);
                EditorPrefs.SetFloat(key + "_G", newColor.g);
                EditorPrefs.SetFloat(key + "_B", newColor.b);
                EditorPrefs.SetFloat(key + "_A", newColor.a);
            }
        }
        private static bool ToggleField(string labelText, string key, bool defaultVal) {
            bool savedVal = EditorPrefs.GetBool(key, defaultVal);
            bool newVal = EditorGUILayout.Toggle(labelText, savedVal);
            
            if(newVal != savedVal) {
                EditorPrefs.SetBool(key, newVal);
            }
            return newVal;
        }
        private static float SliderField(string labelText, string key, float leftValue, float rightValue, float defaultVal) {
            float savedVal = EditorPrefs.GetFloat(key, defaultVal);
            float newVal = EditorGUILayout.Slider(labelText, savedVal, leftValue, rightValue);
            
            if(newVal != savedVal) {
                EditorPrefs.SetFloat(key, newVal);
            }
            return newVal;
        }
        private static void IntField(string labelText, string key, int defaultVal) {
            int savedVal = EditorPrefs.GetInt(key, defaultVal);
            int newVal = EditorGUILayout.DelayedIntField(labelText, savedVal);
            
            if(newVal != savedVal) {
                EditorPrefs.SetInt(key, newVal);
            }
        }
        private static int IntSliderField(string labelText, string key, int leftValue, int rightValue, int defaultVal) {
            int savedVal = EditorPrefs.GetInt(key, defaultVal);
            int newVal = EditorGUILayout.IntSlider(labelText, savedVal, leftValue, rightValue);
            
            if(newVal != savedVal) {
                EditorPrefs.SetInt(key, newVal);
            }
            return newVal;
        }
        private static int EnumField<EnumType>(string labelText, string key) where EnumType : Enum {
            var savedEnumValue = EditorPrefs.GetInt(key);
            var newEnumValue = Convert.ToInt32(EditorGUILayout.EnumPopup(labelText, (EnumType)(object)savedEnumValue));
            Convert.ToInt32(savedEnumValue);
            if(newEnumValue != savedEnumValue) {
                EditorPrefs.SetInt(key, newEnumValue);
                CleanEditorLayout.RepaintToolbar();
                UpdateEditorColor();
            }
            return newEnumValue;
        }
        private static bool LabelField(string labelText, string key) {
            if(labelStyle == null) {
                labelStyle = new GUIStyle(GUI.skin.label) {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }

            var textSize = labelStyle.CalcSize(new GUIContent(labelText));
            var rect = GUILayoutUtility.GetRect(0, textSize.y + 15);
            
            var toggleState = EditorPrefs.GetBool(key);
            EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.2f));

            if(Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition)) {
                EditorPrefs.SetBool(key, !toggleState);
                return !toggleState;
            }
            EditorGUI.LabelField(rect, labelText, labelStyle);

            return toggleState;
        }
        private static void ListField<T>(ref ReorderableList reorderableList, string key, bool isLeftSided = false) {
            if(reorderableList == null) {
                if(typeof(T) == typeof(ButtonEntryData))
                    reorderableList = InitializeButtonsList(key, isLeftSided);
                else
                    reorderableList = InitializeDropdownsList(key);
            }

            reorderableList.DoLayoutList();
        }
        
        public static Color GetToolbarColor() {
            Color toolbarColor;
            Color defaultColor = EditorGUIUtility.isProSkin ? defaultColorDark : defaultColorLight;
            if(EditorPrefs.GetInt("CEL_ColorMode") == 0)
                toolbarColor = defaultColor;
            else
                toolbarColor = LoadColor("CEL_ToolbarColor", defaultColor);

            return toolbarColor;
        }
        public static Color GetButtonColor() {
            Color buttonColor;
            Color defaultButtonColor = EditorGUIUtility.isProSkin ? defaultButtonColorDark : defaultButtonColorLight;
            if(EditorPrefs.GetInt("CEL_ColorMode") == 0)
                buttonColor = defaultButtonColor;
            else
                buttonColor = LoadColor("CEL_ButtonColor", defaultButtonColor);

            return buttonColor;
        }
        private static Color LoadColor(string key, Color defaultColor) {
            return new Color(
                EditorPrefs.GetFloat(key + "_R", defaultColor.r),
                EditorPrefs.GetFloat(key + "_G", defaultColor.g),
                EditorPrefs.GetFloat(key + "_B", defaultColor.b),
                EditorPrefs.GetFloat(key + "_A", defaultColor.a)
            );
        }
        private static void ResetColor(string key, Color defaultColor) {
            EditorPrefs.SetFloat(key + "_R", defaultColor.r);
            EditorPrefs.SetFloat(key + "_G", defaultColor.g);
            EditorPrefs.SetFloat(key + "_B", defaultColor.b);
            EditorPrefs.SetFloat(key + "_A", defaultColor.a);
        }
        private static void OnQualityLevelChanged(int old, int current) {
            CleanEditorLayout.RepaintToolbar();
        }
        private static void UpdateOnEditorSkinChange() {
            if(EditorGUIUtility.isProSkin != EditorPrefs.GetBool("CEL_EditorTheme")) {
                EditorPrefs.SetBool("CEL_EditorTheme", EditorGUIUtility.isProSkin);
                UpdateEditorColor();
                CleanEditorLayout.RepaintToolbar();
            }
        }
        private static void UpdateEditorColor() {
            if(!IsUnitySixOrHigher()) return;
            PreventStyleSheetDuplicates();
            var styleSheetPath = GetStyleSheetPath();
            string oldText = "";
            if(File.Exists(styleSheetPath))
                oldText = File.ReadAllText(styleSheetPath);
            Color defaultColor = EditorGUIUtility.isProSkin ? defaultColorDark : defaultColorLight;
            string color;
            if(EditorPrefs.GetInt("CEL_ColorMode") == 0)
                color = ColorUtility.ToHtmlStringRGB(defaultColor);
            else
                color = ColorUtility.ToHtmlStringRGB(LoadColor("CEL_EditorColor", defaultColor));
            var ussText = @".dockarea { background-color: #" + color + @" } .AppToolbar { background-color: #" + color + @" }";
            if(oldText == ussText) return;
            
            if(!Directory.Exists(styleSheetPath)) { // create folders if path not exists
                Directory.CreateDirectory(styleSheetPath.Substring(0, styleSheetPath.Length - "/Extensions/common.uss".Length));
                Directory.CreateDirectory(styleSheetPath.Substring(0, styleSheetPath.Length - "common.uss".Length));
            }
            File.WriteAllText(styleSheetPath, ussText);
            AssetDatabase.Refresh();
        }
        private static string GetStyleSheetPath() {
            return Directory.GetDirectories(Application.dataPath, "CleanEditorLayout", SearchOption.AllDirectories)[0] + "/StyleSheets/Extensions/common.uss";
        }
        private static void PreventStyleSheetDuplicates() {
            var otherStyleSheetPath = Directory.GetDirectories(Application.dataPath, "StyleSheets", SearchOption.AllDirectories);
            foreach(var path in otherStyleSheetPath) {
                if(!path.Contains("CleanEditorLayout")) {
                    var otherPath = path + "/Extensions/common.uss";
                    if(!Directory.Exists(otherPath)) {
                        File.WriteAllText(otherPath, "");
                        AssetDatabase.Refresh();
                    }
                }
            }
        }
        private class UpdateOnImport : AssetPostprocessor {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
                foreach(var assetPath in importedAssets) {
                    if(assetPath.EndsWith(".uss") && assetPath.Contains("CleanEditorLayout")) {
                        CleanEditorLayout.RepaintToolbar();
                        UpdateEditorColor();
                    }
                }
            }
        }
        public static bool IsUnitySixOrHigher() {
            if(int.TryParse(Application.unityVersion.Split('.')[0], out int version)) {
                return version >= 6000;
            }
            return false;
        }

        private static ReorderableList InitializeButtonsList(string key, bool isLeftSided) {
            var entries = LoadButtonEntries(key);
            var reorderableList = new ReorderableList(entries, typeof(ButtonEntryData), true, true, true, true);
            var lineHeight = EditorGUIUtility.singleLineHeight;

            reorderableList.drawHeaderCallback = (rect) => { };

            reorderableList.drawElementCallback = (rect, index, active, focused) => {
                var entry = entries[index];
                rect.y += 2;
                float fieldWidth = rect.width / 2.5f - 5;
                float typeFieldWidth = rect.width / 5 - 5;
                float offset1 = rect.x + typeFieldWidth + 5;
                float offset2 = rect.x + typeFieldWidth + 5 + fieldWidth + 5;
                var oldLabelWidth = EditorGUIUtility.labelWidth;
                string iconTooltip = "Load any icon available in the EditorGUIUtility.IconContent(string name) function. Just pass in the icon name.";
                string actionTooltip = "Refer to the documentation on how to create your own button actions.";

                EditorGUI.BeginChangeCheck();

                entry.enabled = EditorGUI.Toggle(new Rect(rect.x, rect.y, 20, lineHeight), entry.enabled);
                if(!entry.enabled)
                    GUI.enabled = false;
                entry.type = (Type)EditorGUI.EnumPopup(new Rect(rect.x+20, rect.y, typeFieldWidth-20, lineHeight), entry.type);
                if(entry.type == Type.Button) {
                    EditorGUIUtility.labelWidth = 62;
                    entry.alignment = (Alignment)EditorGUI.EnumPopup(new Rect(offset1, rect.y, fieldWidth, lineHeight), "Alignment", entry.alignment);
                    EditorGUIUtility.labelWidth = 40;
                    entry.width = EditorGUI.DelayedIntField(new Rect(offset2, rect.y, fieldWidth, lineHeight), "Width", entry.width);
                    EditorGUIUtility.labelWidth = 80;
                    entry.displayMode = (DisplayMode)EditorGUI.EnumPopup(new Rect(offset1, rect.y + lineHeight + 5, fieldWidth, lineHeight), "Display Mode", entry.displayMode);
                    if(entry.displayMode == DisplayMode.Text) {
                        EditorGUIUtility.labelWidth = 40;
                        entry.label = EditorGUI.DelayedTextField(new Rect(offset2, rect.y + lineHeight + 5, fieldWidth, lineHeight), "Label", entry.label);
                        if(entry.label != "Console Log") {
                            EditorGUIUtility.labelWidth = 108;
                            entry.backgroundMode = (BackgroundMode)EditorGUI.EnumPopup(new Rect(offset1, rect.y + 2*(lineHeight + 5), fieldWidth, lineHeight), "Background Mode", entry.backgroundMode);
                            EditorGUIUtility.labelWidth = 84;
                            entry.action = EditorGUI.DelayedTextField(new Rect(offset2, rect.y + 2*(lineHeight + 5), fieldWidth, lineHeight), new GUIContent("Button Action", actionTooltip), entry.action);
                        }
                    }
                    else if(entry.displayMode == DisplayMode.Icon) {
                        EditorGUIUtility.labelWidth = 32;
                        entry.label = EditorGUI.DelayedTextField(new Rect(offset2, rect.y + lineHeight + 5, fieldWidth, lineHeight), new GUIContent("Icon", iconTooltip), entry.label);
                        if(entry.label != "Console Log") {
                            EditorGUIUtility.labelWidth = 108;
                            entry.backgroundMode = (BackgroundMode)EditorGUI.EnumPopup(new Rect(offset1, rect.y + 2*(lineHeight + 5), fieldWidth, lineHeight), "Background Mode", entry.backgroundMode);
                            EditorGUIUtility.labelWidth = 84;
                            entry.action = EditorGUI.DelayedTextField(new Rect(offset2, rect.y + 2*(lineHeight + 5), fieldWidth, lineHeight), new GUIContent("Button Action", actionTooltip), entry.action);
                        }
                    }
                    else if(entry.displayMode == DisplayMode.Both) {
                        EditorGUIUtility.labelWidth = 32;
                        entry.label = EditorGUI.DelayedTextField(new Rect(offset2, rect.y + lineHeight + 5, fieldWidth, lineHeight), new GUIContent("Icon", iconTooltip), entry.label);
                        if(entry.label != "Console Log") {
                            EditorGUIUtility.labelWidth = 40;
                            entry.labelBoth = EditorGUI.DelayedTextField(new Rect(offset2, rect.y + 2*(lineHeight + 5), fieldWidth, lineHeight), "Label", entry.labelBoth);
                            EditorGUIUtility.labelWidth = 32;
                            entry.iconBoth = EditorGUI.DelayedTextField(new Rect(offset2, rect.y + 3*(lineHeight + 5), fieldWidth, lineHeight), new GUIContent("Icon", iconTooltip), entry.iconBoth);
                            EditorGUIUtility.labelWidth = 108;
                            entry.backgroundMode = (BackgroundMode)EditorGUI.EnumPopup(new Rect(offset1, rect.y + 4*(lineHeight + 5), fieldWidth, lineHeight), "Background Mode", entry.backgroundMode);
                            EditorGUIUtility.labelWidth = 84;
                            entry.action = EditorGUI.DelayedTextField(new Rect(offset2, rect.y + 4*(lineHeight + 5), fieldWidth, lineHeight), new GUIContent("Button Action", actionTooltip), entry.action);
                        }
                    }

                    if(entry.label == "Console Log") {
                        EditorGUIUtility.labelWidth = 108;
                        entry.backgroundMode = (BackgroundMode)EditorGUI.EnumPopup(new Rect(offset1, rect.y + 2*(lineHeight + 5), fieldWidth, lineHeight), "Background Mode", entry.backgroundMode);
                        EditorGUI.BeginChangeCheck();
                        entry.showError = EditorGUI.ToggleLeft(new Rect(offset2 + 36, rect.y + 2*(lineHeight + 5), (fieldWidth - 36 + 5)/3, lineHeight), new GUIContent("errors", EditorGUIUtility.IconContent("console.erroricon.sml").image, "Show Errors"), entry.showError);
                        entry.showWarn = EditorGUI.ToggleLeft(new Rect(offset2 + 36 + (fieldWidth - 36 + 5)/3, rect.y + 2*(lineHeight + 5), (fieldWidth - 36 + 5)/3, lineHeight), new GUIContent("warnings", EditorGUIUtility.IconContent("console.warnicon.sml").image, "Show Warnings"), entry.showWarn);
                        entry.showLog = EditorGUI.ToggleLeft(new Rect(offset2 + 36 + 2*((fieldWidth - 36 + 5)/3), rect.y + 2*(lineHeight + 5), (fieldWidth - 36 + 5)/3, lineHeight), new GUIContent("logs", EditorGUIUtility.IconContent("console.infoicon.sml").image, "Show Logs"), entry.showLog);
                        if(EditorGUI.EndChangeCheck()) {
                            showError = entry.showError;
                            showWarn = entry.showWarn;
                            showLog = entry.showLog;
                            CleanEditorLayout.RepaintToolbar();
                        }
                    }
                }
                else if(entry.type == Type.Text) {
                    EditorGUIUtility.labelWidth = 62;
                    entry.alignment = (Alignment)EditorGUI.EnumPopup(new Rect(offset1, rect.y, fieldWidth, lineHeight), "Alignment", entry.alignment);
                    EditorGUIUtility.labelWidth = 40;
                    entry.width = EditorGUI.DelayedIntField(new Rect(offset2, rect.y, fieldWidth, lineHeight), "Width", entry.width);
                    EditorGUIUtility.labelWidth = 80;
                    entry.displayMode = (DisplayMode)EditorGUI.EnumPopup(new Rect(offset1, rect.y + lineHeight + 5, fieldWidth, lineHeight), "Display Mode", entry.displayMode);
                    if(entry.displayMode == DisplayMode.Text) {
                        EditorGUIUtility.labelWidth = 40;
                        entry.label = EditorGUI.DelayedTextField(new Rect(offset2, rect.y + lineHeight + 5, fieldWidth, lineHeight), "Label", entry.label);
                    }
                    else if(entry.displayMode == DisplayMode.Icon) {
                        EditorGUIUtility.labelWidth = 32;
                        entry.label = EditorGUI.DelayedTextField(new Rect(offset2, rect.y + lineHeight + 5, fieldWidth, lineHeight), new GUIContent("Icon", iconTooltip), entry.label);
                    }
                    else if(entry.displayMode == DisplayMode.Both) {
                        EditorGUIUtility.labelWidth = 32;
                        entry.label = EditorGUI.DelayedTextField(new Rect(offset2, rect.y + lineHeight + 5, fieldWidth, lineHeight), new GUIContent("Icon", iconTooltip), entry.label);
                        EditorGUIUtility.labelWidth = 40;
                        entry.labelBoth = EditorGUI.DelayedTextField(new Rect(offset2, rect.y + 2*(lineHeight + 5), fieldWidth, lineHeight), "Label", entry.labelBoth);
                        EditorGUIUtility.labelWidth = 32;
                        entry.iconBoth = EditorGUI.DelayedTextField(new Rect(offset2, rect.y + 3*(lineHeight + 5), fieldWidth, lineHeight), new GUIContent("Icon", iconTooltip), entry.iconBoth);
                    }
                }
                else if(entry.type == Type.Space) {
                    EditorGUIUtility.labelWidth = 62;
                    entry.alignment = (Alignment)EditorGUI.EnumPopup(new Rect(offset1, rect.y, fieldWidth, lineHeight), "Alignment", entry.alignment);
                    EditorGUIUtility.labelWidth = 40;
                    entry.width = EditorGUI.DelayedIntField(new Rect(offset2, rect.y, fieldWidth, lineHeight), "Width", entry.width);
                }
                
                if(EditorGUI.EndChangeCheck())
                    SaveButtonEntries(entries, key);
                EditorGUIUtility.labelWidth = oldLabelWidth;
                GUI.enabled = true;
            };

            reorderableList.elementHeightCallback = (index) => {
                if(entries[index].type == Type.Button) {
                    if(entries[index].displayMode == DisplayMode.Both && entries[index].label != "Console Log")
                        return 5*(lineHeight + 5);
                    else
                        return 3*(lineHeight + 5);
                }
                else if(entries[index].type == Type.Text) {
                    if(entries[index].displayMode == DisplayMode.Both)
                        return 4*(lineHeight + 5);
                    else
                        return 2*(lineHeight + 5);
                }
                else
                    return lineHeight + 5;
            };

            reorderableList.onAddDropdownCallback = (buttonRect, list) => {
                var alignment = isLeftSided ? Alignment.Left : Alignment.Right;

                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Button"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 50, DisplayMode.Text, "Button", ""));
                menu.AddItem(new GUIContent("Text"), false, () => AddButtonEntry(entries, key, Type.Text, alignment, 50, DisplayMode.Text, "Text", ""));
                menu.AddItem(new GUIContent("Space"), false, () => AddButtonEntry(entries, key, Type.Space, alignment, 50, DisplayMode.Text, "", ""));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Unity Logo"), false, () => AddButtonEntry(entries, key, Type.Text, alignment, 70, DisplayMode.Both, "UnityLogo", "", BackgroundMode.Default, "Unity"));
                menu.AddItem(new GUIContent("Console Log"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 300, DisplayMode.Both, "Console Log", "Console Log", BackgroundMode.Transparent));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Account"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 95, DisplayMode.Both, "Account", "Account", BackgroundMode.Default, "Account", "icon dropdown"));
                menu.AddItem(new GUIContent("Asset Store"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 115, DisplayMode.Both, "Asset Store", "Asset Store", BackgroundMode.Default, "Asset Store", "icon dropdown"));
                menu.AddItem(new GUIContent("Layout"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 87, DisplayMode.Both, "Layout", "Layout", BackgroundMode.Default, "Layout", "icon dropdown"));
                menu.AddItem(new GUIContent("Multiplayer/Multiplayer Play Mode - Scenario"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 104, DisplayMode.Both, "Multiplayer Scenario", "Multiplayer Scenario", BackgroundMode.Default, "Multiplayer Scenario", "icon dropdown"));
                menu.AddItem(new GUIContent("Multiplayer/Multiplayer Play Mode - Status"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 96, DisplayMode.Both, "Multiplayer Status", "Multiplayer Status", BackgroundMode.Default, "Multiplayer Status", "icon dropdown"));
                menu.AddItem(new GUIContent("Multiplayer/Dedicated Server - Roles"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 46, DisplayMode.Both, "Multiplayer Roles", "Multiplayer Roles", BackgroundMode.Default, "", "icon dropdown"));
                menu.AddItem(new GUIContent("Quality Level"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 86, DisplayMode.Both, "BuildSettings.DedicatedServer", "Quality Level", BackgroundMode.Default, "Quality", "icon dropdown"));
                menu.AddItem(new GUIContent("Maximize"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 66, DisplayMode.Text, "Maximize", "Maximize"));
                #if UNITY_6000_3_OR_NEWER
                menu.AddItem(new GUIContent("AI"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 58, DisplayMode.Both, "AISparkle Icon", "AI", BackgroundMode.Default, "AI", "icon dropdown"));
                #endif
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Version Control"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 32, DisplayMode.Icon, "UnityEditor.VersionControl", "Unity Version Control"));
                menu.AddItem(new GUIContent("Package Manager"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 32, DisplayMode.Icon, "Package Manager", "Package Manager"));
                menu.AddItem(new GUIContent("Undo History"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 32, DisplayMode.Icon, "UndoHistory", "Undo History"));
                menu.AddItem(new GUIContent("Search"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 32, DisplayMode.Icon, "Search Icon", "Search"));
                menu.AddItem(new GUIContent("Background Tasks"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 32, DisplayMode.Icon, "Background Tasks", "Background Tasks"));
                menu.AddItem(new GUIContent("Lighting"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 32, DisplayMode.Icon, "Lighting", "Lighting"));
                menu.AddItem(new GUIContent("Debugger"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 32, DisplayMode.Icon, "Debugger", "Debugger"));
                menu.AddItem(new GUIContent("Cache Server"), false, () => AddButtonEntry(entries, key, Type.Button, alignment, 32, DisplayMode.Icon, "Cache Server", "Cache Server"));
                menu.DropDown(new Rect(buttonRect.x-4, buttonRect.yMax+2, 0, 0));
            };

            reorderableList.onRemoveCallback = (list) => {
                RemoveButtonEntry(entries, key, reorderableList.index);
            };

            reorderableList.onReorderCallback = (list) => {
                SaveButtonEntries(entries, key);
            };

            return reorderableList;
        }
        public static List<ButtonEntryData> LoadButtonEntries(string key) {
            List<ButtonEntryData> entries = new List<ButtonEntryData>();
            if(EditorPrefs.HasKey(key)) {
                string value = EditorPrefs.GetString(key);
                var json = JsonUtility.FromJson<ButtonEntryDataWrapper>(value);
                if(json != null)
                    return json.entries;
            }
            return entries;
        }
        private static void AddButtonEntry(List<ButtonEntryData> entries, string key, Type type, Alignment alignment, int width, DisplayMode displayMode, string label, string action, BackgroundMode backgroundMode = BackgroundMode.Default, string labelBoth = "", string iconBoth = "", bool enabled = true) {
            entries.Add(new ButtonEntryData { type = type, alignment = alignment, width = width, label = label, action = action, backgroundMode = backgroundMode, displayMode = displayMode, labelBoth = labelBoth, iconBoth = iconBoth, enabled = enabled, showError = true, showWarn = false, showLog = true });
            SaveButtonEntries(entries, key);
            ConsoleLogCheck(entries, key);
            BackgroundTasksCheck(entries);
        }
        private static void RemoveButtonEntry(List<ButtonEntryData> entries, string key, int index) {
            entries.RemoveAt(index);
            SaveButtonEntries(entries, key);
            ConsoleLogCheck(entries, key);
            BackgroundTasksCheck(entries);
        }
        private static void SaveButtonEntries(List<ButtonEntryData> entries, string key) {
            string json = JsonUtility.ToJson(new ButtonEntryDataWrapper { entries = entries });
            EditorPrefs.SetString(key, json);
            CleanEditorLayout.RepaintToolbar();
            ConsoleLogCheck(entries, key);
            BackgroundTasksCheck(entries);
        }
        private static void ConsoleLogCheck(List<ButtonEntryData> entries, string key) {
            bool hasConsoleLog = false;
            foreach(var entry in entries) {
                if(entry.label == "Console Log" && entry.enabled)
                    hasConsoleLog = true;
            }
            if(key == "CEL_LeftButtonsList")
                EditorPrefs.SetBool("CEL_HasConsoleLogLeft", hasConsoleLog);
            else
                EditorPrefs.SetBool("CEL_HasConsoleLogRight", hasConsoleLog);
        }
        private static void BackgroundTasksCheck(List<ButtonEntryData> entries) {
            bool hasBackgroundTasks = false;
            foreach(var entry in entries) {
                if(entry.label == "Background Tasks" && entry.enabled)
                    hasBackgroundTasks = true;
            }
            EditorPrefs.SetBool("CEL_HasBackgroundTasks", hasBackgroundTasks);
        } 
        [Serializable] public class ButtonEntryData {
            public bool enabled;
            public Type type;
            public Alignment alignment;
            public int width;
            public string label;
            public string iconBoth;
            public string labelBoth;
            public string action;
            public DisplayMode displayMode;
            public BackgroundMode backgroundMode;
            public bool showError, showWarn, showLog;
        }
        [Serializable] private class ButtonEntryDataWrapper {
            public List<ButtonEntryData> entries;
        }
        
        public static ReorderableList InitializeDropdownsList(string key) {
            var entries = LoadDropdownEntries(key, true);
            var reorderableList = new ReorderableList(entries, typeof(DropdownEntryData), true, true, false, false);
            var lineHeight = EditorGUIUtility.singleLineHeight;

            reorderableList.drawHeaderCallback = (rect) => { };

            reorderableList.drawElementCallback = (rect, index, active, focused) => {
                var entry = entries[index];
                if(entry.notInProject) GUI.enabled = false;
                float fieldWidth = rect.width;
                rect.y += 2;

                EditorGUI.BeginChangeCheck();

                entry.enabled = EditorGUI.Toggle(new Rect(rect.x, rect.y, 20, lineHeight), entry.enabled);
                if(!entry.notInProject) entry.foldout = EditorGUI.Foldout(new Rect(rect.x+20, rect.y, fieldWidth-20, EditorGUIUtility.singleLineHeight), entry.foldout, "");
                EditorGUI.LabelField(new Rect(rect.x+38, rect.y, fieldWidth-38, EditorGUIUtility.singleLineHeight), entry.categoryName);
                if(entry.subMenuEntries != null && entry.foldout) {
                    var offset = 0;
                    for(int i = 0; i < entry.subMenuEntries.Count; i++) {
                        if(entry.subMenuEntries[i].notInProject) continue;
                        entry.subMenuEntries[i].enabled = EditorGUI.Toggle(new Rect(rect.x+20, rect.y + (offset+1)*(lineHeight + 5), fieldWidth-20, EditorGUIUtility.singleLineHeight), entry.subMenuEntries[i].enabled);
                        EditorGUI.LabelField(new Rect(rect.x+38, rect.y + (offset+1)*(lineHeight + 5), fieldWidth-38, EditorGUIUtility.singleLineHeight), entry.subMenuEntries[i].subMenuName);
                        offset++;
                    }
                }

                if(EditorGUI.EndChangeCheck()) {
                    SaveDropdownEntries(entries, key);
                }
                GUI.enabled = true;
            };

            reorderableList.elementHeightCallback = (index) => {
                var itemCount = 0;
                foreach(var entry in entries[index].subMenuEntries)
                    if(!entry.notInProject) itemCount++;
                return entries[index].foldout ? (itemCount+1)*(lineHeight + 5) : lineHeight + 5;
            };

            reorderableList.onReorderCallback = (list) => {
                SaveDropdownEntries(entries, key);
            };

            return reorderableList;
        }
        public static List<DropdownEntryData> LoadDropdownEntries(string key, bool isInitializing) {
            List<DropdownEntryData> entries = new List<DropdownEntryData>();
            var hMenu = Win32.GetMenu(CleanEditorLayout.GetMainWindow());
            var menuCount = Win32.GetMenuItemCount(hMenu);
            var menuItemName = new System.Text.StringBuilder(256);

            // update dropdowns list to latest version
            if(isInitializing || menuCount != -1 && (menuCount == 0 || menuCount != EditorPrefs.GetInt("CEL_LastMenuCount"))) {
                
                // load existing dropdowns list
                List<DropdownEntryData> entriesOld = new List<DropdownEntryData>();
                if(EditorPrefs.HasKey(key)) {
                    string value = EditorPrefs.GetString(key);
                    var json = JsonUtility.FromJson<DropdownEntryDataWrapper>(value);
                    if(json != null)
                        entriesOld = json.entries;
                }

                // hide unavailable entries
                var availableMenusList = new List<string>();
                var availableSubMenusList = new List<string>();
                for(int i = 0; i < entriesOld.Count; i++) {
                    for(int s = 0; s < menuCount; s++) {
                        Win32.GetMenuString(hMenu, s, menuItemName, menuItemName.Capacity, Win32.MF_BYPOSITION);
                        var menuCategory = menuItemName.ToString();
                        if(menuCategory == entriesOld[i].categoryName && !availableMenusList.Contains(menuCategory))
                            availableMenusList.Add(menuCategory);
                        
                        for(int t = 0; t < entriesOld[i].subMenuEntries.Count; t++) {
                            string[] subMenuEntriesCurrent = Unsupported.GetSubmenus(menuCategory);
                            for(int u = 0; u < subMenuEntriesCurrent.Length; u++) {
                                var partsOld = entriesOld[i].subMenuEntries[t].subMenuName.Split('/');
                                var subMenuNameOld = partsOld[0] + "/" + partsOld[1];
                                var partsCurrent = subMenuEntriesCurrent[u].Split('/');
                                var subMenuNameCurrent = partsCurrent[0] + "/" + partsCurrent[1];
                                if(subMenuNameOld == subMenuNameCurrent && !availableSubMenusList.Contains(subMenuNameCurrent))
                                    availableSubMenusList.Add(subMenuNameCurrent);
                            }
                        }
                    }
                }
                for(int i = 0; i < entriesOld.Count; i++) {
                    if(!availableMenusList.Contains(entriesOld[i].categoryName))
                        entriesOld[i].notInProject = true;
                    else
                        entriesOld[i].notInProject = false;

                    for(int t = 0; t < entriesOld[i].subMenuEntries.Count; t++) {
                        var parts = entriesOld[i].subMenuEntries[t].subMenuName.Split('/');
                        var subMenuName = parts[0] + "/" + parts[1];
                        if(!availableSubMenusList.Contains(subMenuName))
                            entriesOld[i].subMenuEntries[t].notInProject = true;
                        else
                            entriesOld[i].subMenuEntries[t].notInProject = false;
                    }
                }

                // add/override entries
                for(int i = 0; i < menuCount; i++) {
                    Win32.GetMenuString(hMenu, i, menuItemName, menuItemName.Capacity, Win32.MF_BYPOSITION);
                    var menuCategory = menuItemName.ToString();
                    string[] subMenuEntriesNew = Unsupported.GetSubmenus(menuCategory);
                    var preventDuplicates = new List<string>();

                    bool hasDropdown = false;
                    int dropdownIndex = 0;
                    for(int s = 0; s < entriesOld.Count; s++) {
                        if(entriesOld[s].categoryName == menuCategory) {
                            hasDropdown = true;
                            dropdownIndex = s;
                        }
                    }

                    if(hasDropdown) {
                        var subMenuEntries = entriesOld[dropdownIndex].subMenuEntries;
                        foreach(var subMenuEntry in subMenuEntries)
                            preventDuplicates.Add(subMenuEntry.subMenuName);

                        for(int s = 0; s < subMenuEntriesNew.Length; s++) {
                            var parts = subMenuEntriesNew[s].Split('/');
                            var subMenuName = parts[0] + "/" + parts[1];
                            var hasSubmenu = false;
                            var subMenuIndex = 0;
                            for(int t = 0; t < entriesOld[dropdownIndex].subMenuEntries.Count; t++) {
                                if(entriesOld[dropdownIndex].subMenuEntries[t].subMenuName == subMenuName) {
                                    hasSubmenu = true;
                                    subMenuIndex = t;
                                }
                            }

                            var entry = new SubMenuEntry();
                            if(hasSubmenu)
                                entry.enabled = entriesOld[dropdownIndex].subMenuEntries[subMenuIndex].enabled;
                            else
                                entry.enabled = true;
                            entry.subMenuName = subMenuName;

                            if(!preventDuplicates.Contains(entry.subMenuName)) {
                                subMenuEntries.Add(entry);
                                preventDuplicates.Add(entry.subMenuName);
                            }
                        }
                        OverrideDropdownEntry(entriesOld, key, dropdownIndex, entriesOld[dropdownIndex].enabled, menuCategory, false, subMenuEntries);
                    }
                    else {
                        var subMenuEntries = new List<SubMenuEntry>();
                        foreach(string item in subMenuEntriesNew) {
                            string[] parts = item.Split('/');
                            var entry = new SubMenuEntry();
                            entry.enabled = true;
                            entry.subMenuName = parts[0] + "/" + parts[1];
                            if(!preventDuplicates.Contains(entry.subMenuName)) {
                                subMenuEntries.Add(entry);
                                preventDuplicates.Add(entry.subMenuName);
                            }
                        }
                        AddDropdownEntry(entriesOld, key, true, menuCategory, false, subMenuEntries);
                    }
                }
            }
            if(menuCount != EditorPrefs.GetInt("CEL_LastMenuCount"))
                EditorPrefs.SetInt("CEL_LastMenuCount", menuCount);

            // load dropdowns list
            if(EditorPrefs.HasKey(key)) {
                string value = EditorPrefs.GetString(key);
                var json = JsonUtility.FromJson<DropdownEntryDataWrapper>(value);
                if(json != null)
                    return json.entries;
            }
            return entries;
        }
        private static void SaveDropdownEntries(List<DropdownEntryData> entries, string key) {
            string json = JsonUtility.ToJson(new DropdownEntryDataWrapper { entries = entries });
            EditorPrefs.SetString(key, json);
            CleanEditorLayout.RepaintToolbar();
        }
        private static void AddDropdownEntry(List<DropdownEntryData> entries, string key, bool enabled, string categoryName, bool foldout, List<SubMenuEntry> subMenuEntries) {
            entries.Add(new DropdownEntryData { enabled = enabled, categoryName = categoryName, foldout = foldout, subMenuEntries = subMenuEntries });
            SaveDropdownEntries(entries, key);
        }
        private static void OverrideDropdownEntry(List<DropdownEntryData> entries, string key, int index, bool enabled, string categoryName, bool foldout, List<SubMenuEntry> subMenuEntries) {
            entries.RemoveAt(index);
            entries.Insert(index, new DropdownEntryData { enabled = enabled, categoryName = categoryName, foldout = foldout, subMenuEntries = subMenuEntries });
            SaveDropdownEntries(entries, key);
        }
        [Serializable] public class DropdownEntryData {
            public bool notInProject;
            public bool enabled;
            public string categoryName;
            public bool foldout;
            public List<SubMenuEntry> subMenuEntries;
        }
        [Serializable] public class SubMenuEntry {
            public bool notInProject;
            public bool enabled;
            public string subMenuName;
        }
        [Serializable] private class DropdownEntryDataWrapper {
            public List<DropdownEntryData> entries;
        }
    }
}
#endif