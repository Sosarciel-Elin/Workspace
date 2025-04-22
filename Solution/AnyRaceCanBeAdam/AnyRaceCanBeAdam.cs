using BepInEx;
using HarmonyLib;
using System.Collections.Generic;



[BepInPlugin("sosarciel.anyracecanbeadam", "AnyRaceCanBeAdam", "1.0.0.0")]
public class AnyRaceCanBeAdam : BaseUnityPlugin {

	private void Start() {
		var harmony = new Harmony("AnyRaceCanBeAdam");
		harmony.PatchAll();
	}
}



[HarmonyPatch(typeof(FoodEffect))]
[HarmonyPatch(nameof(FoodEffect.Proc))]
[HarmonyPatch(new [] { typeof(Chara),typeof(Thing) })]
class FoodEffect_Proc_Patch {
	public static void Postfix(Chara c, Thing food) {
        bool flag3 = food.HasElement(709);
        bool flag5 = food.IsDecayed || flag3;
		if (flag5 && !c.HasElement(480))
			return;

        foreach (Element value in food.elements.dict.Values){
			List<Element> list = food.ListValidTraits(isCraft: true, limit: false);
			if (value.source.foodEffect.IsEmpty() || !list.Contains(value))
                continue;

                string[] foodEffect = value.source.foodEffect;
				switch (foodEffect[0]){
					case "little":{
						if (c.race.id != "mutant" && c.elements.Has(1203) && c.elements.Base(1230) < 10){
                            c.Say("little_adam", c);
                            c.SetFeat(1230, c.elements.Base(1230) + 1);
                        }
						break;
					}
				}
		}
	}
}
