using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO.IsolatedStorage;

namespace WPTalkingAdventures
{
    public class SharedScreen : UserControl
    {
        protected int cursorX = 0;
        protected int cursorY = 0;

        private int _screenX = 0;
        protected int ScreenX
        {
            get { return _screenX; }
            set { _screenX = value; }
        }

        private int _screenY = 0;
        protected int ScreenY
        {
            get { return _screenY; }
            set { _screenY = value; }
        }

        protected Run _currentRun;

        protected bool inInputMode = false;

        protected int fontWidth = 8;
        protected int fontHeight = 16;

        protected int height;
        protected int width;

        protected int _activeWindow = 0;

        public String FileName
        {
            get;
            set;
        }

        public byte[] FileData
        {
            get;
            set;
        }

        protected List<StringBuilder> _lines;

        private ManualResetEvent _mre = new ManualResetEvent(false);

        protected void SetLock()
        {
            _mre.Reset();
        }

        protected void WaitForLock()
        {
            _mre.WaitOne();
            _mre.Reset();
        }

        protected void SendPulse()
        {
            _mre.Set();
        }


        public string SelectGameFile(String fileName, out byte[] filedata)
        {
            if (FileData != null)
            {
                filedata = FileData;
                return FileName;
            }
            else
            {
                filedata = null;
                return null;
            }
        }

        public Frotz.Screen.ZSize GetImageInfo(byte[] Image)
        {
            // throw new NotImplementedException("GetImageInfo Not Implemented");
            return null;
        }

        public void ScrollArea(int top, int bottom, int left, int right, int units)
        {
            throw new NotImplementedException("ScrollArea Not Implemented");
        }

        public void DrawPicture(int picture, byte[] Image, int y, int x)
        {
            //' throw new NotImplementedException("DrawPicture Not Implemented");
        }

        public virtual void SetFont(int font)
        {
            // throw new NotImplementedException();
        }

        public void DisplayMessage(string Message, string Caption)
        {
            throw new NotImplementedException("DisplayMessage Not Implemented");
        }

        protected Size sizeChangedSize = Size.Empty;

        protected Dictionary<String, int> sizes = new Dictionary<string, int>();

        public int GetStringWidth(string stringToCheck, Frotz.Screen.CharDisplayInfo Font)
        {
            if (stringToCheck == "") return 0;

            if (!sizes.ContainsKey(stringToCheck))
            {
                sizes[stringToCheck] = stringToCheck.Length * fontWidth;
            }
            return sizes[stringToCheck];
        }



        public virtual void SetActiveWindow(int win)
        {
            _activeWindow = win;
        }

        public virtual void SetWindowSize(int win, int top, int left, int height, int width)
        {
        }

        public virtual bool ShouldWrap()
        {
            return true;
        }

        public bool GetFontData(int font, ref ushort height, ref ushort width)
        {
            height = (ushort)fontHeight;
            width = (ushort)fontWidth;
            return true;
        }

        public void GetColor(out int foreground, out int background)
        {
            foreground = 1;
            background = 1;
        }

        public void SetColor(int new_foreground, int new_background)
        {
        }

        public ushort PeekColor()
        {
            throw new NotImplementedException("PeekColor Not Implemented");
        }

        public void FinishWithSample(int number)
        {
            //' throw new NotImplementedException("FinishWithSample Not Implemented");
        }

        public void PrepareSample(int number)
        {
            //' throw new NotImplementedException("PrepareSample Not Implemented");
        }

        public void StartSample(int number, int volume, int repeats, ushort eos)
        {
            //' throw new NotImplementedException("StartSample Not Implemented");
        }

        public void StopSample(int number)
        {
            //' throw new NotImplementedException("StopSample Not Implemented");
        }

        public virtual void RefreshScreen()
        {
        }

        public virtual void SetCursorPosition(int x, int y)
        {
            cursorX = x;
            _screenX = (int)(x / fontWidth);
            if (y != cursorY)
            {
                cursorY = y;
                _screenY = (int)(y / fontHeight);
            }
        }

        public virtual void ScrollLines(int top, int height, int lines)
        {
            int l = (int)(lines / fontHeight);
            int t = (int)(top / fontHeight);

            lock (_lines)
            {
                for (int i = 0; i < l; i++)
                {
                    if (_lines.Count > t) _lines.RemoveAt(t);
                    _lines.Add(new StringBuilder());
                }
            }
        }

        public event EventHandler<Frotz.Screen.ZKeyPressEventArgs> KeyPressed;

        public virtual void SetTextStyle(int new_style)
        {
            // throw new NotImplementedException();
        }

        public void Clear()
        {
            foreach (var sb in _lines)
            {
                sb.Length = 0;
            }
        }

        public virtual void ClearArea(int top, int left, int bottom, int right)
        {

            throw new NotImplementedException("ClearArea Not Implemented");
        }

        public virtual System.IO.Stream OpenExistingFile(string defaultName, string Title, string Filter)
        {
            var isf = IsolatedStorageFile.GetUserStoreForApplication();
            if (isf.FileExists(defaultName))
            {
                IsolatedStorageFileStream isfs = isf.OpenFile(defaultName, System.IO.FileMode.Open);
                return isfs;
            }
            else
            {
                return null;
            }
        }

        public virtual System.IO.Stream OpenNewOrExistingFile(string defaultName, string Title, string Filter, string defaultExtension)
        {
            var isf = IsolatedStorageFile.GetUserStoreForApplication();
            IsolatedStorageFileStream isfs = isf.OpenFile(defaultName, System.IO.FileMode.OpenOrCreate);
            return isfs;
        }

        public virtual void StoryStarted(string StoryName, Frotz.Blorb.BlorbFile BlorbFile, int Version)
        {

        }

        public Frotz.Screen.ZPoint GetCursorPosition()
        {
            return new Frotz.Screen.ZPoint(cursorX, cursorY);
        }

        internal void SendKeyPress(char c)
        {
            if (KeyPressed != null)
            {
                KeyPressed(this, new Frotz.Screen.ZKeyPressEventArgs(c));
            }
        }
    }
}
