using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[BepInPlugin("sosarciel.betterbreeding", "BetterBreeding", "1.0.0.0")]
public class BetterBreeding : BaseUnityPlugin {
	private void Start() {
		this.Initialize();
		var harmony = new Harmony("BetterBreeding");
		harmony.PatchAll();
	}
	public void Initialize(){
		BBUtils.BreedingLimit = base.Config.Bind<int>("General", "BreedingLimit", 60,
			"指定育种上限(原版为60, -1则表示无限)\nSpecify breeding limit (originally 60, -1 for no limit)");
		BBUtils.ModMultipleAttributes = base.Config.Bind<int>("General", "ModMultipleAttributes", 1, @"
育种可以同时提升多个属性, 而非单个(原版有9个可培育属性, 若开启AllowAllAttributes则会有17个可培育属性)
Breeding can increase multiple attributes simultaneously instead of just one (in the original game, there are 9 attributes available for breeding, and 17 if AllowAllAttributes is enabled)
".Trim());
		BBUtils.AllowAllAttributes = base.Config.Bind<bool>("General", "AllowAllAttributes", false,
			"允许培育全部属性(原版只有潜力以及魅力)\nAllows breeding to increase all attributes, originally only potential and charm");
		BBUtils.AllowMultipleAttributeGains = base.Config.Bind<bool>("General", "AllowMultipleAttributeGains", false,
			"允许在培育过程中多次获取新属性\nAllows acquiring multiple new attributes during breeding");
		BBUtils.BreedingStepSize = base.Config.Bind<int>("General", "BreedingStepSize", -1,@"
育种步长必定为指定值, 修改此值可以影响附魔等级的属性加值, 但依然受限于育种上限(原版为1-6随机, -1为不生效)
Breeding step size is always the specified value, changing this value can affect enchantment level attribute bonuses, but is still limited by breeding limit (originally random between 1-6, -1 to disable)
".Trim());
		BBUtils.AllowMultipleAttributeGains = base.Config.Bind<bool>("General", "AllowMultipleAttributeGains", false,
			"允许在培育过程中多次获取新属性\nAllows acquiring multiple new attributes during breeding");
		BBUtils.LimitUseTotalAttributeSum = base.Config.Bind<bool>("General", "LimitUseTotalAttributeSum", false, @"
育种上限计算使用全属性总和, 而非分别计算每一条(如果此项不为真, 种子最终都将变为全属性全满)
Use the total sum of all attributes for breeding limit calculation, instead of calculating each one separately (if this option is not enabled, the seeds will eventually have all attributes maximized)
".Trim());
		BBUtils.CropGrowthRateMultiplier = base.Config.Bind<int>("General", "CropGrowthRateMultiplier", 1,
			"作物生长速度倍率\nCrop growth rate multiplier");




		BBUtils.SoilFertilityMultiplier = base.Config.Bind<float>("General", "SoilFertilityMultiplier", 1f,
			"土壤肥沃度倍率\nSoil fertility multiplier");


		BBUtils.AllowExtraEggLaying = base.Config.Bind<bool>("AnimalHusbandry", "AllowExtraEggLaying", false,
			"允许动物的额外产卵\nAllows extra egg laying in animal husbandry");
		BBUtils.AnimalBreedingRateMultiplier = base.Config.Bind<float>("AnimalHusbandry", "AnimalBreedingRateMultiplier", 1.0f,
			"额外产卵时动物的繁育值倍率\nBreeding rate multiplier for animals during extra egg laying");
		BBUtils.AdditionalBreedingValue = base.Config.Bind<int>("AnimalHusbandry", "AdditionalBreedingValue", 0,@"
额外产卵时动物的繁育值额外调整值(在计算繁育值倍率前生效, 繁育值总量累计为2500时将必定产卵)
Additional breeding value adjustment for animals during extra egg laying (applies before calculating breeding rate multiplier; guaranteed to lay an egg when the total breeding value reaches 2500)
".Trim());
		BBUtils.FertilizedEggChance = base.Config.Bind<int>("AnimalHusbandry", "FertilizedEggChance", -1,
			"产卵时产生受精卵的概率(1/x, 为1时必定是受精卵, 原版为20, <=0时不启用)\nChance of producing fertilized eggs during egg laying in animal husbandry (1/x, guaranteed fertilized egg if 1, originally 20, <=0 to disable)");

		BBUtils.AnimalAutoMeatProduction = base.Config.Bind<bool>("AnimalHusbandry", "AnimalAutoMeatProduction", false,
			"动物会自动产肉\nAnimals will automatically produce meat");
		BBUtils.MeatProductionBreedingRateMultiplier = base.Config.Bind<float>("AnimalHusbandry", "MeatProductionBreedingRateMultiplier", 1.0f,
			"产肉时动物的繁育值倍率\nBreeding rate multiplier for animals during meat production");
		BBUtils.MeatProductionAdditionalBreedingValue = base.Config.Bind<int>("AnimalHusbandry", "MeatProductionAdditionalBreedingValue", 0, @"
产肉时动物的繁育值额外调整值(在计算繁育值倍率前生效)
Additional breeding value adjustment for animals during meat production (applies before calculating breeding rate multiplier)
".Trim());
		BBUtils.MarbledMeatChance = base.Config.Bind<int>("AnimalHusbandry", "MarbledMeatChance", 10, @"
自动产肉时产生雪花肉的概率(1/x, 为1时必定是雪花肉, 原版为10)
Chance of producing marbled meat during automatic meat production (1/x, guaranteed to be marbled meat if 1, originally 10)
".Trim());
	}
}


public static class BBUtils{
	public static ConfigEntry<int> BreedingLimit;
	public static ConfigEntry<bool> LimitUseTotalAttributeSum;
	public static ConfigEntry<int> ModMultipleAttributes;
	public static ConfigEntry<bool> AllowAllAttributes;
	public static ConfigEntry<bool> AllowMultipleAttributeGains;
	public static ConfigEntry<float> SoilFertilityMultiplier;
	public static ConfigEntry<int> BreedingStepSize;
	public static ConfigEntry<bool> AllowExtraEggLaying;
	public static ConfigEntry<float> AnimalBreedingRateMultiplier;
	public static ConfigEntry<int> AdditionalBreedingValue;
	public static ConfigEntry<int> CropGrowthRateMultiplier;
	public static ConfigEntry<bool> AnimalAutoMeatProduction;
	public static ConfigEntry<float> MeatProductionBreedingRateMultiplier;
	public static ConfigEntry<int> MeatProductionAdditionalBreedingValue;
	public static ConfigEntry<int> MarbledMeatChance;



	public static ConfigEntry<int> FertilizedEggChance;
	public static System.Random rd = new System.Random();
}

[HarmonyPatch(typeof(CraftUtil))]
[HarmonyPatch(nameof(CraftUtil.ModRandomFoodEnc))]
[HarmonyPatch(new [] { typeof(Thing) })]
class CraftUtil_ModRandomFoodEnc_Patch {
	public static bool Prefix(Thing t) {
		//Msg.Say("CraftUtil_ModRandomFoodEnc_Patch");
		Rand.SetSeed(BBUtils.rd.Next());
		int limit = BBUtils.BreedingLimit.Value;
		int modMultipleAttributes = BBUtils.ModMultipleAttributes.Value;
		int stepSize = BBUtils.BreedingStepSize.Value;
		bool allowMultipleAttributeGains = BBUtils.AllowMultipleAttributeGains.Value;
		if(limit==60 && modMultipleAttributes<=1 && stepSize==-1 && !allowMultipleAttributeGains) return true;
		int actCount = 0;

		bool limitUseTotalAttributeSum = BBUtils.LimitUseTotalAttributeSum.Value;
		if(limitUseTotalAttributeSum && limit>-1){
			int total = 0;
			foreach (var value in t.elements.dict.Values){
				if (value.IsFoodTrait) total += value.Value;
			}
			if (total > limit) return false;
		}

		if(allowMultipleAttributeGains){
			bool allowAllAttributes = BBUtils.AllowAllAttributes.Value;
			List<SourceElement.Row> slist = EClass.sources.elements.rows
				.Where((SourceElement.Row e) =>
					(allowAllAttributes
						? e.foodEffect.Length >= 1
						: e.foodEffect.Length > 1) &&
					CraftUtil.ListFoodEffect.Contains(e.foodEffect[0]))
				.ToList();

			var elements = Enumerable.Range(0, Math.Clamp(modMultipleAttributes, 1, slist.Count))
				.Select(_ => {
					var selectedElement = slist.RandomItemWeighted((SourceElement.Row a) => a.chance);
					slist.Remove(selectedElement);
					return selectedElement;
				})
				.ToList();

			foreach (var row in elements) {
				if(!t.elements.HasBase(row.id)){
					//Msg.Say("addeid:"+row.id);
					t.elements.SetBase(row.id, 1);
					actCount++;
					if(actCount>=modMultipleAttributes){
						t.c_seed = row.id;
						return false;
					}
				}
			}
		}

		List<Element> list = new List<Element>();
		foreach (Element value in t.elements.dict.Values){
			if (value.IsFoodTrait)
				list.Add(value);
		}

		if (list.Count != 0) {
			var elements = Enumerable.Range(0, Math.Clamp(modMultipleAttributes, 1, list.Count))
				.Select(_ => {
					var selectedElement = list.RandomItem();
					list.Remove(selectedElement);
					return selectedElement;
				})
				.ToList();
			var modv = stepSize!=-1 ? stepSize : EClass.rnd(6) + 1;
			foreach (var element in elements) {
				//Msg.Say("modeid:"+element.id);
				//Msg.Say("modv:"+modv);
				t.elements.ModBase(element.id, modv);
				if (limit>-1 && element.Value > limit)
					t.elements.SetTo(element.id, limit);
				actCount++;
				if(actCount>=modMultipleAttributes)
					return false;
			}
		}

		return false;
	}
}

//[HarmonyPatch(nameof(TraitSeed.LevelSeed))]
//[HarmonyPatch(nameof(TraitSeed.MakeSeed))]
[HarmonyPatch(typeof(CraftUtil))]
[HarmonyPatch(nameof(CraftUtil.AddRandomFoodEnc))]
[HarmonyPatch(new [] { typeof(Thing) })]
class CraftUtil_AddRandomFoodEnc_Patch {
	public static bool Prefix(Thing t) {
		int modMultipleAttributes = BBUtils.ModMultipleAttributes.Value;
		bool allowAllAttributes = BBUtils.AllowAllAttributes.Value;
		if(modMultipleAttributes<=1 && !allowAllAttributes) return true;

		List<SourceElement.Row> list = EClass.sources.elements.rows
			.Where((SourceElement.Row e) =>
				(allowAllAttributes
					? e.foodEffect.Length >= 1
					: e.foodEffect.Length > 1) &&
				CraftUtil.ListFoodEffect.Contains(e.foodEffect[0]))
			.ToList();

		list.ForeachReverse(delegate (SourceElement.Row e){
				if (t.elements.dict.ContainsKey(e.id))
					list.Remove(e);
			});
		if (list.Count != 0) {
			var elements = Enumerable.Range(0, Math.Clamp(modMultipleAttributes, 1, list.Count))
				.Select(_ => {
					var selectedElement = list.RandomItem();
					list.Remove(selectedElement);
					return selectedElement;
				})
				.ToList();
			foreach (var row in elements) {
				t.elements.SetBase(row.id, 1);
				t.c_seed = row.id;
			}
		}
	return false;
	}
}



[HarmonyPatch(typeof(FactionBranch))]
[HarmonyPatch(nameof(FactionBranch.MaxSoil))]
[HarmonyPatch(MethodType.Getter)]
public static class FactionBranch_MaxSoil_Getter_Patch {
	public static void Postfix(FactionBranch __instance, ref int __result) {
		if (BBUtils.SoilFertilityMultiplier.Value != 1f) {
			var mul = BBUtils.SoilFertilityMultiplier.Value;
			__result = (int)(__result * mul);
		}
	}
}


[HarmonyPatch(typeof(FactionBranch))]
[HarmonyPatch(nameof(FactionBranch.DailyOutcome))]
[HarmonyPatch(new [] { typeof(VirtualDate) })]
public static class FactionBranch_DailyOutcome_Patch {
	public static void Postfix(FactionBranch __instance, VirtualDate date) {
		bool allowExtraEggLaying = BBUtils.AllowExtraEggLaying.Value;
		bool autoMeat = BBUtils.AnimalAutoMeatProduction.Value;
		if (!allowExtraEggLaying && !autoMeat) return;

		float extmul = BBUtils.AnimalBreedingRateMultiplier.Value;
		int extadd = BBUtils.AdditionalBreedingValue.Value;

		float meatMul = BBUtils.MeatProductionBreedingRateMultiplier.Value;
		int meatAdd = BBUtils.MeatProductionAdditionalBreedingValue.Value;
		int marbleChange = BBUtils.MarbledMeatChance.Value;

		Thing thing = null;
        Chara i;
        foreach (Chara member in __instance.members){
            i = member;
            if (i.IsPCParty || !i.ExistsOnMap) continue;
            //i.RefreshWorkElements(__instance.elements);
            if (i.memberType == FactionMemberType.Livestock){
                if (thing == null)
                    thing = EClass._map.Stocked.Find("pasture", -1, -1, shared: true) ?? EClass._map.Installed.Find("pasture");

                if (thing == null) continue;

				//下蛋
				if(allowExtraEggLaying){
                if ((i.race.breeder+extadd)*extmul >= EClass.rnd(2500 - (int)Mathf.Sqrt(__instance.Evalue(2827) * 100))){
                    if (EClass.rnd(3) != 0){
                        Thing t2 = i.MakeEgg(date.IsRealTime, 1, date.IsRealTime);
                        if (!date.IsRealTime) i.TryPutShared(t2);
                    }
                    else{
                        Thing t3 = i.MakeMilk(date.IsRealTime, 1, date.IsRealTime);
                        if (!date.IsRealTime) i.TryPutShared(t3);
                    }
                }}

				//掉肉
				if (autoMeat){
				if ((i.race.breeder+meatAdd)*meatMul >= EClass.rnd(2500 - (int)Mathf.Sqrt(__instance.Evalue(2827) * 100))){
					string text = i.race.corpse[0];
					if (marbleChange>0 && text == "_meat" && EClass.rnd(marbleChange) == 0)
						text = "meat_marble";

					Thing thing3 = ThingGen.Create(text);
					if (thing3.source._origin == "meat")
						thing3.MakeFoodFrom(i);
					else thing3.ChangeMaterial(i.race.material);

					if (!date.IsRealTime) i.TryPutShared(thing3);
				}}
			}
		}
	}
}



[HarmonyPatch(typeof(Chara))]
[HarmonyPatch(nameof(Chara.MakeEgg))]
[HarmonyPatch(new[] { typeof(bool), typeof(int), typeof(bool) })]
public static class Chara_MakeEgg_Patch{
    public static bool Prefix(Chara __instance, bool effect, int num, bool addToZone, ref Thing __result){
		int fertilizedEggChance = BBUtils.FertilizedEggChance.Value;
		if(fertilizedEggChance<=0) return true;

        Thing thing = ThingGen.Create((EClass.rnd(EClass.debug.enable ? 1 : fertilizedEggChance) == 0)
			? "egg_fertilized" : "_egg").SetNum(num);
        thing.MakeFoodFrom(__instance);
        thing.c_idMainElement = __instance.c_idMainElement;
        if (!addToZone){
			__result = thing;
			return false;
        }

        __result = __instance.GiveBirth(thing, effect);
		return false;
    }
}


[HarmonyPatch(typeof(GrowSystem))]
[HarmonyPatch(nameof(GrowSystem.Grow))]
[HarmonyPatch(new[] { typeof(int) })]
class GrowSystem_Grow_Patch {
	public static bool Prefix(GrowSystem __instance, ref int mtp){
		var mul = BBUtils.CropGrowthRateMultiplier.Value;
		if(mul==1) return true;
		mtp *= mul;
		return true;
	}
}


//[HarmonyPatch(nameof(TraitDrinkMilkMother.OnDrink))]