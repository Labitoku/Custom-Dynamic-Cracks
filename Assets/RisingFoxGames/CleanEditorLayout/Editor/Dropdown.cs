#if UNITY_EDITOR_WIN
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using System;
using System.Linq;
using System.Collections.Generic;

namespace CleanEditorLayout
{
    class Dropdown : EditorWindow
    {
        #region variables
            private class MenuItem {
                public string label;
                public Action action;
                public string actionString;
                public List<MenuItem> subMenu;
            }
            private Rect rect;
            private List<MenuItem> items;
            private static Dropdown dropdown;
            private static List<Dropdown> subMenus;
            private static float checkmarkWidth = 12f;
            private static float arrowWidth = 15f;
            private static float defaultPaddingX = 5;
            private static float dropdownWidth = 0;
            private static List<float> subMenuWidths;
            private static bool layoutSave;
            private static Action layoutSaveAction;
        #endregion

        private void OnGUI() {
            if(items == null || items.Count == 0) return;
            if(subMenus == null) subMenus = new List<Dropdown>();


            for(int i = 0; i < items.Count; i++) {
                // style values
                GUIStyle buttonStyle = new GUIStyle(EditorStyles.toolbarButton) { stretchHeight = true };
                GUIStyle labelStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft } ;
                GUIStyle checkmarkStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter };
                GUIStyle arrowStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 9 };
                GUIStyle shortcutStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight } ;
                if(!Menu.GetEnabled(items[i].actionString)) { labelStyle.normal.textColor *= 0.75f; labelStyle.hover.textColor *= 0.75f; }
                shortcutStyle.normal.textColor *= 0.75f;
                shortcutStyle.hover.textColor *= 0.75f;

                // shortcut values
                string shortcut = "";
                var availableShortcuts = ShortcutManager.instance.GetAvailableShortcutIds();
                foreach(var item in availableShortcuts) {
                    if(items[i].actionString.Contains(item) || item.Contains(items[i].actionString)) {
                        shortcut = ShortcutManager.instance.GetShortcutBinding(item).ToString();
                        break;
                    }
                }
                foreach(var item in availableShortcuts) {
                    if(shortcut == "" && items[i].actionString.Contains("CleanEditorLayout/Maximize View")) {
                        shortcut = "No Shortcut Assigned!";
                        break;
                    }
                }
                float shortcutWidth = GUI.skin.label.CalcSize(new GUIContent(shortcut)).x;
                
            // draw dropdown
                Color oldBackroundColor = GUI.backgroundColor;
                GUI.backgroundColor *= Preferences.DropdownBrightness;
                var paddingX = defaultPaddingX + Preferences.DropdownPaddingX;
                if(i == 0 || i == items.Count-1) 
                    buttonStyle.fixedHeight = EditorStyles.toolbarButton.fixedHeight + Preferences.DropdownPaddingY + Preferences.DropdownOffsetTopBottom;
                else
                    buttonStyle.fixedHeight = EditorStyles.toolbarButton.fixedHeight + Preferences.DropdownPaddingY;
                var offsetY = 0;
                if(i == 0)
                    offsetY = Preferences.DropdownOffsetTopBottom/2;
                else if(i == items.Count-1)
                    offsetY = -Preferences.DropdownOffsetTopBottom/2;

                Rect rect = GUILayoutUtility.GetRect(new GUIContent(), buttonStyle);
                if(items[i].subMenu != null) { // does not have submenu
                    GUI.Button(rect, "", buttonStyle);
                    GUI.Label(new Rect(rect.x + checkmarkWidth + paddingX, rect.y + offsetY, rect.width - checkmarkWidth - arrowWidth - (2 * paddingX), rect.height), items[i].label, labelStyle);
                    GUI.Label(new Rect(rect.xMax - arrowWidth - paddingX, rect.y + offsetY, 20, rect.height), "▶", arrowStyle);
                }
                else { // has submenu
                    var shortcutText = shortcut == "" ? "" : shortcut;
                    if(GUI.Button(rect, "", buttonStyle)) {
                        if(items[i].actionString.Contains("Save Layout")) {
                            layoutSaveAction = items[i].action;
                            layoutSave = true;
                        }
                        else
                            items[i].action?.Invoke(); // call submenu action
                        CloseAllDropdowns();
                    }
                    GUI.Label(new Rect(rect.x + checkmarkWidth + paddingX, rect.y + offsetY, rect.width - checkmarkWidth - arrowWidth - (2 * paddingX), rect.height), items[i].label, labelStyle);
                    if(Menu.GetChecked(items[i].actionString))
                        GUI.Label(new Rect(rect.x + (paddingX/2), rect.y + offsetY, checkmarkWidth, rect.height), EditorGUIUtility.IconContent("d_FilterSelectedOnly"), checkmarkStyle);
                    GUI.Label(new Rect(rect.xMax - shortcutWidth - paddingX, rect.y + offsetY, shortcutWidth, rect.height), shortcutText, shortcutStyle);
                }
                GUI.backgroundColor = oldBackroundColor;

            // open submenu
                if(rect.Contains(Event.current.mousePosition)) {
                    if(items[i].subMenu != null) {
                        Repaint();
                        ShowSubMenu(items[i].subMenu, i, position);
                    }
                    else {
                        CloseSubMenus(items);
                    }
                }
            }
            
            // fix
            if(layoutSave) {
                #if UNITY_6000_3_OR_NEWER
                    layoutSaveAction?.Invoke();
                    EditorApplication.delayCall += ClearConsole;
                #else
                    Toolbar.CloseToolbar();
                    CloseAllDropdowns();
                    layoutSaveAction?.Invoke();
                #endif
                layoutSave = false;
            }
        }
        public static void ShowDropdown(string categoryName, List<Preferences.SubMenuEntry> subMenuEntries, Vector2 position, bool buttonPressed) {
            if(buttonPressed) CloseAllDropdowns();
            if(dropdown == null)
                dropdown = CreateInstance<Dropdown>();
            dropdown.items = CreateMenuItemList(categoryName, LoadSortedDropDownCommands(categoryName, subMenuEntries));
            
            dropdownWidth = GetTargetMenuWidth(dropdown.items);
            var dropDownSize = new Vector2(dropdownWidth, dropdown.items.Count * (20 + Preferences.DropdownPaddingY) + 2 * Preferences.DropdownOffsetTopBottom);
            dropdown.ShowAsDropDown(new Rect(position, Vector2.zero), dropDownSize);
            dropdown.maxSize = dropDownSize;
        }
        private static void ShowSubMenu(List<MenuItem> subMenuItems, int i, Rect windowRect) {
            int depthLevel = subMenuItems[0].actionString.Split('/').Length - 3;

            if(subMenus.Count <= depthLevel)
                subMenus.Add(null);
            if(subMenus[depthLevel] == null)
                subMenus[depthLevel] = CreateInstance<Dropdown>();

            subMenus[depthLevel].items = subMenuItems;

            var subMenuWidth = GetTargetMenuWidth(subMenus[depthLevel].items);
            if(subMenuWidths == null) subMenuWidths = new List<float>();
            if(subMenuWidths.Count <= depthLevel) subMenuWidths.Add(0);
            subMenuWidths[depthLevel] = subMenuWidth;

            var cleanEditorMainWindow = CleanEditorLayout.GetCleanEditorWindow();
            var editorScaling = EditorGUIUtility.pixelsPerPoint;
            var currentMonitor = Win32.MonitorFromWindow(cleanEditorMainWindow, Win32.MONITOR_DEFAULTTONEAREST);
            Win32.GetDpiForMonitor(currentMonitor, Win32.MDT_EFFECTIVE_DPI, out uint dpiCurrent, out uint dpiY);
            var toolbarHeight = Win32.GetSystemMetricsForDpi(Win32.SM_CYCAPTION, dpiCurrent) / editorScaling;
            var editorHeight = Win32.GetMonitorSize(currentMonitor).y / editorScaling - toolbarHeight * 4;
            var dropDownSize = new Vector2(subMenuWidth,  Mathf.Clamp(subMenus[depthLevel].items.Count * (20 + Preferences.DropdownPaddingY) + Preferences.DropdownOffsetTopBottom * (subMenuItems.Count !=1 ? 2f : 1f), 0, editorHeight));
            var positionOffset = depthLevel == 0 ? dropdownWidth : subMenuWidths[depthLevel - 1];
            var posY = windowRect.y + i * (20 + Preferences.DropdownPaddingY);
            if(dropDownSize.y+posY >= editorHeight) posY = editorHeight-dropDownSize.y + toolbarHeight;
            Vector2 submenuPosition = new Vector2(windowRect.x + positionOffset, posY);
            subMenus[depthLevel].ShowAsDropDown(new Rect(submenuPosition, Vector2.zero), dropDownSize);
            subMenus[depthLevel].maxSize = dropDownSize;
        }
        
        private static Dictionary<int, List<string>> LoadSortedDropDownCommands(string dropdownName, List<Preferences.SubMenuEntry> subMenuEntries) {

                // extract menu commands
                var menuItems = new List<string>();
                string[] allItems = Unsupported.GetSubmenus(dropdownName);

                foreach(string item in allItems) {
                    // skip if disabled in preferences
                    if(subMenuEntries.Any(entry => (item + "/").Contains(entry.subMenuName + "/") && !entry.enabled)) continue;

                    string[] parts = item.Split(new char[] { '/' }, 2);
                    string command = parts.Length > 1 ? parts[1] : "";
                    menuItems.Add(command);
                }

                // sort into depth levels
                var menuLevels = new Dictionary<int, List<string>>();
                foreach(string menuItem in menuItems) {
                    string[] parts = menuItem.Split('/');
                    for(int depth = 1; depth <= parts.Length; depth++) {
                        string partialPath = string.Join("/", parts, 0, depth);

                        if(!menuLevels.ContainsKey(depth))
                            menuLevels[depth] = new List<string>();
                        if(!menuLevels[depth].Contains(partialPath))
                            menuLevels[depth].Add(partialPath);
                    }
                }
                return menuLevels;
            }
        private static List<MenuItem> CreateMenuItemList(string categoryName, Dictionary<int, List<string>> menuLevels) {
            var menuItemsFinal = new List<MenuItem>();
            var currentLevel = 1;

            if(!menuLevels.ContainsKey(currentLevel)) return menuItemsFinal;

            foreach(var menuItem in menuLevels[currentLevel]) {
                List<MenuItem> subMenuItems = GetSubMenusRecursive(categoryName, currentLevel, menuLevels, menuItem);
                if(subMenuItems == null)
                    menuItemsFinal.Add(CreateMenuItem(categoryName, currentLevel-1, menuItem));
                else
                    menuItemsFinal.Add(CreateMenuItem(categoryName, currentLevel-1, menuItem, subMenuItems));
            }

            return menuItemsFinal;
        }
        private static List<MenuItem> GetSubMenusRecursive(string categoryName, int currentLevel, Dictionary<int, List<string>> menuLevels, string parentItem) {
            List<MenuItem> subMenuItems = null;

            if(!menuLevels.ContainsKey(currentLevel+1))
                return subMenuItems;

            foreach(var subItem in menuLevels[currentLevel+1]) {
                if(subItem.StartsWith(parentItem + "/")) {
                    List<MenuItem> subSubMenuItems = GetSubMenusRecursive(categoryName, currentLevel+1, menuLevels, subItem);
                    
                    if(subMenuItems == null)
                        subMenuItems = new List<MenuItem>();

                    subMenuItems.Add(CreateMenuItem(categoryName, currentLevel, subItem, subSubMenuItems));
                }
            }

            return subMenuItems;
        }
        private static MenuItem CreateMenuItem(string categoryName, int labelIndex, string menuItem, List<MenuItem> subMenuItems = null) {
            var label = menuItem.Contains("/") ? menuItem.Split('/')[labelIndex] : menuItem;
            Action action = delegate { EditorApplication.ExecuteMenuItem(categoryName + "/" + menuItem); };
            var newMenuItem = new MenuItem { label = label, action = action, actionString = categoryName + "/" + menuItem, subMenu = subMenuItems };
            return newMenuItem;
        }
        private static float GetTargetMenuWidth(List<MenuItem> items) {
            var menuWidth = 0f;
            var style = new GUIStyle(GUI.skin.label) { fixedWidth = 0, stretchWidth = false };
            for(int i = 0; i < items.Count; i++) {
                var width = 0f;
                width += style.CalcSize(new GUIContent(items[i].label)).x;

                string shortcut = "";
                var availableShortcuts = ShortcutManager.instance.GetAvailableShortcutIds();
                foreach(var item in availableShortcuts) {
                    if(items[i].actionString.Contains(item) || item.Contains(items[i].actionString)) {
                        shortcut = ShortcutManager.instance.GetShortcutBinding(item).ToString();
                        break;
                    }
                }
                foreach(var item in availableShortcuts) {
                    if(shortcut == "" && items[i].actionString.Contains("CleanEditorLayout/Maximize View")) {
                        shortcut = "No Shortcut Assigned!";
                        break;
                    }
                }
                width += style.CalcSize(new GUIContent(shortcut)).x;

                if(items[i].subMenu != null)
                    width += arrowWidth;

                width += checkmarkWidth + 4*(defaultPaddingX + Preferences.DropdownPaddingX);
                
                if(width > menuWidth)
                    menuWidth = width;
            }
            return menuWidth;
        }
        private static void CloseSubMenus(List<MenuItem> subMenuItems) {
            if(subMenus == null || subMenus.Count == 0) return;
            
            int depthLevel = subMenuItems[0].actionString.Split('/').Length-2;
            for(int i = subMenus.Count; i >= depthLevel; i--) {
                try{
                    if(subMenus[i] != null)
                        subMenus[i].Close();
                } catch {}
            }
        }
        public static void CloseAllDropdowns() {
            if(subMenus == null || subMenus.Count == 0) return;
            var allDropdowns = Resources.FindObjectsOfTypeAll<Dropdown>();
            
            for(int i = allDropdowns.Length; i >= 0; i--) {
                try{
                    if(allDropdowns[i] != null)
                        allDropdowns[i].Close();
                } catch {}
            }
        }
        private static void ClearConsole() {
            var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor");
            var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            clearMethod.Invoke(null, null);
        }
    }
}
#endif