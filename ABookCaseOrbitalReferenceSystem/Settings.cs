using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

// http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
// search for "Mod integration into Stock Settings

namespace ABCORS
{
    public class ABCORSSettings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return ""; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "ABCORS"; } }
        public override string DisplaySection { get { return "ABCORS"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomParameterUI("Allow Target")]
        public bool allowTarget = true;

        [GameParameters.CustomParameterUI("Show Time")]
        public bool showTime = true;

        [GameParameters.CustomParameterUI("Show Altitude")]
        public bool showAltitude = true;

        [GameParameters.CustomParameterUI("Show Speed")]
        public bool showSpeed = false;

        [GameParameters.CustomParameterUI("Show Angle to Prograde")]
        public bool showAngleToPrograde = false;

        [GameParameters.CustomIntParameterUI("Width of display", minValue = 100, maxValue = 200)]
        public int displayWidth = 160;

        [GameParameters.CustomIntParameterUI("Height of display", minValue = 100, maxValue = 200)]
        public int displayHeight = 160;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        { }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            return true;
        }
        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            return true;
        }
        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }
}
