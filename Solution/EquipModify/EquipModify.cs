using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;


[BepInPlugin("sosarciel.equipmodify", "EquipModify", "1.0.0.0")]
[BepInProcess("Elin.exe")]
public class EquipModify : BaseUnityPlugin {
    public static new ManualLogSource Logger;

    void Awake() {
        Logger = base.Logger;
        Initialize();
        var harmony = new Harmony("EquipModify");
        harmony.PatchAll();
        Logger.LogInfo("Awake");
    }
    public void OnStartCore() {
        Logger.LogInfo("OnStartCore");
        var dir = Path.GetDirectoryName(Info.Location);
        var sources = Core.Instance.sources;

        var itemDir = dir + "/Item/EquipModifyThingThing.xlsx";
        ModUtil.ImportExcel(itemDir, "things", sources.things);

		var recipeDir = dir + "/Recipe/EquipModifyRecipe.xlsx";
		ModUtil.ImportExcel(recipeDir, "recipes", sources.recipes);
    }
    void Initialize(){}
}

public static class EMUtils{
    public static string DispelPowderID = "sosarciel_dispel_powder";
    public static string EnchantGemID = "sosarciel_enchant_gem";
}


[HarmonyPatch(typeof(InvOwnerMod))]
[HarmonyPatch(nameof(InvOwnerMod.ShouldShowGuide))]
[HarmonyPatch(new [] { typeof(Thing)})]
public static class InvOwnerMod_ShouldShowGuide_Patch{
    private static bool Prefix(InvOwnerMod __instance, Thing t){
        var owner = __instance.owner;
        if(owner.id != EMUtils.EnchantGemID) return true;


        
        return false;
    }
}


[HarmonyPatch(typeof(InvOwnerMod))]
[HarmonyPatch(nameof(InvOwnerMod._OnProcess))]
[HarmonyPatch(new [] { typeof(Thing)})]
public static class InvOwnerMod__OnProcess_Patch{
    private static bool Prefix(InvOwnerMod __instance, Thing t){
        var owner = __instance.owner;
        if(owner.id != EMUtils.EnchantGemID) return true;

        SE.Play("reloaded");
        EClass.pc.PlayEffect("identify");
        Msg.Say("modded", t, owner);
        t.ApplySocket(owner.Thing);

        return false;
    }
}