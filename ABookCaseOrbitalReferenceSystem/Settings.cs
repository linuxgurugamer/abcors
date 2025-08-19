using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP.Localization;

// http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
// search for "Mod integration into Stock Settings

namespace ABCORS
{
    public class ABCORSSettings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return ""; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return Localizer.Format("#LOC_ABCORS_ABCORS"); } }
        public override string DisplaySection { get { return Localizer.Format("#LOC_ABCORS_ABCORS"); } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomParameterUI("#autoLoc_Settings_allowTarget")]
        public bool allowTarget = true;

        [GameParameters.CustomParameterUI("#autoLoc_Settings_showTime")]
        public bool showTime = true;

        [GameParameters.CustomParameterUI("#autoLoc_Settings_showAltitude")]
        public bool showAltitude = true;

        [GameParameters.CustomParameterUI("#autoLoc_Settings_showSpeed")]
        public bool showSpeed = false;

        [GameParameters.CustomParameterUI("#autoLoc_Settings_showAngleToPrograde")]
        public bool showAngleToPrograde = false;

        [GameParameters.CustomIntParameterUI("#autoLoc_Settings_displayWidth", minValue = 100, maxValue = 200)]
        public int displayWidth = 160;

        [GameParameters.CustomIntParameterUI("#autoLoc_Settings_displayHeight", minValue = 100, maxValue = 200)]
        public int displayHeight = 160;



        [GameParameters.CustomParameterUI("Show dialog to add alarm")]
        public bool showAlarmDialog = true;

        [GameParameters.CustomParameterUI("Use Left Button to show alarm dialog")]
        public bool useLeftButton = false;

        [GameParameters.CustomParameterUI("Use Right Button to show alarm dialog")]
        public bool useRightButton = true;

        [GameParameters.CustomParameterUI("Ignore stock alarm if KAC is installed")]
        public bool ignoreStock = true;



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
