using System.Collections.Generic;
using UnityEngine;

namespace ABCORS
{
    public class Utils
    {



        public static class ScrollingTextFieldHelper
        {
            private class FieldState
            {
                public float scrollX;
                public bool wasFocused;
                public float unfocusedAt;
            }

            private static readonly Dictionary<string, FieldState> states =
                new Dictionary<string, FieldState>();

            public static string TextField(
                string controlName,
                string text,
                float width = 150f,
                float speed = 25f,
                int gapSpaces = 5)
            {
                text = text ?? "";

                FieldState state;
                if (!states.TryGetValue(controlName, out state))
                {
                    state = new FieldState();
                    state.unfocusedAt = Time.realtimeSinceStartup;
                    states.Add(controlName, state);
                }

                GUIStyle fieldStyle = GUI.skin.textField;

                float height = Mathf.Max(
                    20f,
                    fieldStyle.lineHeight + fieldStyle.padding.vertical);

                Rect rect = GUILayoutUtility.GetRect(
                    width, height,
                    GUILayout.Width(width),
                    GUILayout.Height(height));

                // The native text field handles keyboard input.
                // Its text is hidden because we draw it ourselves.
                GUIStyle hiddenStyle = new GUIStyle(fieldStyle);
                hiddenStyle.wordWrap = false;
                hiddenStyle.clipping = TextClipping.Clip;

                Color transparent = Color.clear;

                hiddenStyle.normal.textColor = transparent;
                hiddenStyle.hover.textColor = transparent;
                hiddenStyle.active.textColor = transparent;
                hiddenStyle.focused.textColor = transparent;

                hiddenStyle.onNormal.textColor = transparent;
                hiddenStyle.onHover.textColor = transparent;
                hiddenStyle.onActive.textColor = transparent;
                hiddenStyle.onFocused.textColor = transparent;

                // CRITICAL: Always create exactly one interactive
                // control here, on EVERY OnGUI event.
                GUI.SetNextControlName(controlName);

                string result = GUI.TextField(
                    rect, text, hiddenStyle);

                bool focused =
                    GUI.GetNameOfFocusedControl() == controlName;

                // Track focus changes independently for every field.
                if (focused != state.wasFocused)
                {
                    state.scrollX = 0f;

                    if (!focused)
                        state.unfocusedAt = Time.realtimeSinceStartup;

                    state.wasFocused = focused;
                }

                float innerWidth = Mathf.Max(
                    1f,
                    width -
                    fieldStyle.padding.left -
                    fieldStyle.padding.right);

                GUIStyle drawStyle = new GUIStyle(fieldStyle);

                drawStyle.normal.background = null;
                drawStyle.hover.background = null;
                drawStyle.active.background = null;
                drawStyle.focused.background = null;

                drawStyle.padding = new RectOffset(0, 0, 0, 0);
                drawStyle.margin = new RectOffset(0, 0, 0, 0);
                drawStyle.wordWrap = false;
                drawStyle.clipping = TextClipping.Clip;
                drawStyle.alignment = TextAnchor.MiddleLeft;

                float textWidth = drawStyle.CalcSize(
                    new GUIContent(result)).x;

                TextEditor editor = null;

                // Only access the editor for the focused field.
                if (focused && GUIUtility.keyboardControl != 0)
                {
                    editor = GUIUtility.GetStateObject(
                        typeof(TextEditor),
                        GUIUtility.keyboardControl) as TextEditor;
                }

                // Follow the cursor while editing.
                if (focused && editor != null)
                {
                    int cursorIndex = Mathf.Clamp(
                        editor.cursorIndex, 0, result.Length);

                    float cursorX = drawStyle.GetCursorPixelPosition(
                        new Rect(
                            0f, 0f,
                            Mathf.Max(textWidth + 20f, innerWidth),
                            height),
                        new GUIContent(result),
                        cursorIndex).x;

                    float margin = 5f;
                    float visibleX = cursorX - state.scrollX;

                    if (visibleX > innerWidth - margin)
                    {
                        state.scrollX +=
                            visibleX - (innerWidth - margin);
                    }
                    else if (visibleX < margin)
                    {
                        state.scrollX -= margin - visibleX;
                    }

                    state.scrollX = Mathf.Clamp(
                        state.scrollX,
                        0f,
                        Mathf.Max(0f, textWidth - innerWidth + 10f));
                }

                // From here onward, ONLY draw on Repaint.
                // Do not call GUI.Label, GUI.Box, GUI.TextField,
                // or any other interactive/layout controls.
                if (Event.current.type != EventType.Repaint)
                    return result;

                Rect clipRect = new Rect(
                    rect.x + fieldStyle.padding.left,
                    rect.y,
                    innerWidth,
                    rect.height);

                // BeginClip clips drawing without introducing another
                // text-field control.
                GUI.BeginClip(clipRect);

                try
                {
                    if (focused)
                    {
                        // EDITING: No marquee.
                        drawStyle.Draw(
                            new Rect(
                                -state.scrollX,
                                0f,
                                Mathf.Max(textWidth + 20f, innerWidth),
                                height),
                            result,
                            false, false, false, false);

                        // Custom blinking insertion cursor.
                        if (editor != null &&
                            editor.cursorIndex == editor.selectIndex)
                        {
                            int cursorIndex = Mathf.Clamp(
                                editor.cursorIndex, 0, result.Length);

                            float cursorX =
                                drawStyle.GetCursorPixelPosition(
                                    new Rect(
                                        0f, 0f,
                                        Mathf.Max(
                                            textWidth + 20f, innerWidth),
                                        height),
                                    new GUIContent(result),
                                    cursorIndex).x;

                            float x = cursorX - state.scrollX;

                            if (x >= 0f && x <= innerWidth &&
                                (Time.realtimeSinceStartup % 1f) < 0.5f)
                            {
                                Color oldColor = GUI.color;
                                GUI.color = fieldStyle.normal.textColor;

                                GUI.DrawTexture(
                                    new Rect(x, 3f, 1f, height - 6f),
                                    Texture2D.whiteTexture);

                                GUI.color = oldColor;
                            }
                        }
                    }
                    else if (textWidth <= innerWidth)
                    {
                        // Normal display when text fits.
                        drawStyle.Draw(
                            new Rect(0f, 0f, innerWidth, height),
                            result,
                            false, false, false, false);
                    }
                    else
                    {
                        // Unfocused marquee.
                        string repeated =
                            result +
                            new string(' ', Mathf.Max(0, gapSpaces));

                        float cycleWidth = drawStyle.CalcSize(
                            new GUIContent(repeated)).x;

                        if (cycleWidth > 0f)
                        {
                            float elapsed =
                                Time.realtimeSinceStartup -
                                state.unfocusedAt;

                            float offset =
                                (elapsed * Mathf.Max(0f, speed))
                                % cycleWidth;

                            for (float x = -offset;
                                 x < innerWidth;
                                 x += cycleWidth)
                            {
                                drawStyle.Draw(
                                    new Rect(
                                        x, 0f,
                                        cycleWidth + 2f, height),
                                    repeated,
                                    false, false, false, false);
                            }
                        }
                    }
                }
                finally
                {
                    GUI.EndClip();
                }

                return result;
            }

            public static void Remove(string controlName)
            {
                states.Remove(controlName);
            }

            public static void Clear()
            {
                states.Clear();
            }
        }


    }
}