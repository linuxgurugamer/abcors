using ABCORS_KACWrapper;
using KSP.Localization;
using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace ABCORS
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    internal class ABookCaseOrbitalReferenceSystem : MonoBehaviour
    {
        private bool _mouseOver = false;
        private bool _isTarget = false;

        private Orbit _hitOrbit = null;
        private Vector3 _hitScreenPoint = new Vector3(0, 0, 0);
        private double _hitUT = 0;

        private Rect _popup = new Rect(0f, 0f, 160f, 160f);



        protected void Start()
        {
            _popup.Set(0, 0, HighLogic.CurrentGame.Parameters.CustomParams<ABCORSSettings>().displayWidth, HighLogic.CurrentGame.Parameters.CustomParams<ABCORSSettings>().displayHeight);

            KACWrapper.InitKACWrapper();
            if (KACWrapper.APIReady)
            {
                //All good to go
                Debug.Log("ABCORS KACWrapper.KAC.Alarms.Count: " + KACWrapper.KAC.Alarms.Count);
            }
        }

        private void Awake()
        {
            _popup.center = new Vector2(Screen.width * 0.5f - _popup.width * 0.5f,
                Screen.height * 0.5f - _popup.height * 0.5f);
        }

        private void FixedUpdate()
        {
            _mouseOver = MapView.MapIsEnabled && MouseOverOrbit();

            if (!_mouseOver)
                return;

            _popup.center = new Vector2(_hitScreenPoint.x, Screen.height - _hitScreenPoint.y);
        }

        private void OnGUI()
        {
            if (!_mouseOver)
                return;

            GUI.skin = HighLogic.Skin;

            Orbit orbit = _hitOrbit;
            Vector3d deltaPos = orbit.getPositionAtUT(_hitUT) - orbit.referenceBody.position;
            double altitude = deltaPos.magnitude - orbit.referenceBody.Radius;
            double speed = orbit.getOrbitalSpeedAt(orbit.getObtAtUT(_hitUT));

            string labelText = "";
            string Time = Localizer.Format("#autoLoc_Main_Time");
            string Altitude = Localizer.Format("#autoLoc_Main_Altitude");
            string Speed = Localizer.Format("#autoLoc_Main_Speed");
            string AngleToPrograde = Localizer.Format("#autoLoc_Main_AngleToPrograde");
            if (HighLogic.CurrentGame.Parameters.CustomParams<ABCORSSettings>().showTime)
            {
                labelText += Time + KSPUtil.PrintTime((int)(Planetarium.GetUniversalTime() - _hitUT), 5, true) + "\n";
            }
            if (HighLogic.CurrentGame.Parameters.CustomParams<ABCORSSettings>().showAltitude)
            {
                labelText += Altitude + altitude.ToString("N0", CultureInfo.CurrentCulture) + "m\n";  // NO_LOCALIZATION
            }
            if (HighLogic.CurrentGame.Parameters.CustomParams<ABCORSSettings>().showSpeed)
            {
                labelText += Speed + speed.ToString("N0", CultureInfo.CurrentCulture) + "m/s\n"; // NO_LOCALIZATION
            }
            if (HighLogic.CurrentGame.Parameters.CustomParams<ABCORSSettings>().showAngleToPrograde && orbit.referenceBody.orbit != null)
            {
                Vector3d bodyVel = orbit.referenceBody.orbit.getOrbitalVelocityAtUT(_hitUT);
                Vector3d shipPos = orbit.getRelativePositionAtUT(_hitUT);
                double angle = Vector3d.Angle(shipPos, bodyVel);
                Vector3d rotatedBodyVel = QuaternionD.AngleAxis(90.0, Vector3d.forward) * bodyVel;
                if (Vector3d.Dot(rotatedBodyVel, shipPos) > 0)
                {
                    angle = 360 - angle;
                }

                labelText += AngleToPrograde + angle.ToString("N1", CultureInfo.CurrentCulture) + "\u00B0\n";
            }

            GUILayout.BeginArea(GUIUtility.ScreenToGUIRect(_popup));
            GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("Label"));  // NO_LOCALIZATION
            if (_isTarget)
                labelStyle.normal.textColor = Color.cyan;
            GUILayout.Label(labelText, labelStyle);
            GUILayout.EndArea();
            MouseEvent();
        }

        private bool MouseOverOrbit()
        {
            _isTarget = false;
            _hitOrbit = null;
            _hitUT = 0;

            if (FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.patchedConicSolver == null)
                return false;

            if (MouseOverVessel(FlightGlobals.ActiveVessel))
            {
                return true;
            }

            // no hit on the main vessel, let's try the target
            if (HighLogic.CurrentGame.Parameters.CustomParams<ABCORSSettings>().allowTarget && FlightGlobals.ActiveVessel.targetObject != null)
            {
                Vessel targetVessel = FlightGlobals.ActiveVessel.targetObject as Vessel;
                if (targetVessel != null)
                {
                    if (MouseOverVessel(targetVessel))
                    {
                        _isTarget = true;
                        return true;
                    }
                }
                else
                {
                    if (MouseOverTargetable(FlightGlobals.ActiveVessel.targetObject))
                    {
                        _isTarget = true;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool MouseOverVessel(Vessel vessel)
        {
            bool result = false;

            var patchRenderer = vessel.patchedConicRenderer;

            if (patchRenderer == null || patchRenderer.solver == null)
                return result;

            var patches = patchRenderer.solver.maneuverNodes.Any()
                ? patchRenderer.flightPlanRenders
                : patchRenderer.patchRenders;

            if (patches == null)
                return result;

            PatchedConics.PatchCastHit hit = default(PatchedConics.PatchCastHit);
            if (PatchedConics.ScreenCast(Input.mousePosition, patches, out hit))
            {
                result = true;
                _hitOrbit = hit.pr.patch;
                _hitScreenPoint = hit.GetScreenSpacePoint();
                _hitUT = hit.UTatTA;
            }

            return result;
        }

        private bool MouseOverTargetable(ITargetable targetable)
        {
            bool result = false;

            OrbitDriver targetDriver = targetable.GetOrbitDriver();
            OrbitRenderer.OrbitCastHit rendererHit = default(OrbitRenderer.OrbitCastHit);
            if (targetDriver != null && targetDriver.Renderer.OrbitCast(Input.mousePosition, out rendererHit))
            {
                result = true;
                _hitOrbit = rendererHit.or.driver.orbit;
                _hitScreenPoint = rendererHit.GetScreenSpacePoint();
                _hitUT = rendererHit.UTatTA;
            }

            return result;
        }

        private void MouseEvent()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<ABCORSSettings>().showAlarmDialog &&
                (HighLogic.CurrentGame.Parameters.CustomParams<ABCORSSettings>().useRightButton && Input.GetKey(KeyCode.Mouse1) ||
                (HighLogic.CurrentGame.Parameters.CustomParams<ABCORSSettings>().useLeftButton && Input.GetKey(KeyCode.Mouse0)))
                )
            {
                if (AlarmManager.am == null)
                    AlarmManager.am =  new GameObject().AddComponent<AlarmManager>();
                AlarmManager.AlarmTime = _hitUT;
            }
        }
    }
}
