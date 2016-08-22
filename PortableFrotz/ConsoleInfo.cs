#if REMOVE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.InteropServices;

namespace Frotz
{
    public class ConsoleInfo
    {
        IntPtr hOut;

        public int TextHeight { get; private set; }
        public int TextWidth { get; private set; }

        public ConsoleInfo()
        {
            TextHeight = Console.WindowHeight;
            TextWidth = Console.WindowWidth;

            hOut = GetStdHandle(-11);

            CONSOLE_SCREEN_BUFFER_INFO csbi;
            GetConsoleScreenBufferInfo(hOut, out csbi);
            normal = csbi.wAttributes;
        }

        int normal = -1;

        public void SetInverse()
        {
            setAttr(Attrs.FG_BLUE | Attrs.BG_BLUE | Attrs.BG_GREEN | Attrs.BG_RED | Attrs.BG_INTENSITY);
        }

        public void SetNormal()
        {
            setAttr((Attrs)normal);
        }

        private void setAttr(Attrs wAttributes)
        {
            SetConsoleTextAttribute(hOut, wAttributes);
        }

        public void ClearArea(int left, int top, int right, int bottom) {
            int hWrittenChars = 0;
            CONSOLE_SCREEN_BUFFER_INFO strConsoleInfo = new CONSOLE_SCREEN_BUFFER_INFO();
            COORD Home;
            GetConsoleScreenBufferInfo(hOut, out strConsoleInfo);

            for (int i = top; i <= bottom; i++) {
                Home.X = (short)left;
                Home.Y = (short)i;
                FillConsoleOutputCharacter(hOut, EMPTY,
                    right - left + 1, Home, ref hWrittenChars);
            }
        }

        private const byte EMPTY = 32;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetConsoleTextAttribute(
        IntPtr hConsoleOutput,
        Attrs wAttributes);
        /* declaring the setconsoletextattribute function*/
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetStdHandle(int nStdHandle);
        /*declaring the getstdhandle funtion to get thehandle that would be used in 
        SetConsoletextattribute function */

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput,
           out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        [DllImport("kernel32.dll", EntryPoint = "FillConsoleOutputCharacter", 
            SetLastError = true, 
            CharSet = CharSet.Auto, 
            CallingConvention = CallingConvention.StdCall)]
        private static extern int FillConsoleOutputCharacter(IntPtr hConsoleOutput, 
            byte cCharacter, int nLength, COORD dwWriteCoord, ref int lpNumberOfCharsWritten);

        internal enum Attrs
        {
            FG_BLUE = 0x0001,
            FG_GREEN = 0x0002,
            FG_RED = 0x0004,
            FG_INTENSITY = 0x0008,
            BG_BLUE = 0x0010,
            BG_GREEN = 0x0020,
            BG_RED = 0x0040,
            BG_INTENSITY = 0x0080,
            COMMON_LVB_LEADING_BYTE = 0x0100,
            COMMON_LVB_TRAILING_BYTE = 0x0200,
            COMMON_LVB_GRID_HORIZONTAL = 0x0400,
            COMMON_LVB_GRID_LVERTICAL = 0x0800,
            COMMON_LVB_GRID_RVERTICAL = 0x1000,
            COMMON_LVB_REVERSE_VIDEO = 0x4000,
            COMMON_LVB_UNDERSCORE = 0x8000
        }


        internal struct COORD
        {
            public short X;
            public short Y;
        }

        internal struct SMALL_RECT
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        internal struct CONSOLE_SCREEN_BUFFER_INFO
        {

            public COORD dwSize;
            public COORD dwCursorPosition;
            public short wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;
        }
    }
}
#endif