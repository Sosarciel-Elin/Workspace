using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;


[BepInPlugin("sosarciel.securemultisize", "SecureMultisize", "1.0.0.0")]
[BepInProcess("Elin.exe")]
public class EquipModify : BaseUnityPlugin {
    public static new ManualLogSource Logger;

    void Awake() {
        Logger = base.Logger;
        Initialize();
        var harmony = new Harmony("SecureMultisize");
        harmony.PatchAll();
        Logger.LogInfo("Awake");
    }
    void Initialize(){
        SMUtils.AllowDestroyCities = base.Config.Bind<bool>("General", "AllowDestroyCities", false,
"允许大型生物破坏城市\nAllows large creatures to destroy cities");
        SMUtils.RestrictAllLargeCreatures = base.Config.Bind<bool>("General", "RestrictAllLargeCreatures", false,
"约束所有大型生物。如果未启用，则只约束友方大型生物。\nRestricts all large creatures. If disabled, only friendly large creatures are restricted.");
    }
}

public static class SMUtils{
    public static ConfigEntry<bool> AllowDestroyCities;
    public static ConfigEntry<bool> RestrictAllLargeCreatures;

}


[HarmonyPatch(typeof(Chara))]
[HarmonyPatch(nameof(Chara.CanDestroyPath))]
public static class Chara_CanDestroyPath_Patch{
    private static bool Prefix(Chara __instance, ref bool __result){
        if (!__instance.IsMultisize) return true;
        if (!__instance.IsPCFactionOrMinion && !SMUtils.RestrictAllLargeCreatures.Value) return true;

        //始终无法破坏家园与帐篷
        if(EClass._zone.IsPCFaction || EClass._zone is Zone_Tent){
            __result = false;
            return false;
        }

        if(EClass._zone is Zone_Civilized && !SMUtils.AllowDestroyCities.Value){
            __result = false;
            return false;
        }

        __result = true;
        return false;
    }
}
