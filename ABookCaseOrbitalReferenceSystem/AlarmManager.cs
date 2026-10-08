
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

        private Rect alarmManagerWin = new Rect(
            100f, 100f, wnd_width, wnd_height);

        private GUIStyle winStyle;

        private bool openAtMouse = false;
        private bool windowMoved = false;

        private static double alarmTime;
        private static string strAlarmtime;

        public static double AlarmTime
        {
            set
            {
                alarmTime = value;

                strAlarmtime = KSPUtil.PrintTime(
                    (int)(Planetarium.GetUniversalTime() - value),
                    5,
                    true);
            }
            get
            {
                return alarmTime;
            }
        }

        private string descr = Localizer.Format(
            "#LOC_ABCORS_ABCORS_Orbit_Alarm");

        private string title = "";

        void Start()
        {
            // Build the configuration file path.
            configFilePath = Path.Combine(
                KSPUtil.ApplicationRootPath,
                ConfigRelativePath);

            // Read the setting once when the window opens.
            openAtMouse =
                HighLogic.CurrentGame != null &&
                HighLogic.CurrentGame.Parameters
                    .CustomParams<ABCORSSettings>()
                    .openAtMouse;

            if (openAtMouse)
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

            // Initialize title from active vessel.
            if (FlightGlobals.ActiveVessel != null)
            {
                title = FlightGlobals.ActiveVessel.vesselName;
            }
        }

        /// <summary>
        /// Positions the window at the current mouse cursor.
        /// </summary>
        private void PositionWindowAtMouse()
        {
            Vector3 mousePos = Input.mousePosition;

            // Convert Unity screen coordinates (bottom-left)
            // to IMGUI coordinates (top-left).
            alarmManagerWin.x = mousePos.x;
            alarmManagerWin.y = Screen.height - mousePos.y;

            ClampWindowToScreen();
        }

        /// <summary>
        /// Keeps the window within screen boundaries.
        /// Uses the actual dimensions of alarmManagerWin.
        /// </summary>
        private void ClampWindowToScreen()
        {
            float width = Mathf.Max(1f, alarmManagerWin.width);
            float height = Mathf.Max(1f, alarmManagerWin.height);

            float maxX = Mathf.Max(
                0f,
                Screen.width - width);

            float maxY = Mathf.Max(
                0f,
                Screen.height - height);

            alarmManagerWin.x = Mathf.Clamp(
                alarmManagerWin.x,
                0f,
                maxX);

            alarmManagerWin.y = Mathf.Clamp(
                alarmManagerWin.y,
                0f,
                maxY);
        }

        /// <summary>
        /// Loads the saved window position from AlarmManager.cfg.
        /// </summary>
        private void LoadWindowPosition()
        {
            try
            {
                if (!File.Exists(configFilePath))
                {
                    Debug.Log(
                        "[ABCORS] No saved AlarmManager position. " +
                        "Using default position.");

                    ClampWindowToScreen();
                    return;
                }

                ConfigNode data = ConfigNode.Load(configFilePath);

                if (data == null)
                {
                    Debug.LogWarning(
                        "[ABCORS] Unable to load AlarmManager.cfg");

                    return;
                }

                ConfigNode node = data.GetNode(ConfigNodeName);

                if (node == null)
                {
                    Debug.LogWarning(
                        "[ABCORS] ALARM_MANAGER node not found.");

                    return;
                }

                float x = alarmManagerWin.x;
                float y = alarmManagerWin.y;

                string savedX = node.GetValue(ConfigWindowX);
                string savedY = node.GetValue(ConfigWindowY);

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

                alarmManagerWin.x = x;
                alarmManagerWin.y = y;

                ClampWindowToScreen();

                Debug.Log(
                    "[ABCORS] Loaded AlarmManager position: " +
                    alarmManagerWin.x.ToString(
                        CultureInfo.InvariantCulture) +
                    ", " +
                    alarmManagerWin.y.ToString(
                        CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[ABCORS] Error loading AlarmManager.cfg: " +
                    ex);
            }
        }

        /// <summary>
        /// Saves the window position using a ConfigNode.
        /// Does nothing when openAtMouse is enabled.
        /// </summary>
        private void SaveWindowPosition()
        {
            // Never overwrite saved positions when the window
            // was opened using the mouse position.
            if (openAtMouse)
                return;

            try
            {
                if (string.IsNullOrEmpty(configFilePath))
                    return;

                // Ensure PluginData directory exists.
                string directory =
                    Path.GetDirectoryName(configFilePath);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // Create unnamed root node.
                ConfigNode data = new ConfigNode();

                // Create ALARM_MANAGER child node.
                ConfigNode node =
                    new ConfigNode(ConfigNodeName);

                node.AddValue(
                    ConfigWindowX,
                    alarmManagerWin.x.ToString(
                        CultureInfo.InvariantCulture));

                node.AddValue(
                    ConfigWindowY,
                    alarmManagerWin.y.ToString(
                        CultureInfo.InvariantCulture));

                data.AddNode(node);

                // Save root node to disk.
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
            // Save final position only when openAtMouse is false.
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

            // Clamp after GUILayoutWindow calculates
            // the actual dimensions.
            ClampWindowToScreen();

            // Only save changes when openAtMouse is disabled.
            if (!openAtMouse)
            {
                if (alarmManagerWin.position != oldPosition)
                    windowMoved = true;

                if (windowMoved &&
                    Event.current.rawType == EventType.MouseUp)
                {
                    SaveWindowPosition();
                }
            }
        }

        private void AlarmManagerWin(int id)
        {
            using (new GUILayout.VerticalScope())
            {
                // Display alarm time.
                GUILayout.Label(
                    Localizer.Format("#LOC_ABCORS_Alarm_time") +
                    " " + strAlarmtime);

                // Alarm title.
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

                // Alarm description.
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

                // Create a Kerbal Alarm Clock alarm.
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

                // Create a stock KSP alarm.
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
                                        AlarmActions.WarpEnum
                                            .KillWarp,

                                    message =
                                        AlarmActions.MessageEnum
                                            .Yes
                                },
                                ut = alarmTime
                            };

                        AlarmClockScenario.AddAlarm(alarmToSet);
                    }
                }

                GUILayout.Space(10);

                // Close window.
                if (GUILayout.Button(
                    Localizer.Format("#LOC_ABCORS_Close")))
                {
                    SaveWindowPosition();
                    Destroy(this);
                }
            }

            // Only drag from the title bar.
            // Avoid interfering with text fields and buttons.
            GUI.DragWindow(
                new Rect(0f, 0f, 10000f, 20f));
        }
    }
}
