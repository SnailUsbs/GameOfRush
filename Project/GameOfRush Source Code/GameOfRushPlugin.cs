using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using GameOfRush.Phone;
using UnityEngine;

namespace GameOfRush
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("CommonAPI", BepInDependency.DependencyFlags.HardDependency)]
    public class GameOfRushPlugin : BaseUnityPlugin
    {
        private static string _modFolder;
        private Harmony _harmony;

        public static ConfigEntry<bool>   RunInBackground     { get; private set; }
        public static ConfigEntry<int>    GameSpeed           { get; private set; }
        public static ConfigEntry<int>    LcdOption           { get; private set; }

        public static string GetAppIconPath(string filename)
            => string.IsNullOrEmpty(_modFolder) ? null : Path.Combine(_modFolder, filename);

        private void Awake()
        {
            _modFolder = Path.GetDirectoryName(Info.Location);

            RunInBackground    = Config.Bind("Game Settings", "RunInBackground",    false, "Runs after you close the in-game phone");
            GameSpeed          = Config.Bind("Game Settings", "GameSpeed",          1,     "Emulation speed multiplier.");
            LcdOption          = Config.Bind("Game Settings", "LcdOption",          0,     "LCD color scheme.");

            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll();

            var playModeGo = new GameObject("GameOfRush_PlayMode");
            playModeGo.AddComponent<GameOfRushPlayMode>();

            AppGameOfRush.Initialize();
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID    = "com.GameOfRush";
        public const string PLUGIN_NAME    = "GameOfRush";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}
