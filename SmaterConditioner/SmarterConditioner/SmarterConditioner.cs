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


        public float GetWattsConsumed()
        {
            return Mathf.Abs(this.energyConsumer.WattsNeededWhenActive * (this.airConditioner.temperatureDelta / 20f));
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            base.Subscribe<SmarterConditioner>(-905833192, SmarterConditioner.OnCopySettingsDelegate);
        }

        protected override void OnSpawn()
        {
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
            this.energyConsumer.BaseWattageRating = this.GetWattsConsumed();
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
        private float targetTemperature = 0f;

    }
}
