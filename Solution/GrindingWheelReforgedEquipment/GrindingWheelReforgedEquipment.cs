using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[BepInPlugin("sosarciel.grindingwheelreforgedequipment", "GrindingWheelReforgedEquipment", "1.0.0.0")]
public class GrindingWheelReforgedEquipment : BaseUnityPlugin {
    private void Start() {
        Initialize();
        var harmony = new Harmony("GrindingWheelReforgedEquipment");
        harmony.PatchAll();
    }
    public void OnStartCore() {
        var dir = Path.GetDirectoryName(Info.Location);
        var excel = dir + "/Recipe.xlsx";
        var sources = Core.Instance.sources;
        ModUtil.ImportExcel(excel, "recipes", sources.recipes);
    }
    void Initialize(){
        GWREUtil.CalibrationMaxValue = base.Config.Bind<bool>("General", "CalibrationMaxValue", false,
            "一次校准就能达到最大值\nOne-time calibration reaches maximum value");
    }
}




static class GWREUtil{
    public static ConfigEntry<bool> CalibrationMaxValue;
    static public void TryChangeBase(Thing thing){

        int rd(int val, bool toLow=false){
            return GWREUtil.CalibrationMaxValue.Value
                ? toLow ? 0 : val-1
                : EClass.rnd(val);
        }

        //thing.ApplyMaterial
        var sc = thing.sourceCard;
        var source = thing.source;
        if(sc.quality>=4 || thing.isReplica){
            var text = (Lang.langCode == Lang.LangCode.CN.ToString())
                ? "无法校准。"
                : "Calibration not possible.";
            Msg.SetColor(Msg.colors.MutateBad);
            Msg.Say(text);
            return;
        }

        bool success = false;
        int num = 120;
        bool flag3 = !thing.IsAmmo;
        if (thing.rarity <= Rarity.Crude)
            num = 150;
        else if (thing.rarity == Rarity.Superior)
            num = 100;
        else if (thing.rarity >= Rarity.Legendary)
            num = 80;

        var sockets = thing.sockets;
        var socketdmg = 0;
        var socketpv = 0;
        var sockethit = 0;
        if (sockets != null){
            for (int i = 0; i < sockets.Count; i++){
                int num2 = sockets[i];
                int num3 = num2 / 100;
                if (num3 == 67) socketdmg = num2 % 100;
                if (num3 == 66) sockethit = num2 % 100;
                if (num3 == 65) socketpv = num2 % 100;
            }
        }


        if (source.offense.Length != 0){
            var dim = source.offense[1] * thing.material.dice / (num + (flag3 ? rd(25,true) : 0));
            if(dim>thing.c_diceDim){
                thing.c_diceDim = dim;
                success = true;
            }
        }

        if (source.offense.Length > 2){
            var hit = sockethit + source.offense[2] * thing.material.atk * 9 / (num - (flag3 ? rd(30) : 0));
            success |= SetBase(66, hit);
        }

        if (source.offense.Length > 3){
            var ench = 0;
            if(thing.IsWeapon || thing.IsAmmo)
                ench = thing.encLV + ((thing.blessedState == BlessedState.Blessed) ? 1 : 0);

            var dmg = socketdmg + ench + source.offense[3] * thing.material.dmg * 5 / (num - (flag3 ? rd(30) : 0));
            success |= SetBase(67, dmg);
        }

        if (source.defense.Length != 0)
            success |= SetBase(64, source.defense[0] * thing.material.dv * 7 / (num - (flag3 ? rd(30) : 0)));

        if (source.defense.Length > 1){
            var ench = 0;
            if(!thing.IsWeapon && !thing.IsAmmo)
                ench = (thing.encLV + ((thing.blessedState == BlessedState.Blessed) ? 1 : 0)) * 2;

            var pv = socketpv + ench + source.defense[1] * thing.material.pv * 9 / (num - (flag3 ? rd(30) : 0));
            success |= SetBase(65, pv);
        }

        if(success){
            var text = (Lang.langCode == Lang.LangCode.CN.ToString())
                ? "成功校准。" : "Successfully success.";
            Msg.SetColor(Msg.colors.MutateGood);
            Msg.Say(text);
        }else{
            var failText = (Lang.langCode == Lang.LangCode.CN.ToString())
                ? "校准失败。" : "Calibration failed.";
            Msg.SetColor(Msg.colors.MutateBad);
            Msg.Say(failText);
        }

        bool SetBase(int ele, int a){
            if(a>thing.elements.Base(ele)){
                thing.elements.SetBase(ele, a);
                return true;
            }
            return false;
        }
    }
}

[HarmonyPatch(typeof(TraitCrafter))]
[HarmonyPatch(nameof(TraitCrafter.Craft))]
[HarmonyPatch(new [] { typeof(AI_UseCrafter) })]
class TraitCrafter_Craft_Patch {
    public static bool Prefix(TraitGrindstone __instance, AI_UseCrafter ai, ref Thing __result){
        SourceRecipe.Row source = __instance.GetSource(ai);
        if (source == null) return true;
        //TraitGrindstone
        //Recipe
        if (!EClass.player.knownCraft.Contains(source.id)){
            SE.Play("idea");
            Msg.Say("newKnownCraft");
            EClass.player.knownCraft.Add(source.id);
            if ((bool)LayerDragGrid.Instance)
                LayerDragGrid.Instance.info.Refresh();
        }

        TraitCrafter.MixType mixType = source.type.ToEnum<TraitCrafter.MixType>();
        switch (mixType){
            case TraitCrafter.MixType.Grind:
                if (source.tag.Contains("sosarciel_reforge")){
                    GWREUtil.TryChangeBase(ai.ings[1]);
                    ai.ings[0].ModNum(-1);
                    return false;
                }
                break;
        }
        return true;
    }
}



