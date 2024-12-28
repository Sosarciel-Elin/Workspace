using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Diagnostics;


[BepInPlugin("sosarciel.betterstoragechest", "BetterStorageChest", "1.0.0.0")]
public class BetterStorageChest : BaseUnityPlugin {
	private void Start() {
		this.Initialize();
		var harmony = new Harmony("BetterStorageChest");
		harmony.PatchAll();
	}
	public void Initialize(){
        BSUtils.UpgradeMultiplier = base.Config.Bind<float>("General", "UpgradeMultiplier", 1f,
			"扳手升级效果乘数\nWrench upgrade effect multiplier");
		BSUtils.EnergyConsumptionMultiplier = base.Config.Bind<float>("General", "EnergyConsumptionMultiplier", 1f,
			"电量消耗乘数\nEnergy consumption multiplier");
		BSUtils.OpenInTent = base.Config.Bind<bool>("General", "OpenInTent", false,
			"允许在帐篷内打开\nAllows opening in the tent");
		BSUtils.UseDictionaryBasedMerge = base.Config.Bind<bool>("General", "UseDictionaryBasedMerge", false,@"
开启基于字典的合并算法, 优化物品合并性能(原版游戏在单容器包含2000物品之后, 合并算法的时间开销几乎大于1秒, 想要继续使用是几乎不可能的)
Enable dictionary-based merge algorithm, optimizing item merge performance (in the original game, the merge algorithm's time cost becomes almost greater than 1 second when a single container holds more than 2000 items, making it nearly impossible to continue using it)
".Trim());
        BSUtils.TagForSale = base.Config.Bind<bool>("General", "TagForSale", false,
            "允许为收纳箱贴上售卖标签\nAllows tagging storage chests for sale");
    }
}


public static class BSUtils{
    public static ConfigEntry<float> UpgradeMultiplier;
    public static ConfigEntry<float> EnergyConsumptionMultiplier;
    public static ConfigEntry<bool> OpenInTent;
    public static ConfigEntry<bool> UseDictionaryBasedMerge;
    public static ConfigEntry<bool> TagForSale;
}


[HarmonyPatch(typeof(TraitWrench))]
[HarmonyPatch(nameof(TraitWrench.Upgrade))]
[HarmonyPatch(new[] { typeof(Thing)})]
public static class TraitWrench_Upgrade_Patch{
    public static bool Prefix(TraitWrench __instance,Thing t,ref bool __result){
		if(BSUtils.UpgradeMultiplier.Value == 1f) return true;
		var mul = BSUtils.UpgradeMultiplier.Value;
        switch (__instance.ID){
            case "storage":
                t.c_containerUpgrade.cap += (int)(20*mul);
				if (EClass.Branch != null)
					EClass.Branch.resources.SetDirty();
				__result = true;
				return false;
        }
		return true;
    }
}

//[HarmonyPatch(typeof(TraitMagicChest))]
//[HarmonyPatch(nameof(TraitMagicChest.Electricity))]
//[HarmonyPatch(MethodType.Getter)]
//public static class TraitMagicChest_Electricity_Getter_Patch{
//    public static bool Prefix(TraitMagicChest __instance, ref int __result){
//		if(BSUtils.EnergyConsumptionMultiplier.Value == 1f) return true;
//		var mul = BSUtils.EnergyConsumptionMultiplier.Value;
//
//		//var baseElectricityProperty = typeof(TraitMagicChest).BaseType.GetProperty("Electricity",
//		//	System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//		//int baseElectricity = (int)baseElectricityProperty.GetValue(__instance);
//
//		//var baseType = typeof(TraitMagicChest).BaseType;
//		//var baseElectricityMethod = baseType.GetProperty("Electricity",
//		//	System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
//		//	.GetGetMethod(true);
//		//int baseElectricity = (int)baseElectricityMethod.Invoke(__instance, null);
//
//
//        int baseel(){
//			if (!__instance.owner.isThing)
//				return 0;
//
//			int electricity = __instance.owner.Thing.source.electricity;
//			if (electricity > 0 || EClass._zone == null || EClass._zone.branch == null)
//				return electricity;
//
//            return electricity * 100 / (100 + EClass._zone.branch.Evalue(2700) / 2);
//        }
//
//
//        var origel = baseel() +
//			((__instance.IsFridge ? 50 : 0) + __instance.owner.c_containerUpgrade.cap / 5) * -1;
//		__result = (int)(origel*mul);
//		return false;
//    }
//}

[HarmonyPatch(typeof(TraitMagicChest))]
[HarmonyPatch(nameof(TraitMagicChest.Electricity))]
[HarmonyPatch(MethodType.Getter)]
public static class TraitMagicChest_Electricity_Getter_Patch {
    public static void Postfix(TraitMagicChest __instance, ref int __result) {
        if (BSUtils.EnergyConsumptionMultiplier.Value != 1f) {
            var mul = BSUtils.EnergyConsumptionMultiplier.Value;
            __result = (int)(__result * mul);
        }
    }
}


[HarmonyPatch(typeof(TraitMagicChest))]
[HarmonyPatch(nameof(TraitMagicChest.CanOpenContainer))]
[HarmonyPatch(MethodType.Getter)]
public static class TraitMagicChest_CanOpenContainer_Getter_Patch{
    public static bool Prefix(TraitMagicChest __instance, ref bool __result){
		if(!BSUtils.OpenInTent.Value) return true;
		__result = __instance.owner.IsInstalled &&
			(EClass._zone is Zone_Tent || EClass._zone.IsPCFaction);
		return false;
    }
}

[HarmonyPatch(typeof(TraitMagicChest))]
[HarmonyPatch(nameof(TraitMagicChest.CanSearchContents))]
[HarmonyPatch(MethodType.Getter)]
public static class TraitMagicChest_CanSearchContents_Getter_Patch{
    public static bool Prefix(TraitMagicChest __instance, ref bool __result){
		if(!BSUtils.OpenInTent.Value) return true;

		__result = (EClass.core.IsGameStarted && __instance.owner.IsInstalled) &&
			(EClass._zone is Zone_Tent || EClass._zone.IsPCFaction);
		return false;
    }
}

[HarmonyPatch(typeof(UIInventory))]
[HarmonyPatch(nameof(UIInventory.Sort))]
[HarmonyPatch(new[] { typeof(bool)})]
public static class UIInventory_Sort_Patch{
	public static bool Prefix(UIInventory __instance, bool redraw){
		if(!BSUtils.UseDictionaryBasedMerge.Value) return true;
        UIList.SortMode i = __instance.IsShop
            ? EMono.player.pref.sortInvShop
            : (__instance.IsAdvSort
                ? __instance.window.saveData.sortMode
                : EMono.player.pref.sortInv);
        //UIList.SortMode i = (__instance.IsShop ? EMono.player.pref.sortInvShop : EMono.player.pref.sortInv);

		#region 合并同类物品
		if(__instance.owner.Container.things.Count<=500){
			bool flag = true;
            while (flag){
                flag = false;
                foreach (Thing thing in __instance.owner.Container.things){
                    if (thing.invY == 1)
                        continue;
                    foreach (Thing thing2 in __instance.owner.Container.things){
                        if (thing != thing2 && thing2.invY != 1 && thing.TryStackTo(thing2)){
                            flag = true;
                            break;
                        }
                    }
                    if (flag) break;
                }
            }
		}
		else{
			Func<Thing, string> getKey = (Thing t)=>
				(t.c_altName ?? "") + t.id;
            Dictionary<string, List<Thing>> thingMap = new();
            foreach (Thing thing in __instance.owner.Container.things){
                string key = getKey(thing);
                if (thingMap.TryGetValue(key, out List<Thing> existingThings)){
                    bool merged = false;
                    foreach (var existingThing in existingThings){
                        if (thing.TryStackTo(existingThing)){
                            merged = true;
                            break;
                        }
                    }
                    if (!merged) existingThings.Add(thing);
                }
                else thingMap[key] = new List<Thing> { thing };
            }
		}
		#endregion

        int num = 0;
        foreach (Thing thing3 in __instance.owner.Container.things){
            if (thing3.invY != 1){
                thing3.invY = 0;
                thing3.invX = -1;
            }

            thing3.SetSortVal(i, __instance.owner.currency);
            num++;
        }

		#region 排序
        //bool flag2 = (__instance.IsShop ? EMono.player.pref.sort_ascending_shop : EMono.player.pref.sort_ascending);
        bool flag2 = __instance.IsShop
            ? EMono.player.pref.sort_ascending_shop
            : (__instance.IsAdvSort
                ? __instance.window.saveData.sort_ascending
                : EMono.player.pref.sort_ascending);
		if(i == UIList.SortMode.ByName){
			__instance.owner.Container.things.Sort(delegate (Thing a, Thing b){
				if (flag2) return string.Compare(a.GetName(NameStyle.FullNoArticle, 1), b.GetName(NameStyle.FullNoArticle, 1));
				return string.Compare(b.GetName(NameStyle.FullNoArticle, 1), a.GetName(NameStyle.FullNoArticle, 1));
			});
		}
        else{
			__instance.owner.Container.things.Sort(delegate (Thing a, Thing b){
				if (a.sortVal == b.sortVal) return b.SecondaryCompare(i, a);
				return (!flag2) ? (a.sortVal - b.sortVal) : (b.sortVal - a.sortVal);
			});
		}
		#endregion


        if (!__instance.UseGrid){
            int num2 = 0;
            int num3 = 0;
            Vector2 sizeDelta = __instance.list.Rect().sizeDelta;
            sizeDelta.x -= 60f;
            sizeDelta.y -= 60f;
            foreach (Thing thing4 in __instance.owner.Container.things){
                if (thing4.invY != 0)
                    continue;

                thing4.posInvX = num2 + 30;
                thing4.posInvY = (int)sizeDelta.y - num3 + 30;
                num2 += 40;
                if ((float)num2 > sizeDelta.x){
                    num2 = 0;
                    num3 += 40;
                    if ((float)num3 > sizeDelta.y)
                        num3 = 20;
                }
            }
        }

        if (redraw) __instance.list.Redraw();

		return false;
    }
}



[HarmonyPatch(typeof(TraitSalesTag))]
[HarmonyPatch(nameof(TraitSalesTag.CanTagSale))]
[HarmonyPatch(new[] { typeof(Card),typeof(bool)})]
public static class TraitSalesTag_CanTagSale_Patch{
    public static bool Prefix(TraitSalesTag __instance, Card t, bool insideContainer, ref bool __result){
        if(!BSUtils.TagForSale.Value) return true;
        if (t.isSale) return true;
        if (!insideContainer && !t.IsInstalled) return true;
        if(t.id=="container_magic"){
            __result = true;
            return false;
        }
		return true;
    }
}

[HarmonyPatch(typeof(AI_Shopping))]
[HarmonyPatch(nameof(AI_Shopping.Buy))]
[HarmonyPatch(new[] { typeof(Chara),typeof(Card),typeof(bool),typeof(Card)})]
public static class AI_Shopping_Buy_Patch{
    public static bool Prefix(AI_Shopping __instance, Chara c, Card dest, bool realtime, Card container){
        if (dest.id=="container_magic") return false;
        return true;
    }
}


//[HarmonyPatch(nameof(FoodEffect.Proc))]