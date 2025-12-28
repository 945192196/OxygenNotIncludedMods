using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KSerialization;
using SmarterConditioner.STRINGS;
using STRINGS;
using UnityEngine;
using static STRINGS.UI.UISIDESCREENS;
using System.Reflection;

namespace SmarterConditioner
{


    [SerializationConfig(MemberSerialization.OptIn)]
    public class SmarterConditioner : KMonoBehaviour, IUserControlledCapacity
    {
        private static void SetTargetTemperatureDirect(AirConditioner instance, float value)
        {
            FieldInfo field = typeof(AirConditioner).GetField(
                "targetTemperature",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            field.SetValue(instance, value);
        }

        private static void OnCopySettings(SmarterConditioner comp, object data)
        {
            comp.OnCopySettings(data);
        }

        public float UserMaxCapacity
        {
            get
            {
                return GameUtil.GetTemperatureConvertedFromKelvin(this.targetTemperature, GameUtil.temperatureUnit);
            }
            set
            {
                this.targetTemperature = GameUtil.GetTemperatureConvertedToKelvin(value);
            }
        }

        public float AmountStored
        {
            get
            {
                return this.UserMaxCapacity;
            }
        }

        public float MinCapacity
        {
            get
            {
                return GameUtil.GetTemperatureConvertedFromKelvin(0f, GameUtil.temperatureUnit);
            }
        }

        public float MaxCapacity
        {
            get
            {
                return GameUtil.GetTemperatureConvertedFromKelvin(573.15f, GameUtil.temperatureUnit);
            }
        }

        public bool WholeValues
        {
            get
            {
                return false;
            }
        }
        
        public LocString CapacityUnits
        {
            get
            {
                return GameUtil.GetTemperatureUnitSuffix();
            }
        }

        public int SliderDecimalPlaces(int i)
        {
            return 8;
        }

        public float GetSliderValue(int i)
        {
            return this.targetTemperature;
        }

        public string GetSliderTooltipKey(int i)
        {
            return "STRINGS.UI.UISIDESCREENS.AIRCONDITIONERTEMPERATURESIDESCREEN.TOOLTIP";
        }

        public string GetSliderTooltip()
        {
                return string.Format(SmarterConditioner.STRINGS.UI.UISIDESCREENS.AIRCONDITIONERTEMPERATURESIDESCREEN.TOOLTIP, new object[]
            {
                this.targetTemperature,
                this.SliderUnits,
                this.GetWattsConsumed(),
                global::STRINGS.UI.UNITSUFFIXES.ELECTRICAL.WATT
            });
        }

        public string SliderTitleKey
        {
            get
            {
                return "STRINGS.UI.UISIDESCREENS.AIRCONDITIONERTEMPERATURESIDESCREEN.TITLE";
            }
        }

        public string SliderUnits
        {
            get
            {
                return GameUtil.GetTemperatureUnitSuffix();
            }
        }

        public void SetSliderValue(float val, int i)
        {
            this.targetTemperature = val;
            this.Update();
        }


        // 缓存基础功率值，避免重复获取
        // 基础功率是固定的：空调 340W，液体冷却器 1700W
        private float? _baseWatts = null;

        private float GetBaseWatts()
        {
            if (!_baseWatts.HasValue)
            {
                // 检查是否为液体冷却器
                bool isLiquidConditioner = this.airConditioner.GetComponent<LiquidConditioner>() != null;
                _baseWatts = isLiquidConditioner ? 1700f : 340f;
            }
            return _baseWatts.Value;
        }

        public float GetWattsConsumed()
        {
            // 使用反射获取 temperatureDelta 字段
            float temperatureDelta = GetTemperatureDelta();
            float baseWatts = GetBaseWatts();
            // 计算实际功率：基础功率 * (温度差 / 20)，最小为 0
            return Mathf.Max(0f, Mathf.Abs(baseWatts * (temperatureDelta / 20f)));
        }

        private static readonly FieldInfo _temperatureDeltaField = typeof(AirConditioner)
            .GetField("temperatureDelta", BindingFlags.NonPublic | BindingFlags.Instance);

        private float GetTemperatureDelta()
        {
            if (_temperatureDeltaField != null && this.airConditioner != null)
            {
                object value = _temperatureDeltaField.GetValue(this.airConditioner);
                if (value != null)
                {
                    return (float)value;
                }
            }
            // 如果无法获取，使用目标温度与实际温度的差值作为估算
            float currentTemp = this.airConditioner.TargetTemperature;
            return Mathf.Abs(this.targetTemperature - currentTemp);
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            base.Subscribe<SmarterConditioner>(-905833192, SmarterConditioner.OnCopySettingsDelegate);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // 初始化时设置目标温度
            if (targetTemperature != this.airConditioner.TargetTemperature)
            {
                SetTargetTemperatureDirect(this.airConditioner, targetTemperature);
            }
            this.Update();
        }

        internal void OnCopySettings(object data)
        {
            SmarterConditioner comp = ((GameObject)data).GetComponent<SmarterConditioner>();
            if (comp != null)
            {
                this.targetTemperature = comp.targetTemperature;
            }
        }

        internal void Update()
        {
            if (targetTemperature != this.airConditioner.TargetTemperature)
            {
                SetTargetTemperatureDirect(this.airConditioner, targetTemperature);
            }
            // 更新实际功率消耗
            float actualWatts = this.GetWattsConsumed();
            this.energyConsumer.BaseWattageRating = actualWatts;
            
            // 同时更新 BuildingDef 中的功率值，以便电路负载计算使用
            UpdateBuildingDefPower(actualWatts);
        }

        private void UpdateBuildingDefPower(float watts)
        {
            // 直接通过 Building.Def 属性更新功率值
            // 这样 WattsNeededWhenActive 就会返回正确的值
            if (this.energyConsumer.building != null && this.energyConsumer.building.Def != null)
            {
                this.energyConsumer.building.Def.EnergyConsumptionWhenActive = watts;
            }
        }

        private static readonly EventSystem.IntraObjectHandler<SmarterConditioner> OnCopySettingsDelegate = new EventSystem.IntraObjectHandler<SmarterConditioner>(new Action<SmarterConditioner, object>(SmarterConditioner.OnCopySettings));
        public const string KEY = "STRINGS.UI.UISIDESCREENS.AIRCONDITIONERTEMPERATURESIDESCREEN";

        [MyCmpReq]
        public AirConditioner airConditioner;

        [MyCmpReq]
        public EnergyConsumer energyConsumer;

        [MyCmpAdd]
        public CopyBuildingSettings copyBuildingSettings;

        [Serialize]
        private float targetTemperature = 293.15f; // 默认20摄氏度

    }
}
