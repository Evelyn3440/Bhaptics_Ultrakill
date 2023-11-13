using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using MyBhapticsTactsuit;
using HarmonyLib;
using System.Collections.Generic;
using Logic;

namespace Bhaptics
{
    /**
     * This code is designed to give someone a stroke
     * GOOD LUCK.
    **/
    // Dependencies are for compatibility with other mods.
    [BepInDependency("com.eternalUnion.pluginConfigurator", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("xzxADIxzx.Jaket", BepInDependency.DependencyFlags.SoftDependency)]
    //[BepInDependency("com.whateverusername0.vrtrakill", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        public static TactsuitVR tactsuitVr;

        private void Awake()
        {
            // Make my own logger so it can be accessed from the Tactsuit class
            Log = base.Logger;
            // Plugin startup logic
            Logger.LogMessage("Plugin Ultrakill_bhaptics is loaded!");
            tactsuitVr = new TactsuitVR();
            // one startup heartbeat so you know the vest works correctly
            tactsuitVr.PlaybackHaptics("HeartBeat");
            // patch all functions
            var harmony = new Harmony("bhaptics.patch.ultrakill");
            harmony.PatchAll();
        }
        private static void doHaptic(Vector3 Location, float intensity = 1.0f, float duration = 1.0f)
        {
            var angleShift = getAngleAndShift(Location);
            string feedbackKey = "BulletHit";
            tactsuitVr.PlayBackHit(feedbackKey, angleShift.Key, angleShift.Value, intensity, duration);
        }
        //STOLEN CODE!
        private static KeyValuePair<float, float> getAngleAndShift(Vector3 hit)
        {
            Transform player = MonoSingleton<NewMovement>.Instance.transform;
            // bhaptics starts in the front, then rotates to the left. 0° is front, 90° is left, 270° is right.
            // y is "up", z is "forward" in local coordinates
            Vector3 patternOrigin = new Vector3(0f, 0f, 1f);
            Vector3 hitPosition = hit - player.position;
            Quaternion PlayerRotation = player.rotation;
            Vector3 playerDir = PlayerRotation.eulerAngles;
            // get rid of the up/down component to analyze xz-rotation
            Vector3 flattenedHit = new Vector3(hitPosition.x, 0f, hitPosition.z);

            // get angle. .Net < 4.0 does not have a "SignedAngle" function...
            float earlyhitAngle = Vector3.Angle(flattenedHit, patternOrigin);
            // check if cross product points up or down, to make signed angle myself
            Vector3 earlycrossProduct = Vector3.Cross(flattenedHit, patternOrigin);
            if (earlycrossProduct.y > 0f) { earlyhitAngle *= -1f; }
            // relative to player direction
            float myRotation = earlyhitAngle - playerDir.y;
            // switch directions (bhaptics angles are in mathematically negative direction)
            myRotation *= -1f;
            // convert signed angle into [0, 360] rotation
            if (myRotation < 0f) { myRotation = 360f + myRotation; }

            // up/down shift is in y-direction
            float hitShift = hitPosition.y;
            // in H3VR, the TorsoTransform has y=0 at the neck,
            // and the torso ends at roughly -0.5 (that's in meters)
            // so cap the shift to [-0.5, 0]...
            if (hitShift > 0.0f) { hitShift = 0.5f; }
            else if (hitShift < -0.5f) { hitShift = -0.5f; }
            // ...and then spread/shift it to [-0.5, 0.5]
            else { hitShift = (hitShift + 0.5f) * 2.0f - 0.5f; }

            //tactsuitVr.LOG("Relative x-z-position: " + relativeHitDir.x.ToString() + " "  + relativeHitDir.z.ToString());
            //tactsuitVr.LOG("HitAngle: " + hitAngle.ToString());
            //tactsuitVr.LOG("HitShift: " + hitShift.ToString());

            // No tuple returns available in .NET < 4.0, so this is the easiest quickfix
            return new KeyValuePair<float, float>(myRotation, hitShift);
        }
        [HarmonyPatch(typeof(Explosion), "Collide")]
        public class ExplosionBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, Explosion __instance, Collider other)
            {
                if (MonoSingleton<NewMovement>.Instance.hp < __state)
                {
                    //mmm linear scaling
                    float distance = Vector3.Distance(other.transform.position, __instance.transform.position);
                    float intensity = Mathf.Min(Mathf.Max(1 - (distance / 18), 0.1f), 0.75f);
                    tactsuitVr.PlaybackHaptics("ExplosionBelly", intensity);
                }
            }
        }
        //Time for the hell of damage patches for a LOT of different stuff
        [HarmonyPatch(typeof(BeamgunBeam), "Update")]
        public class BeamgunBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, BeamgunBeam __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    LayerMask layerMask = LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment);
                    if (__instance.canHitPlayer && (double)__instance.playerDamageCooldown <= 0.0)
                        layerMask = LayerMaskDefaults.Get(LMD.EnemiesEnvironmentAndPlayer);
                    RaycastHit hitInfo;
                    if (Physics.Raycast(__instance.transform.position, __instance.transform.forward, out hitInfo, float.PositiveInfinity, (int)layerMask, QueryTriggerInteraction.Ignore))
                    {
                        doHaptic(hitInfo.point);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Projectile), "TimeToDie")]
        public class ProjectileBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, Projectile __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    doHaptic(__instance.transform.position);
                }
            }
        }
        [HarmonyPatch(typeof(BlackHoleProjectile), "OnTriggerEnter")]
        public class BlackHoleProjectileBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    if (MonoSingleton<NewMovement>.Instance.hp < __state)
                    {
                        tactsuitVr.PlaybackHaptics("ExplosionBelly", 0.5f);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Coin), "ShootAtPlayer")]
        public class CoinBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, Coin __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    doHaptic(__instance.transform.position);
                }
            }
        }
        [HarmonyPatch(typeof(ContinuousBeam), "Update")]
        public class ContinuousBeamBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, ContinuousBeam __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    Vector3 zero = Vector3.zero;
                    RaycastHit hitInfo;
                    Vector3 vector3 = !Physics.Raycast(__instance.transform.position, __instance.transform.forward, out hitInfo, float.PositiveInfinity, (int)__instance.environmentMask) ? __instance.transform.position + __instance.transform.forward * 999f : hitInfo.point;
                    __instance.lr.SetPosition(0, __instance.transform.position);
                    __instance.lr.SetPosition(1, vector3);
                    if ((bool)(Object)__instance.impactEffect)
                        __instance.impactEffect.transform.position = vector3;
                    RaycastHit[] raycastHitArray = Physics.SphereCastAll(__instance.transform.position + __instance.transform.forward * 0.35f, 0.35f, __instance.transform.forward, Vector3.Distance(__instance.transform.position, vector3) - 0.35f, (int)__instance.hitMask);
                    if (raycastHitArray != null && raycastHitArray.Length != 0)
                    {
                        for (int index = 0; index < raycastHitArray.Length; ++index)
                        {
                            if (raycastHitArray[index].collider.gameObject.tag == "Player" && __instance.canHitPlayer && (double)__instance.playerCooldown <= 0.0)
                            {
                                //Christ thats a lot
                                doHaptic(raycastHitArray[index].point);
                            }
                        }
                    }
                }
            }

        }
        [HarmonyPatch(typeof(DeathZone), "GotHit")]
        public class DeathZoneBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, DeathZone __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    doHaptic(__instance.transform.position);
                }
            }
        }
        [HarmonyPatch(typeof(FireZone), "OnTriggerStay")]
        public class FireZoneBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, DeathZone __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    doHaptic(__instance.transform.position);
                }
            }
        }
        [HarmonyPatch(typeof(HurtZone), "FixedUpdate")]
        public class HurtZoneBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, HurtZone __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    doHaptic(__instance.transform.position);
                }
            }
        }
        [HarmonyPatch(typeof(MassSpear), "OnTriggerEnter")]
        public class MassSpearBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, Projectile __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    doHaptic(__instance.transform.position);
                }
            }
        }
        [HarmonyPatch(typeof(MinosPrime), "DropAttackActivate")]
        public class MinosPrimeBhaptics
        {
            [HarmonyPostfix]
            public static void Postfix(MinosPrime __instance)
            {
                RaycastHit hitInfo;
                Physics.Raycast(__instance.aimingBone.position, Vector3.down, out hitInfo, 250f, (int)LayerMaskDefaults.Get(LMD.Environment));
                LineRenderer component1 = Object.Instantiate<GameObject>(__instance.attackTrail, __instance.aimingBone.position, __instance.transform.rotation).GetComponent<LineRenderer>();
                component1.SetPosition(0, __instance.aimingBone.position);
                RaycastHit[] raycastHitArray = Physics.SphereCastAll(__instance.aimingBone.position, 5f, Vector3.down, Vector3.Distance(__instance.aimingBone.position, hitInfo.point), (int)LayerMaskDefaults.Get(LMD.EnemiesAndPlayer));
                bool flag = false;
                List<EnemyIdentifier> enemyIdentifierList = new List<EnemyIdentifier>();
                foreach (RaycastHit raycastHit in raycastHitArray)
                {
                    if (raycastHit.collider.gameObject.tag == "Player" && !flag)
                    {
                        doHaptic(raycastHit.point);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(MinosPrime), "RiderKickActivate")]
        public class MinosPrimeKickBhaptics
        {
            [HarmonyPostfix]
            public static void Postfix(MinosPrime __instance)
            {
                RaycastHit hitInfo;
                Physics.Raycast(__instance.aimingBone.position, __instance.transform.forward, out hitInfo, 250f, (int)LayerMaskDefaults.Get(LMD.Environment));
                LineRenderer component1 = Object.Instantiate<GameObject>(__instance.attackTrail, __instance.aimingBone.position, __instance.transform.rotation).GetComponent<LineRenderer>();
                component1.SetPosition(0, __instance.aimingBone.position);
                RaycastHit[] raycastHitArray = Physics.SphereCastAll(__instance.aimingBone.position, 5f, __instance.transform.forward, Vector3.Distance(__instance.aimingBone.position, hitInfo.point), (int)LayerMaskDefaults.Get(LMD.EnemiesAndPlayer));
                bool flag = false;
                foreach (RaycastHit raycastHit in raycastHitArray)
                {
                    if (raycastHit.collider.gameObject.tag == "Player" && !flag)
                    {
                        doHaptic(raycastHit.point);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Nail), "OnCollisionEnter")]
        public class NailBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, Nail __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    doHaptic(__instance.transform.position);
                }
            }
        }
        [HarmonyPatch(typeof(PhysicalShockwave), "CheckCollision")]
        public class ShockWaveBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    tactsuitVr.PlaybackHaptics("ExplosionBelly", 0.25f);
                }
            }
        }
        [HarmonyPatch(typeof(RevolverBeam), "ExecuteHits")]
        public class RevolverBeamBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, RaycastHit currentHit)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    doHaptic(currentHit.point);
                }
            }
        }
        [HarmonyPatch(typeof(SisyphusPrime), "DropAttackActivate")]
        public class SisyphusPrimeBhaptics
        {
            [HarmonyPostfix]
            public static void Postfix(SisyphusPrime __instance)
            {
                RaycastHit hitInfo;
                Physics.Raycast(__instance.aimingBone.position, Vector3.down, out hitInfo, 250f, (int)LayerMaskDefaults.Get(LMD.Environment));
                LineRenderer component1 = Object.Instantiate<GameObject>(__instance.attackTrail, __instance.aimingBone.position, __instance.transform.rotation).GetComponent<LineRenderer>();
                component1.SetPosition(0, __instance.aimingBone.position);
                RaycastHit[] raycastHitArray = Physics.SphereCastAll(__instance.aimingBone.position, 5f, Vector3.down, Vector3.Distance(__instance.aimingBone.position, hitInfo.point), (int)LayerMaskDefaults.Get(LMD.EnemiesAndPlayer));
                foreach (RaycastHit raycastHit in raycastHitArray)
                {
                    if (raycastHit.collider.gameObject.tag == "Player")
                    {
                        doHaptic(raycastHit.point);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(SisyphusPrime), "RiderKickActivate")]
        public class SisyphusPrimeKickBhaptics
        {
            [HarmonyPostfix]
            public static void Postfix(SisyphusPrime __instance)
            {
                RaycastHit hitInfo;
                Physics.Raycast(__instance.aimingBone.position, __instance.transform.forward, out hitInfo, 250f, (int)LayerMaskDefaults.Get(LMD.Environment));
                LineRenderer component1 = Object.Instantiate<GameObject>(__instance.attackTrail, __instance.aimingBone.position, __instance.transform.rotation).GetComponent<LineRenderer>();
                component1.SetPosition(0, __instance.aimingBone.position);
                RaycastHit[] raycastHitArray = Physics.SphereCastAll(__instance.aimingBone.position, 5f, __instance.transform.forward, Vector3.Distance(__instance.aimingBone.position, hitInfo.point), (int)LayerMaskDefaults.Get(LMD.EnemiesAndPlayer));
                foreach (RaycastHit raycastHit in raycastHitArray)
                {
                    if (raycastHit.collider.gameObject.tag == "Player")
                    {
                        doHaptic(raycastHit.point);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(SwingCheck2), "CheckCollision")]
        public class SwingCheck2Bhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, SwingCheck2 __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    doHaptic(__instance.transform.position);
                }
            }
        }
        [HarmonyPatch(typeof(ThrownSword), "OnTriggerEnter")]
        public class ThrownSwordBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, ThrownSword __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    doHaptic(__instance.transform.position);
                }
            }
        }
        [HarmonyPatch(typeof(VirtueInsignia), "OnTriggerEnter")]
        public class VirtueInsigniaBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, VirtueInsignia __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    doHaptic(__instance.transform.position);
                }
            }
        }
        [HarmonyPatch(typeof(Wicked), "OnCollisionEnter")]
        public class WickedBhaptics
        {
            [HarmonyPrefix]
            public static void Prefix(out int __state)
            {
                //This is bad. Really bad. but I'm gonna go with it!
                __state = MonoSingleton<NewMovement>.Instance.hp;
            }
            [HarmonyPostfix]
            public static void Postfix(int __state, Wicked __instance)
            {
                int damage = __state - MonoSingleton<NewMovement>.Instance.hp;
                if (damage > 0)
                {
                    tactsuitVr.PlaybackHaptics("ExplosionBelly", 1f);
                }
            }
        }
        [HarmonyPatch(typeof(SceneHelper), "LoadScene")]
        public class HeartBeatWicked
        {
            [HarmonyPostfix]
            public static void Postfix(string sceneName)
            {
                Log.LogMessage(sceneName);
                if (sceneName == "Level 0-S"){
                    tactsuitVr.StartHeartBeat();
                }
                else
                {
                    tactsuitVr.StopHeartBeat();
                }
            }
        }
    }
}