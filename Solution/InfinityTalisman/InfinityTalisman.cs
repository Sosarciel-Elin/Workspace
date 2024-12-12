using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;

[BepInPlugin("sosarciel.infinitytalisman", "InfinityTalisman", "1.0.0.0")]
public class InfinityTalisman : BaseUnityPlugin{
    private void Start(){
        Initialize();
        var harmony = new Harmony("InfinityTalisman");
        harmony.PatchAll();
    }
    public void Initialize(){
        ITUtils.TalismanMastery = base.Config.Bind<bool>("General", "TalismanMastery", false,
            "要求灵符知识专长\nRequires Talisman Mastery");
        ITUtils.UnlimitedTalismanForNPC = base.Config.Bind<bool>("General", "UnlimitedTalismanForNPC", false,
            "NPC无限灵符\nUnlimited Talisman for NPC");
        ITUtils.UnlimitedTalismanForPC = base.Config.Bind<bool>("General", "UnlimitedTalismanForPC", false,
            "PC无限灵符\nUnlimited Talisman for PC");
    }

}

public struct TmpAmmo{
    public Thing ammoDataTemp;
    public int ammo;
}

public static class ITUtils{
    public static Dictionary<BodySlot,TmpAmmo> tmpAmmoList = null;
    public static ConfigEntry<bool> TalismanMastery;
    public static ConfigEntry<bool> UnlimitedTalismanForNPC;
    public static ConfigEntry<bool> UnlimitedTalismanForPC;
}

[HarmonyPatch(typeof(ActMelee))]
[HarmonyPatch(nameof(ActMelee.Attack))]
[HarmonyPatch(new [] { typeof(float),typeof(bool) })]
public static class ActMelee_CalcHit_Patch{
    private static bool Prefix(ActMelee __instance, float dmgMulti, bool maxRoll){
        if(Act.CC.IsPC && !ITUtils.UnlimitedTalismanForPC.Value) return true;
        if(!Act.CC.IsPC && !ITUtils.UnlimitedTalismanForNPC.Value) return true;
        if(ITUtils.TalismanMastery.Value && !Act.CC.HasElement(1418)) return true;

        foreach (BodySlot slot in Act.CC.body.slots){
            if(slot.thing == null) continue;
            var w = slot.thing;
            if (w.c_ammo > 0 && w.ammoData.id == "talisman"){
                ITUtils.tmpAmmoList ??= new();
                ITUtils.tmpAmmoList.Add(slot,
                    new TmpAmmo{ammoDataTemp = w.ammoData, ammo = w.c_ammo});
            }
        }
        return true;
    }
    private static void Postfix(ActMelee __instance, float dmgMulti, bool maxRoll){
        if(Act.CC.IsPC && !ITUtils.UnlimitedTalismanForPC.Value) return;
        if(!Act.CC.IsPC && !ITUtils.UnlimitedTalismanForNPC.Value) return;
        if(ITUtils.TalismanMastery.Value && !Act.CC.HasElement(1418)) return;
        if(ITUtils.tmpAmmoList==null) return;

        foreach (BodySlot slot in Act.CC.body.slots){
            var w = slot.thing;
            if(!ITUtils.tmpAmmoList.ContainsKey(slot)) continue;
            var tmp = ITUtils.tmpAmmoList[slot];
            w.ammoData = tmp.ammoDataTemp;
            w.c_ammo = tmp.ammo;
            w.ammoData.Num = w.c_ammo;
        }
        ITUtils.tmpAmmoList = null;
    }
}