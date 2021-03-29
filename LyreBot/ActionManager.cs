using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Shapes;
using Melanchall.DryWetMidi;
using Melanchall.DryWetMidi.Core;
using LyreBot.ViewModels;
using Keyboard = InputManager.Keyboard;
using Timer = System.Threading.Timer;

namespace LyreBot
{
    public class ActionManager
    {
        public const uint WM_KEYDOWN = 0x0100;
        public const uint WM_KEYUP = 0x0101;
        public const uint WM_SETTEXT = 0x000C;

        private static IntPtr genshinWindow = IntPtr.Zero;

        // Dictionary of note IDs and a series of ints. In order: Scale, Fret, Key, Vibrato
        private static Dictionary<int, Keys> lyreNotes = new Dictionary<int, Keys>
        {
            { 48, Keys.D3 }, // C3
            //{ 49, Keys.D3 }, // C#3
            { 50, Keys.D3 }, // D3
            //{ 51, Keys.D3 }, // D#3
            { 52, Keys.D3 }, // E3
            { 53, Keys.D3 }, // F3
            //{ 54, Keys.D3 }, // F#3
            { 55, Keys.D3 }, // G3
            //{ 56, Keys.D3 }, // G#3
            { 57, Keys.D3 }, // A3
            //{ 58, Keys.D3 }, // A#3
            { 59, Keys.D3 }, // B3
            { 60, Keys.D3 }, // C4
            //{ 61, Keys.D3 }, // C#4
            { 62, Keys.D3 }, // D4
            //{ 63, Keys.D3 }, // D#4
            { 64, Keys.D3 }, // E4
            { 65, Keys.D3 }, // F4
            //{ 66, Keys.D3 }, // F#4
            { 67, Keys.D3 }, // G4
            //{ 68, Keys.D3 }, // G#4
            { 69, Keys.D3 }, // A4
            //{ 70, Keys.D3 }, // A#4
            { 71, Keys.D3 }, // B4
            { 72, Keys.D3 }, // C5
            //{ 73, Keys.D3 }, // C#5
            { 74, Keys.D3 }, // D5
            //{ 75, Keys.D3 }, // D#5
            { 76, Keys.D3 }, // E5
            { 77, Keys.D3 }, // F5
            { 78, Keys.D3 }, // F#5
            { 79, Keys.D3 }, // G5
            //{ 80, Keys.D3 }, // G#5
            { 81, Keys.D3 }, // A5
            //{ 82, Keys.D3 }, // A#5
            { 83, Keys.D3 }, // B5
        };

        public static int activeScale = 0;

        private enum ShowWindowEnum
        {
            Hide = 0,
            ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
            Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
            Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
            Restore = 9, ShowDefault = 10, ForceMinimized = 11
        };

        private struct WindowPlacement
        {
            public int length;
            public int flags;
            public int showCmd;
            public Point ptMinPosition;
            public Point ptMaxPosition;
            public Rectangle rcNormalPosition;
            public Rectangle rcDevice;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string className, string windowTitle);

        public static IntPtr FindWindow(string lpWindowName)
        {
            return FindWindow(null, lpWindowName);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindows);

        [DllImport("user32.dll")]
        private static extern Int32 SendMessage(IntPtr hWnd, UInt32 Msg, char wParam, UInt32 lParam);

        [DllImport("user32.dll")]
        static extern void SwitchToThisWindow(IntPtr hWnd, bool fUnknown);

        /// <summary>
        /// Play a MIDI note inside Genshin Impact.
        /// </summary>
        /// <param name="note"> The note to be played.</param>
        /// <param name="enableVibrato"> Should we use vibrato to play unplayable notes?.</param>
        /// <param name="transposeNotes"> Should we transpose unplayable notes?.</param>
        public static bool PlayNote(NoteOnEvent note, bool enableVibrato, bool transposeNotes)
        {
            //var hWnd = GetForegroundWindow();
            //if (genshinWindow.Equals(IntPtr.Zero) || !hWnd.Equals(genshinWindow)) return false;
            if (!IsWindowFocused("Genshin Impact")) return false;

            var noteId = (int)note.NoteNumber;
            if (!lyreNotes.ContainsKey(noteId))
            {
                if (transposeNotes)
                {
                    if (noteId < lyreNotes.Keys.First())
                    {
                        noteId = lyreNotes.Keys.First() + noteId % 12;
                    }
                    else if (noteId > lyreNotes.Keys.Last())
                    {
                        noteId = lyreNotes.Keys.Last() - 15 + noteId % 12;
                    }
                }
                else
                {
                    return false;
                }
            }

            PlayNote(noteId, enableVibrato, transposeNotes);
            return true;
        }

        /// <summary>
        /// Play a MIDI note inside Genshin Impact.
        /// </summary>
        /// <param name="noteId"> The MIDI ID of the note to be played.</param>
        /// <param name="enableVibrato"> Should we use vibrato to play unplayable notes?.</param>
        /// <param name="transposeNotes"> Should we transpose unplayable notes?.</param>
        public static void PlayNote(int noteId, bool enableVibrato, bool transposeNotes)
        {
            var lyreNote = lyreNotes[noteId];

            KeyTap(lyreNote);
        }

        /// <summary>
        /// Tap a key.
        /// </summary>
        /// <param name="key"> The key to be tapped.</param>
        public static void KeyTap(Keys key)
        {
            Keyboard.KeyDown(key);
            Keyboard.KeyUp(key);
        }

        /// <summary>
        /// Hold key for certain amount of time and release. (UNTESTED)
        /// </summary>
        /// <param name="key"> The key to be held.</param>
        /// <param name="time"> The amount of time the key should be held for.</param>
        public static void KeyHold(Keys key, TimeSpan time)
        {
            Keyboard.KeyDown(key);
            new Timer(state => Keyboard.KeyUp(key), null, time, Timeout.InfiniteTimeSpan);
        }

        public static bool OnSongPlay()
        {
            genshinWindow = FindWindow("Genshin Impact");
            //BringWindowToFront(genshinWindow);
            SwitchToThisWindow(genshinWindow, true);
            var hWnd = GetForegroundWindow();
            if (genshinWindow.Equals(IntPtr.Zero) || !hWnd.Equals(genshinWindow)) return false;
            return true;
        }

        public static bool IsWindowFocused(IntPtr windowPtr)
        {
            var hWnd = GetForegroundWindow();
            if (windowPtr.Equals(IntPtr.Zero) || !hWnd.Equals(windowPtr)) return false;
            return true;
        }

        public static bool IsWindowFocused(string windowName)
        {
            var windowPtr = FindWindow(windowName);
            return IsWindowFocused(windowPtr);
        }

    }
}
