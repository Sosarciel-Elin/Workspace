using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;


[BepInPlugin("sosarciel.moreloot", "MoreLoot", "1.0.0.0")]
public class MoreLoot : BaseUnityPlugin {

	private void Start() {
		Initialize();
		var harmony = new Harmony("MoreLoot");
		harmony.PatchAll();
	}

	public void Initialize(){
        MLUtils.EvolChangeMultiplier = base.Config.Bind<int>("General", "EvolChangeMultiplier", 1,
            "进化概率乘数\nMultiplier for evolution chance");
        MLUtils.BoosLootMultiplier = base.Config.Bind<int>("General", "BoosLootMultiplier", 1,
            "Boss战利品乘数\nMultiplier for loot dropped by bosses");
		MLUtils.ExtraGeneDropRate = base.Config.Bind<int>("General", "ExtraGeneDropRate", -1,
            "额外基因掉落率(1/x, 1必定掉落, 小于1不启用, 原版掉落率为1/200)\nExtra gene drop rate (1/x, 1 guarantees drop, less than 1 disables, base game drop rate is 1/200)");
		MLUtils.AllowPCFactionDropGene = base.Config.Bind<bool>("General", "AllowPCFactionDropGene", false,
            "允许队友或居民掉落额外基因\nAllow allies or residents to drop extra genes");
    }
}

public static class MLUtils{
    public static ConfigEntry<int> EvolChangeMultiplier;
    public static ConfigEntry<int> BoosLootMultiplier;
    public static ConfigEntry<int> ExtraGeneDropRate;
    public static ConfigEntry<bool> AllowPCFactionDropGene;

}

[HarmonyPatch(typeof(Chara))]
[HarmonyPatch(nameof(Chara.TryDropBossLoot))]
class Chara_TryDropBossLoot_Patch {
	static bool Prefix(Chara __instance){
		//Msg.Say("Chara_TryDropBossLoot_Patch");
		if(MLUtils.BoosLootMultiplier.Value<=1)
			return true;
		var extDrop = MLUtils.BoosLootMultiplier.Value-1;
		//Msg.Say("Chara_TryDropBossLoot_Patch "+extDrop);
		if (__instance.IsPCFaction || __instance.IsPCFactionMinion)
            return true;
		for(var i=0;i<extDrop;i++){
			//Msg.Say("Chara_TryDropBossLoot_Patch for "+i);
			Point point = __instance.pos.GetNearestPoint(allowBlock: true, allowChara: false, allowInstalled: false, ignoreCenter: true) ?? __instance.pos;
			int num = 0;
			TreasureType type = TreasureType.BossQuest;
			if (EClass._zone.Boss == __instance){
				type = TreasureType.BossNefia;
				num = 2 + EClass.rnd(2);
			}
			switch (__instance.id){
				case "vernis_boss":
					//Msg.Say("vernis_boss");
					num = 5;
					break;
				case "melilith_boss":
					//Msg.Say("melilith_boss");
					num = 5;
					break;
				case "isygarad":
					//Msg.Say("isygarad");
					num = 5;
					break;
				case "swordkeeper":
					//Msg.Say("swordkeeper");
					num = 10;
					break;
			}
			if (num != 0){
				//Msg.Say("place");
				Thing thing = ThingGen.CreateTreasure("chest_boss", __instance.LV, type);
				point.SetBlock();
				point.SetObj();
				EClass._zone.AddCard(thing, point).Install();
				ThingGen.TryLickChest(thing);
			}
		}
		return true;
	}
}

[HarmonyPatch(typeof(Card))]
[HarmonyPatch(nameof(Card.SpawnLoot))]
[HarmonyPatch(new[] { typeof(Card) })]
class Card_SpawnLoot_Patch {
	static bool Prefix(Card __instance, Card origin){
		if(MLUtils.ExtraGeneDropRate.Value<1)
			return true;
		var allowAllies = MLUtils.AllowPCFactionDropGene.Value;
		var extChange = MLUtils.ExtraGeneDropRate.Value;

        List<Card> list = new List<Card>();

		if ((!__instance.IsPCFaction || allowAllies) && chance(extChange))
            list.Add(__instance.Chara.MakeGene());

		Point nearestPoint = __instance.GetRootCard().pos;
        if (nearestPoint.IsBlocked)
            nearestPoint = nearestPoint.GetNearestPoint();

		foreach (Card item2 in list){
            item2.isHidden = false;
            item2.SetInt(116);
            EClass._zone.AddCard(item2, nearestPoint);
            if (!item2.IsEquipmentOrRanged || item2.rarity < Rarity.Superior || item2.IsCursed)
                continue;

            foreach (Chara chara in EClass._map.charas){
                if (chara.HasElement(1412) && chara.Dist(nearestPoint) < 3){
                    item2.Thing.TryLickEnchant(chara);
                    break;
                }
            }
        }

		bool chance(int i){
            i = i * 100 / (100 + EClass.player.codex.GetOrCreate(__instance.id).BonusDropLv * 10);
            if (i < 1)
                i = 1;

            if (EClass.rnd(i) == 0)
                return true;

            return false;
        }
		return true;
	}
}


[HarmonyPatch(typeof(Zone))]
[HarmonyPatch(nameof(Zone.TryGenerateEvolved))]
[HarmonyPatch(new[] { typeof(bool), typeof(Point)})]
class Zone_TryGenerateEvolved_Patch {
	static bool Prefix(Zone __instance, bool force, Point p, ref Chara __result){
		//Msg.Say("Zone_TryGenerateEvolved_Patch");
		if(MLUtils.EvolChangeMultiplier.Value<=1)
			return true;
		var extChange = MLUtils.EvolChangeMultiplier.Value;
		//Msg.Say("Zone_TryGenerateEvolved_Patch "+extChange);
        if (!force && ((__instance.EvolvedChance*extChange) <= EClass.rndf(1f))){
			__result = null;
            return false;
		}

        Chara chara = __instance.SpawnMob(p, SpawnSetting.Evolved());
        for (int i = 0; i < 2 + EClass.rnd(2); i++)
            chara.ability.AddRandom();

        chara.AddThing(chara.MakeGene(DNA.Type.Default));
        if (EClass.rnd(2) == 0)
            chara.AddThing(chara.MakeGene(DNA.Type.Superior));
		__result = chara;
        return false;
    }
}

