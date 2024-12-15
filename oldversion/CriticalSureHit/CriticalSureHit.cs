using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Diagnostics;


[BepInPlugin("sosarciel.criticalsurehit", "CriticalSureHit", "1.0.0.0")]
public class CriticalSureHit : BaseUnityPlugin {
	private void Start() {
		var harmony = new Harmony("CriticalSureHit");
		harmony.PatchAll();
	}

}


[HarmonyPatch(typeof(AttackProcess))]
[HarmonyPatch(nameof(AttackProcess.CalcHit))]
public static class AttackProcess_CalcHit_Patch {
    public static bool Prefix(AttackProcess __instance, ref bool __result){
        #region const
        var TC = __instance.TC;
        var CC = __instance.CC;
        var toHit = __instance.toHit;
        var evasion = __instance.evasion;
        var IsRanged = __instance.IsRanged;
        Func<bool> Crit = delegate(){
            __instance.crit = true;
            return true;
        };
        Func<bool> EvadePlus = delegate(){
            __instance.evadePlus = true;
            return false;
        };
        #endregion

        //Msg.Say("Pre PerfectEvasion");

        #region PerfectEvasion And StatusHit
        if (TC != null){
            if (TC.HasCondition<ConDim>() && EClass.rnd(4) == 0){
                __result = Crit();
                return false;
            }

            if (TC.IsDeadOrSleeping){
                __result = Crit();
                return false;
            }

            if (TC.Evalue(57) > EClass.rnd(100)){
                __result = EvadePlus();
                return false;
            }
        }
        #endregion

        //Msg.Say("Pre D20Hit");

        #region D20Hit
        if (EClass.rnd(20) == 0){
            //__result = true;
            //mod to d20=20 crit
            __result = Crit();
            return false;
        }

        if (EClass.rnd(20) == 0){
            __result = false;
            return false;
        }
        #endregion

        //Msg.Say("Pre 0ValueHit");

        #region 0ValueHit
        if (toHit < 1){
            __result = false;
            return false;
        }

        if (evasion < 1){
            __result = true;
            return false;
        }
        #endregion

        //Msg.Say("Pre Crit");

        #region Crit
        if (EClass.rnd(5000) < CC.Evalue(73) + 50){
            __result = Crit();
            return false;
        }

        if ((float)CC.Evalue(90) + Mathf.Sqrt(CC.Evalue(134)) > (float)EClass.rnd(200)){
            __result = Crit();
            return false;
        }

        if (CC.Evalue(1420) > 0){
            int num3 = Mathf.Min(100, 100 - CC.hp * 100 / CC.MaxHP);
            if (num3 >= 50 && num3 * num3 * num3 * num3 / 3 > EClass.rnd(100000000)){
                __result = Crit();
                return false;
            }
        }
        #endregion

        //Msg.Say("Aft Crit");

        #region GreaterEvasion
        if(TC!=null){
            int num = TC.Evalue(151);
            if (num != 0 && toHit < num * 10){
                int num2 = evasion * 100 / Mathf.Clamp(toHit, 1, toHit);
                if (num2 > 300 && EClass.rnd(num + 250) > 100){
                    __result = EvadePlus();
                    return false;
                }

                if (num2 > 200 && EClass.rnd(num + 250) > 150){
                    __result = EvadePlus();
                    return false;
                }

                if (num2 > 150 && EClass.rnd(num + 250) > 200){
                    __result = EvadePlus();
                    return false;
                }
            }
        }
        #endregion

        #region HitProcess
        if (EClass.rnd(toHit) < EClass.rnd(evasion * (IsRanged ? 150 : 125) / 100)){
            __result = false;
            return false;
        }
        #endregion

        __result = true;
        return false;
    }
}