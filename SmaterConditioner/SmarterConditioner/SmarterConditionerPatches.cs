using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using static STRINGS.BUILDINGS.PREFABS;
using static STRINGS.UI.NEWBUILDCATEGORIES;
using STRINGS;

namespace SmarterConditioner
{
    public static class SmarterConditionerPatches
    {
        public const float ENERGY_MODIFIER = 1.4285715f;
        public const float MIN_TEMPERATURE_DELTA = -20f;

        [HarmonyPatch(typeof(AirConditionerConfig), "ConfigureBuildingTemplate")]
        private static class Patch_AirConditionerConfig_ConfigureBuildingTemplate
        {
            public static void Postfix(GameObject go)
            {
                go.AddOrGet<SmarterConditioner>();
            }
        }

        [HarmonyPatch(typeof(LiquidConditionerConfig), "ConfigureBuildingTemplate")]
        private static class Patch_LiquidConditionerConfig_ConfigureBuildingTemplate
        {
            public static void Postfix(GameObject go)
            {
                go.AddOrGet<SmarterConditioner>();
            }
        }

        [HarmonyPatch(typeof(AirConditionerConfig), "CreateBuildingDef")]
        private static class Patch_AirConditionerConfig_CreateBuildingDef
        {
            public static void Postfix(BuildingDef __result)
            {
                __result.EnergyConsumptionWhenActive = 340f;
            }
        }

        [HarmonyPatch(typeof(LiquidConditionerConfig), "CreateBuildingDef")]
        private static class Patch_LiquidConditionerConfig_CreateBuildingDef
        {
            public static void Postfix(BuildingDef __result)
            {
                __result.EnergyConsumptionWhenActive = 1700f;
            }
        }

        [HarmonyPatch(typeof(AirConditioner), "UpdateState")]
        private static class Patch_AirConditioner_UpdateState
        {
            private static MethodInfo _updateStatusMethod;
            private static MethodInfo _setlastEnvTemp;
            private static MethodInfo _setlastGasTemp;

            // 缓存 FieldInfo 以提高性能
            private static readonly FieldInfo _updateStateCbDelegateField =
                typeof(AirConditioner)
                    .GetField("UpdateStateCbDelegate",
                        BindingFlags.NonPublic |
                        BindingFlags.Static);

            // 安全获取委托实例
            private static Func<int, object, bool> GetDelegate()
            {
                if (_updateStateCbDelegateField == null)
                    throw new InvalidOperationException("未找到 UpdateStateCbDelegate 字段");

                return _updateStateCbDelegateField.GetValue(null)
                    as Func<int, object, bool>;
            }

            static Patch_AirConditioner_UpdateState()
            {
                // 预加载方法信息（优化性能）
                _updateStatusMethod = typeof(AirConditioner).GetMethod("UpdateStatus",
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);
                _setlastEnvTemp = typeof(AirConditioner).GetMethod("set_lastEnvTemp",
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);
                _setlastGasTemp = typeof(AirConditioner).GetMethod("set_lastGasTemp",
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);
            }

            public static bool Prefix(AirConditioner __instance, ref ConduitConsumer ___consumer, ref float ___targetTemperature, ref float ___envTemp, ref int ___cellCount,
                ref OccupyArea ___occupyArea, ref Storage ___storage, ref float ___lowTempLag,
                ref bool ___showingLowTemp, ref bool ___isLiquidConditioner, ref float ___temperatureDelta,
                ref float ___lastSampleTime, ref KBatchedAnimHeatPostProcessingEffect ___heatEffect, ref HandleVector<int>.Handle ___structureTemperature, 
                ref Operational ___operational, ref int ___cooledAirOutputCell, ref float dt)
            {
                bool value = ___consumer.IsSatisfied;
                ___envTemp = 0f;
                ___cellCount = 0;
                if (___occupyArea != null && __instance.gameObject != null)
                {
                    ___occupyArea.TestArea(Grid.PosToCell(__instance.gameObject), __instance, GetDelegate());
                    ___envTemp /= ___cellCount;
                }
                object[] parameters = { ___envTemp }; // 设置温度为 25°C
                _setlastEnvTemp.Invoke(__instance, parameters);
                List<GameObject> items = ___storage.items;
                for (int i = 0; i < items.Count; i++)
                {
                    PrimaryElement component = items[i].GetComponent<PrimaryElement>();
                    if (component.Mass > 0f && (!___isLiquidConditioner || !component.Element.IsGas) && (___isLiquidConditioner || !component.Element.IsLiquid))
                    {
                        value = true;
                        object[] args = { component.Temperature };
                        _setlastGasTemp.Invoke(__instance, args);
                        float num;
                        if (___targetTemperature < component.Temperature)
                        {
                            ___temperatureDelta = Math.Max(___targetTemperature - component.Temperature, MIN_TEMPERATURE_DELTA);
                            num = Math.Max(component.Temperature + MIN_TEMPERATURE_DELTA, ___targetTemperature);
                        }
                        else
                        {
                            ___temperatureDelta = 0;
                            num = component.Temperature;
                        }
                        if (num < 1f)
                        {
                            num = 1f;
                            ___lowTempLag = Mathf.Min(___lowTempLag + dt / 5f, 1f);
                        }
                        else
                        {
                            ___lowTempLag = Mathf.Min(___lowTempLag - dt / 5f, 0f);
                        }

                        float num2 = (___isLiquidConditioner ? Game.Instance.liquidConduitFlow : Game.Instance.gasConduitFlow).AddElement(___cooledAirOutputCell, component.ElementID, component.Mass, num, component.DiseaseIdx, component.DiseaseCount);
                        component.KeepZeroMassObject = true;
                        float num3 = num2 / component.Mass;
                        int num4 = (int)((float)component.DiseaseCount * num3);
                        component.Mass -= num2;
                        component.ModifyDiseaseCount(-num4, "AirConditioner.UpdateState");
                        float num5 = (num - component.Temperature) * component.Element.specificHeatCapacity * num2;
                        Debug.Log(string.Format("{0} {1} {2}", component.Temperature, num, num5));
                        float display_dt = ((___lastSampleTime > 0f) ? (Time.time - ___lastSampleTime) : 1f);
                        ___lastSampleTime = Time.time;
                        ___heatEffect.SetHeatBeingProducedValue(Mathf.Abs(num5));
                        GameComps.StructureTemperatures.ProduceEnergy(___structureTemperature, 0f - num5, BUILDING.STATUSITEMS.OPERATINGENERGY.PIPECONTENTS_TRANSFER, display_dt);
                        break;
                    }
                }

                if (Time.time - ___lastSampleTime > 2f)
                {
                    GameComps.StructureTemperatures.ProduceEnergy(___structureTemperature, 0f, BUILDING.STATUSITEMS.OPERATINGENERGY.PIPECONTENTS_TRANSFER, Time.time - ___lastSampleTime);
                    ___lastSampleTime = Time.time;
                }

                ___operational.SetActive(value);
                _updateStatusMethod.Invoke(__instance, null);
                return false;
            }
        }
    }
}
