using BepInEx;
using BepInEx.Logging;
using Valve.VR;
using UnityEngine;
using MyBhapticsTactsuit;
using HarmonyLib;
using static UnityEngine.Random;

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
        public class BhapticsNonExplosion
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
                if (!explosion)
                {
                    tactsuitVr.PlaybackHaptics("BulletHit", damage / 100, 1f);
                }
            }
        }
        [HarmonyPatch(typeof(Explosion), "Collide")]
        public class BhapticsExplosion
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state,Explosion __instance, Collider other)
            {
                if (MonoSingleton<NewMovement>.Instance.hp < __state){
                    //mmm linear scaling
                    float distance = Vector3.Distance(other.transform.position, __instance.transform.position);
                    float intensity = Mathf.Min(Mathf.Max(1 - (distance / 18), 0.1f),0.75f);
                    tactsuitVr.PlaybackHaptics("ExplosionBelly", intensity);
                    Log.LogMessage(intensity);
                }
            }
        }
    }
}