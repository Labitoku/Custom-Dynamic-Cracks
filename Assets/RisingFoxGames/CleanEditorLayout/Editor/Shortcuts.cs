#if UNITY_EDITOR_WIN
using UnityEngine;
using UnityEditor.ShortcutManagement;

namespace CleanEditorLayout
{
    internal static class Shortcuts
    {
        public static bool shortcutActive;
        public static string currentShortcut;

        [Shortcut("CleanEditorLayout/Toolbar/File", KeyCode.F, ShortcutModifiers.Alt)] private static void FileShortcut() => Toolbar.OpenDropdownWithShortcut("File");
        [Shortcut("CleanEditorLayout/Toolbar/Edit", KeyCode.E, ShortcutModifiers.Alt)] private static void EditShortcut() => Toolbar.OpenDropdownWithShortcut("Edit");
        [Shortcut("CleanEditorLayout/Toolbar/Assets", KeyCode.A, ShortcutModifiers.Alt)] private static void AssetsShortcut() => Toolbar.OpenDropdownWithShortcut("Assets");
        [Shortcut("CleanEditorLayout/Toolbar/GameObject", KeyCode.G, ShortcutModifiers.Alt)] private static void GameObjectShortcut() => Toolbar.OpenDropdownWithShortcut("GameObject");
        [Shortcut("CleanEditorLayout/Toolbar/Component", KeyCode.C, ShortcutModifiers.Alt)] private static void ComponentShortcut() => Toolbar.OpenDropdownWithShortcut("Component");
        [Shortcut("CleanEditorLayout/Toolbar/Services", KeyCode.S, ShortcutModifiers.Alt)] private static void ServicesShortcut() => Toolbar.OpenDropdownWithShortcut("Services");
        [Shortcut("CleanEditorLayout/Toolbar/Tools", KeyCode.T, ShortcutModifiers.Alt)] private static void ToolsShortcut() => Toolbar.OpenDropdownWithShortcut("Tools");
        [Shortcut("CleanEditorLayout/Toolbar/Jobs", KeyCode.J, ShortcutModifiers.Alt)] private static void JobsShortcut() => Toolbar.OpenDropdownWithShortcut("Jobs");
        [Shortcut("CleanEditorLayout/Toolbar/Window", KeyCode.W, ShortcutModifiers.Alt)] private static void WindowShortcut() => Toolbar.OpenDropdownWithShortcut("Window");
        [Shortcut("CleanEditorLayout/Toolbar/Help", KeyCode.H, ShortcutModifiers.Alt)] private static void HelpShortcut() => Toolbar.OpenDropdownWithShortcut("Help");
    }
}
#endif