using HarmonyLib;

namespace GameOfRush.Patches
{
    [HarmonyPatch(typeof(Reptile.Phone.Phone), "CloseCurrentApp")]
    internal static class PhoneClosePatch
    {
        private static bool Prefix()
        {
            if (GameOfRushPlayMode.IsActive || GameOfRushPlayMode.IsRunning)
                return false;

            return true;
        }
    }
}
