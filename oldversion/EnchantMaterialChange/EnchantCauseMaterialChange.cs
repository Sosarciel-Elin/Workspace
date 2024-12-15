using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;



[BepInPlugin("sosarciel.enchantmentcausematerialchange", "EnchantCauseMaterialChange", "1.0.0.0")]
public class EnchantCauseMaterialChange : BaseUnityPlugin {

	private void Start() {
		var harmony = new Harmony("EnchantCauseMaterialChange");
		harmony.PatchAll();
	}
}



struct MatData{
	public string name;
	public int id;
	public int tier;
}
static class MatTable{
	static Regex matcher = new Regex(@"\t\t(.+?)\t(.+?)\t.+\t[^\d]+(\d+)\n");
	static string replacer = "new MatData {id=$1, name=\"$2\", tier=$3},\n";
	static List<MatData> matDataList = new List<MatData>{
		new MatData {id=1, name="橡木", tier=0},
		new MatData {id=2, name="铁", tier=1},
		new MatData {id=3, name="花岗岩", tier=0},
		new MatData {id=5, name="草", tier=0},
		new MatData {id=8, name="沙", tier=0},
		new MatData {id=9, name="胶状物", tier=0},
		new MatData {id=10, name="生鲜", tier=0},
		new MatData {id=12, name="金", tier=3},
		new MatData {id=13, name="银", tier=1},
		new MatData {id=14, name="铜", tier=0},
		new MatData {id=15, name="青铜", tier=1},
		new MatData {id=16, name="云母", tier=0},
		new MatData {id=17, name="铬铁", tier=3},
		new MatData {id=18, name="钻石", tier=4},
		new MatData {id=19, name="红宝石", tier=4},
		new MatData {id=20, name="钢", tier=2},
		new MatData {id=21, name="珊瑚", tier=1},
		new MatData {id=22, name="硅砂", tier=1},
		new MatData {id=23, name="水晶", tier=2},
		new MatData {id=24, name="革", tier=2},
		new MatData {id=25, name="龙鳞", tier=4},
		new MatData {id=26, name="珍珠", tier=2},
		new MatData {id=27, name="绿宝石", tier=3},
		new MatData {id=29, name="精金", tier=4},
		new MatData {id=39, name="白金", tier=2},
		new MatData {id=40, name="骨", tier=0},
		new MatData {id=42, name="纸", tier=0},
		new MatData {id=43, name="以太", tier=4},
		new MatData {id=55, name="黑曜石", tier=1},
		new MatData {id=62, name="稻草", tier=0},
		new MatData {id=68, name="秘银", tier=3},
		new MatData {id=69, name="钛", tier=4},
		new MatData {id=70, name="棉花", tier=0},
		new MatData {id=71, name="丝绸", tier=1},
		new MatData {id=72, name="鳞", tier=1},
		new MatData {id=73, name="山羊绒", tier=2},
		new MatData {id=74, name="柴隆纤维", tier=3},
		new MatData {id=75, name="灵布", tier=3},
		new MatData {id=76, name="暮染", tier=4},
		new MatData {id=77, name="狮鹫鳞", tier=4},
		new MatData {id=78, name="塑料", tier=1},
		new MatData {id=80, name="羊毛", tier=2},
		new MatData {id=81, name="蜘蛛丝", tier=2},
		new MatData {id=91, name="亚麻", tier=0},
		new MatData {id=99, name="紫水晶", tier=3},
	};
	static List<MatData> mat01DataList = matDataList.Where(mat=>mat.tier<=1).ToList();
	static List<MatData> mat12DataList = matDataList.Where(mat=>mat.tier<=2 && mat.tier>=1).ToList();
	static List<MatData> mat23DataList = matDataList.Where(mat=>mat.tier<=3 && mat.tier>=2).ToList();
	static List<MatData> mat34DataList = matDataList.Where(mat=>mat.tier<=4 && mat.tier>=3).ToList();
	public static void ChangeMaterial(Card tc, bool blessed, bool plus){
        List<MatData> selectedList;

        if (!plus && !blessed)
            selectedList = mat01DataList;
        else if (!plus && blessed)
            selectedList = mat12DataList;
        else if (plus && !blessed)
            selectedList = mat23DataList;
        else selectedList = mat34DataList;

        var currentId = tc.material.id;
        var nextMat = selectedList
			.OrderBy(mat => mat.id)
			.FirstOrDefault(mat => mat.id > currentId);

        // 如果找不到更大的id，循环从头开始
        if (nextMat.name == default(MatData).name)
            nextMat = selectedList.OrderBy(mat => mat.id).First();

        tc.ChangeMaterial(nextMat.id);
    }
}

[HarmonyPatch(typeof(ActEffect))]
[HarmonyPatch(nameof(ActEffect.Proc))]
[HarmonyPatch(new[] { typeof(EffectId), typeof(int), typeof(BlessedState), typeof(Card), typeof(Card), typeof(ActRef) })]
class ProcPatch {
    static bool Prefix(EffectId id, int power, BlessedState state, Card cc, Card tc, ActRef actRef) {
		try{
		if(	state != BlessedState.Cursed &&
			(id==EffectId.EnchantArmorGreat || id==EffectId.EnchantArmor ||
			id==EffectId.EnchantWeaponGreat || id==EffectId.EnchantWeapon)){
			if(tc!=null && !tc.IsUnique){
				bool blessed = state == BlessedState.Blessed;
				bool plus = id == EffectId.EnchantWeaponGreat || id == EffectId.EnchantArmorGreat;
				int max = (plus ? 4 : 2) + (blessed ? 1 : 0);
				if(tc.encLV>=max){
					var text = (Lang.langCode == Lang.LangCode.CN.ToString()) ? "#1的材质变换了。" : "The material for #1 has been changed.";
					cc.Say(text,tc,null,null);
					cc.PlaySound("identify", 1f, true);
					cc.PlayEffect("identify", true, 0f, default);
					MatTable.ChangeMaterial(tc, blessed, plus);
					return false;
				}
			}
		}
		}catch(System.Exception e){
			Msg.Say("EnchantCauseMaterialChange.ProcPatch error: "+e.ToString());
		}
		return true;
    }
}


