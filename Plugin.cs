using BepInEx;
using BepInEx.Logging;
using Valve.VR;
using UnityEngine;
using MyBhapticsTactsuit;
using HarmonyLib;
namespace Bhaptics
{

    // Dependencies are for compatibility with other mods.
    [BepInDependency("com.eternalUnion.pluginConfigurator", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("xzxADIxzx.Jaket", BepInDependency.DependencyFlags.SoftDependency)]
    //[BepInDependency("com.whateverusername0.vrtrakill", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        public static TactsuitVR tactsuitVr;
        public static Vector3 playerPosition;
        private void Awake()
        {
            // Make my own logger so it can be accessed from the Tactsuit class
            Log = base.Logger;
            // Plugin startup logic
            Logger.LogMessage("Plugin H3VR_bhaptics is loaded!");
            tactsuitVr = new TactsuitVR();
            // one startup heartbeat so you know the vest works correctly
            tactsuitVr.PlaybackHaptics("HeartBeat");
            // patch all functions
            var harmony = new Harmony("bhaptics.patch.ultrakill");
            harmony.PatchAll();
        }
        [HarmonyPatch(typeof(NewMovement), "GetHurt")]
        public class BhapticsAdditions
        {
            [HarmonyPostfix]
            public static void Postfix(
                NewMovement __instance,
                int damage,
                bool invincible,
                float scoreLossMultiplier = 1f,
                bool explosion = false,
                bool instablack = false)
            {
                if (explosion)
                {
                    tactsuitVr.PlaybackHaptics("ExplosionBelly",damage/100,1.5f);
                }
                else
                {
                    tactsuitVr.PlaybackHaptics("BulletHit", damage / 10, 1f);
                }
            }
        }
    }
}