using KSP.Localization;
using ABCORS_KACWrapper;
using ClickThroughFix;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ABCORS
{
    internal class AlarmManager : MonoBehaviour
    {
        internal static AlarmManager am = null;

        const int wnd_width = 200;
        const int wnd_height = 50;

        Rect alarmManagerWin = new Rect(100.0f, 100.0f, wnd_width, wnd_height);
        GUIStyle winStyle;

        static double alarmTime;
        static string strAlarmtime;
        public static double AlarmTime { set {  alarmTime = value;
                strAlarmtime= KSPUtil.PrintTime((int)(Planetarium.GetUniversalTime() - value), 5, true);
            }  get { return alarmTime; } }

        string descr = Localizer.Format("#LOC_ABCORS_ABCORS_Orbit_Alarm");
        string title = "";

        void Start()
        {
            winStyle = new GUIStyle(HighLogic.Skin.window);
            winStyle.active.background = winStyle.normal.background;
            Texture2D tex = winStyle.normal.background; //.CreateReadable();

            var pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; ++i)
                pixels[i].a = 255;

            tex.SetPixels32(pixels); tex.Apply();

            winStyle.active.background = tex;
            winStyle.focused.background = tex;
            winStyle.normal.background = tex;

            title = FlightGlobals.ActiveVessel.vesselName;

        }

        void OnDestroy()
        {
            am = null;
        }

        public void OnGUI()
        {
            alarmManagerWin = ClickThruBlocker.GUILayoutWindow(565949, alarmManagerWin, AlarmManagerWin, Localizer.Format("#LOC_ABCORS_ABCORS_Alarm_Manager"), winStyle);
        }

        void AlarmManagerWin(int id)
        {
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label(Localizer.Format("#LOC_ABCORS_Alarm_time") + strAlarmtime);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(Localizer.Format("#LOC_ABCORS_Title"), GUILayout.Width(50));
                    title = GUILayout.TextField(title, GUILayout.Width(150));
                    GUILayout.FlexibleSpace();
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(Localizer.Format("#LOC_ABCORS_Descr"), GUILayout.Width(50));
                    descr = GUILayout.TextField(descr, GUILayout.MinWidth(150));
                    GUILayout.FlexibleSpace();
                }
                GUILayout.Space(20);

                if (KACWrapper.APIReady && GUILayout.Button(Localizer.Format("#LOC_ABCORS_Set_KAC_Alarm")))
                {
                    String aID = KACWrapper.KAC.CreateAlarm(KACWrapper.KACAPI.AlarmTypeEnum.Raw, title, alarmTime);

                    if (aID != "")
                    {
                        Debug.Log("ABCORS, aID: " + aID);
                        //if the alarm was made get the object so we can update it
                        KACWrapper.KACAPI.KACAlarm a = KACWrapper.KAC.Alarms.First(z => z.ID == aID);

                        //Now update some of the other properties
                        a.Notes = descr; 
                        a.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.PauseGame;


                    }
                }
                if (!KACWrapper.APIReady || !HighLogic.CurrentGame.Parameters.CustomParams<ABCORSSettings>().ignoreStock)
                {
                    if (GUILayout.Button(Localizer.Format("#LOC_ABCORS_Set_Stock_Alarm")))
                    {
                        AlarmTypeRaw alarmToSet = new AlarmTypeRaw
                        {
                            title = this.title,
                            description = descr,
                            actions =
                            {
                                warp = AlarmActions.WarpEnum.KillWarp,
                                message = AlarmActions.MessageEnum.Yes
                            },
                            ut = alarmTime
                        };
                        AlarmClockScenario.AddAlarm(alarmToSet);

                    }
                }
                GUILayout.Space(10);
                if (GUILayout.Button(Localizer.Format("#LOC_ABCORS_Close")))
                    Destroy(this);
            }
            GUI.DragWindow();
        }
    }
}

