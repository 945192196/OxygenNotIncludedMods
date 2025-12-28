using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using KMod;
using SmarterConditioner.STRINGS;

namespace SmarterConditioner
{
    // Token: 0x02000006 RID: 6
    public class SmarterConditionerMod : UserMod2
    {
        // Token: 0x06000006 RID: 6 RVA: 0x0000209D File Offset: 0x0000029D
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            LocString.CreateLocStringKeys(typeof(UI), "STRINGS.");
        }
    }
}
