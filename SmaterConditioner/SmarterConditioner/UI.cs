using System;
using System.Runtime.CompilerServices;
using STRINGS;
using Microsoft.CodeAnalysis;

namespace SmarterConditioner.STRINGS
{
    // Token: 0x02000009 RID: 9
    public static class UI
    {
        // Token: 0x0200000E RID: 14
        public static class UISIDESCREENS
        {
            // Token: 0x0200000F RID: 15
            public static class AIRCONDITIONERTEMPERATURESIDESCREEN
            {
                // Token: 0x0400000E RID: 14
                public static LocString TITLE = "Cooling Temperature";

                // Token: 0x0400000F RID: 15
                public static LocString TOOLTIP = string.Concat(new string[]
                {
                    "This device will adjust the temperature of fluids passing through by ",
                    global::STRINGS.UI.FormatAsKeyWord("{0}{1}"),
                    ", consuming ",
                    global::STRINGS.UI.FormatAsKeyWord("{2}{3}"),
                    " of power to do so."
                });
            }
        }
    }
}
