#if false
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
                GUILayout.Label(Localizer.Format("#LOC_ABCORS_Alarm_time") + " " + strAlarmtime);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(Localizer.Format("#LOC_ABCORS_Title"), GUILayout.Width(65));
                    //title = GUILayout.TextField(title, GUILayout.Width(150));
                    title = ABCORS.Utils.ScrollingTextFieldHelper.TextField("title", title, width:150);
                    GUILayout.FlexibleSpace();
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(Localizer.Format("#LOC_ABCORS_Descr"), GUILayout.Width(65));
                    // descr = GUILayout.TextField(descr, GUILayout.MinWidth(150));
                    descr = ABCORS.Utils.ScrollingTextFieldHelper.TextField("descr", descr, width: 150);
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
                if (!ABookCaseOrbitalReferenceSystem.stockAlarmDisabler && (!KACWrapper.APIReady || !HighLogic.CurrentGame.Parameters.CustomParams<ABCORSSettings>().ignoreStock))
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

#else



using KSP.Localization;
using ABCORS_KACWrapper;
using ClickThroughFix;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ABCORS
{
    internal class AlarmManager : MonoBehaviour
    {
        internal static AlarmManager am = null;

        const int wnd_width = 200;
        const int wnd_height = 50;

        private const string ConfigNodeName = "ALARM_MANAGER";
        private const string ConfigWindowX = "windowX";
        private const string ConfigWindowY = "windowY";

        private const string ConfigRelativePath =
            "GameData/ABCORS/PluginData/AlarmManager.cfg";

        private string configFilePath;

        Rect alarmManagerWin = new Rect(
            100f, 100f, wnd_width, wnd_height);

        GUIStyle winStyle;

        private bool windowMoved = false;

        static double alarmTime;
        static string strAlarmtime;

        public static double AlarmTime
        {
            set
            {
                alarmTime = value;

                strAlarmtime = KSPUtil.PrintTime(
                    (int)(Planetarium.GetUniversalTime() - value),
                    5, true);
            }
            get
            {
                return alarmTime;
            }
        }

        string descr = Localizer.Format(
            "#LOC_ABCORS_ABCORS_Orbit_Alarm");

        string title = "";

        void Start()
        {
            configFilePath = Path.Combine(
                KSPUtil.ApplicationRootPath,
                ConfigRelativePath);

            if (HighLogic.CurrentGame.Parameters
                .CustomParams<ABCORSSettings>().openAtMouse)
            {
                PositionWindowAtMouse();
            }
            else
            {
                LoadWindowPosition();
            }

            // Initialize window style.
            winStyle = new GUIStyle(HighLogic.Skin.window);
            winStyle.active.background =
                winStyle.normal.background;

            Texture2D tex = winStyle.normal.background;

            if (tex != null)
            {
                try
                {
                    var pixels = tex.GetPixels32();

                    for (int i = 0; i < pixels.Length; ++i)
                        pixels[i].a = 255;

                    tex.SetPixels32(pixels);
                    tex.Apply();

                    winStyle.active.background = tex;
                    winStyle.focused.background = tex;
                    winStyle.normal.background = tex;
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        "[ABCORS] Error configuring window texture: " +
                        ex);
                }
            }

            if (FlightGlobals.ActiveVessel != null)
                title = FlightGlobals.ActiveVessel.vesselName;
        }

        private void PositionWindowAtMouse()
        {
            Vector3 mousePos = Input.mousePosition;

            // Convert from Unity screen coordinates
            // (bottom-left origin) to IMGUI coordinates
            // (top-left origin).
            float x = mousePos.x;
            float y = Screen.height - mousePos.y;

            // Keep the window within the screen.
            x = Mathf.Clamp(
                x,
                0f,
                Mathf.Max(0f, Screen.width - wnd_width));

            y = Mathf.Clamp(
                y,
                0f,
                Mathf.Max(0f, Screen.height - 25f));

            alarmManagerWin.x = x;
            alarmManagerWin.y = y;
        }

        /// <summary>
        /// Loads the window position from AlarmManager.cfg.
        /// </summary>
        private void LoadWindowPosition()
        {
            try
            {
                if (!File.Exists(configFilePath))
                {
                    Debug.Log(
                        "[ABCORS] No saved AlarmManager position found. " +
                        "Using default position.");

                    return;
                }

                ConfigNode node = ConfigNode.Load(configFilePath);

                if (node == null)
                {
                    Debug.LogWarning(
                        "[ABCORS] Unable to read AlarmManager.cfg");

                    return;
                }

                // Support a file whose root is ALARM_MANAGER,
                // or a ConfigNode containing that child node.
                ConfigNode settings = node;

                if (node.name != ConfigNodeName)
                {
                    settings = node.GetNode(ConfigNodeName);
                }

                if (settings == null)
                {
                    Debug.LogWarning(
                        "[ABCORS] ALARM_MANAGER node not found.");

                    return;
                }

                float x = alarmManagerWin.x;
                float y = alarmManagerWin.y;

                string savedX = settings.GetValue(ConfigWindowX);
                string savedY = settings.GetValue(ConfigWindowY);

                float parsed;

                if (!string.IsNullOrEmpty(savedX) &&
                    float.TryParse(
                        savedX,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out parsed))
                {
                    x = parsed;
                }

                if (!string.IsNullOrEmpty(savedY) &&
                    float.TryParse(
                        savedY,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out parsed))
                {
                    y = parsed;
                }

                if (float.IsNaN(x) || float.IsInfinity(x))
                    x = 100f;

                if (float.IsNaN(y) || float.IsInfinity(y))
                    y = 100f;

                // Ensure the saved position remains visible.
                x = Mathf.Clamp(
                    x,
                    0f,
                    Mathf.Max(0f, Screen.width - wnd_width));

                y = Mathf.Clamp(
                    y,
                    0f,
                    Mathf.Max(0f, Screen.height - 25f));

                alarmManagerWin.x = x;
                alarmManagerWin.y = y;

                Debug.Log(
                    "[ABCORS] Loaded AlarmManager position: " +
                    x.ToString(CultureInfo.InvariantCulture) +
                    ", " +
                    y.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[ABCORS] Error loading AlarmManager.cfg: " +
                    ex);
            }
        }

        /// <summary>
        /// Saves the current window position to AlarmManager.cfg.
        /// </summary>
        private void SaveWindowPosition()
        {
            try
            {
                if (string.IsNullOrEmpty(configFilePath))
                    return;

                // Ensure PluginData exists.
                string directory =
                    Path.GetDirectoryName(configFilePath);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                ConfigNode data = new ConfigNode();
                ConfigNode node = new ConfigNode(ConfigNodeName);

                node.AddValue(
                    ConfigWindowX,
                    alarmManagerWin.x.ToString(
                        CultureInfo.InvariantCulture));

                node.AddValue(
                    ConfigWindowY,
                    alarmManagerWin.y.ToString(
                        CultureInfo.InvariantCulture));
                data.AddNode(node);
                data.Save(configFilePath);

                windowMoved = false;

                Debug.Log(
                    "[ABCORS] Saved AlarmManager position: " +
                    alarmManagerWin.x.ToString(
                        CultureInfo.InvariantCulture) +
                    ", " +
                    alarmManagerWin.y.ToString(
                        CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[ABCORS] Error saving AlarmManager.cfg: " +
                    ex);
            }
        }

        void OnDestroy()
        {
            SaveWindowPosition();
            am = null;
        }

        public void OnGUI()
        {
            if (winStyle == null)
                return;

            Vector2 oldPosition = alarmManagerWin.position;

            alarmManagerWin = ClickThruBlocker.GUILayoutWindow(
                565949,
                alarmManagerWin,
                AlarmManagerWin,
                Localizer.Format(
                    "#LOC_ABCORS_ABCORS_Alarm_Manager"),
                winStyle);

            if (alarmManagerWin.position != oldPosition)
                windowMoved = true;

            if (windowMoved &&
                Event.current.rawType == EventType.MouseUp)
            {
                SaveWindowPosition();
            }
        }

        void AlarmManagerWin(int id)
        {
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label(
                    Localizer.Format("#LOC_ABCORS_Alarm_time") +
                    " " + strAlarmtime);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(
                        Localizer.Format("#LOC_ABCORS_Title"),
                        GUILayout.Width(65));

                    title =
                        ABCORS.Utils.ScrollingTextFieldHelper.TextField(
                            "title",
                            title,
                            width: 150);

                    GUILayout.FlexibleSpace();
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(
                        Localizer.Format("#LOC_ABCORS_Descr"),
                        GUILayout.Width(65));

                    descr =
                        ABCORS.Utils.ScrollingTextFieldHelper.TextField(
                            "descr",
                            descr,
                            width: 150);

                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(20);

                if (KACWrapper.APIReady &&
                    GUILayout.Button(
                        Localizer.Format(
                            "#LOC_ABCORS_Set_KAC_Alarm")))
                {
                    String aID =
                        KACWrapper.KAC.CreateAlarm(
                            KACWrapper.KACAPI.AlarmTypeEnum.Raw,
                            title,
                            alarmTime);

                    if (aID != "")
                    {
                        Debug.Log("ABCORS, aID: " + aID);

                        KACWrapper.KACAPI.KACAlarm a =
                            KACWrapper.KAC.Alarms.First(
                                z => z.ID == aID);

                        a.Notes = descr;

                        a.AlarmAction =
                            KACWrapper.KACAPI.AlarmActionEnum
                                .PauseGame;
                    }
                }

                if (!ABookCaseOrbitalReferenceSystem
                        .stockAlarmDisabler &&
                    (!KACWrapper.APIReady ||
                     !HighLogic.CurrentGame.Parameters
                        .CustomParams<ABCORSSettings>()
                        .ignoreStock))
                {
                    if (GUILayout.Button(
                        Localizer.Format(
                            "#LOC_ABCORS_Set_Stock_Alarm")))
                    {
                        AlarmTypeRaw alarmToSet =
                            new AlarmTypeRaw
                            {
                                title = this.title,
                                description = descr,
                                actions =
                                {
                                    warp =
                                        AlarmActions.WarpEnum.KillWarp,

                                    message =
                                        AlarmActions.MessageEnum.Yes
                                },
                                ut = alarmTime
                            };

                        AlarmClockScenario.AddAlarm(alarmToSet);
                    }
                }

                GUILayout.Space(10);

                if (GUILayout.Button(
                    Localizer.Format("#LOC_ABCORS_Close")))
                {
                    SaveWindowPosition();
                    Destroy(this);
                }
            }

            // Drag by the title bar only.
            GUI.DragWindow(
                new Rect(0f, 0f, 10000f, 20f));
        }
    }
}


#endif