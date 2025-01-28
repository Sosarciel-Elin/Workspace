using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("sosarciel.fullpowerrapidarow", "FullPowerRapidArow", "1.0.0.0")]
public class FullPowerRapidArow : BaseUnityPlugin{
    private void Start(){
        Initialize();
        var harmony = new Harmony("FullPowerRapidArow");
        harmony.PatchAll();
    }
    public void Initialize(){
        FPRAUtils.RapidFireNoFalloff = base.Config.Bind<bool>("General", "RapidFireNoFalloff", false,
            "速射附魔无衰减\nNo falloff for Rapid Fire enchantment");
        FPRAUtils.RapidArrowNoFalloff = base.Config.Bind<bool>("General", "RapidArrowNoFalloff", false,
            "连续射击天赋无衰减\nNo falloff for Rapid Arrow feat");
    }

}

public static class FPRAUtils{
    public static ConfigEntry<bool> RapidFireNoFalloff;
    public static ConfigEntry<bool> RapidArrowNoFalloff;
}

//[HarmonyPatch(typeof(ActRanged))]
//[HarmonyPatch(nameof(ActRanged.Perform))]
[HarmonyPatch(typeof(AttackProcess))]
[HarmonyPatch(nameof(AttackProcess.Perform))]
[HarmonyPatch(new[] { typeof(int), typeof(bool), typeof(float), typeof(bool), typeof(bool) })]
public static class AttackProcess_Perform_Patch{
    private static bool Prefix(AttackProcess __instance, int count, bool hasHit, float dmgMulti, bool maxRoll, bool subAttack){
        if(__instance.IsRanged && FPRAUtils.RapidFireNoFalloff.Value){
            Thing weapon = __instance.CC.ranged;
            if(weapon!=null){
                var add = Mathf.CeilToInt(weapon.Evalue(602)/10);
                __instance.numFireWithoutDamageLoss +=add;
            }
        }

        if(__instance.IsRanged && FPRAUtils.RapidArrowNoFalloff.Value){
            var add = __instance.CC.Evalue(1652);
            __instance.numFireWithoutDamageLoss +=add;
        }

        return true;
    }
}