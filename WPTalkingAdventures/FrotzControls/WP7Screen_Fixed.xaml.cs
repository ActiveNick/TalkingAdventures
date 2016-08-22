// The On-Screen Keyboard is 336 pixels tall in portrait view and 256 pixels tall in either landscape view. 
// The text suggestion window is 65 pixels tall in both screen orientations.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using System.Text;
using System.Threading;
using System.IO.IsolatedStorage;

namespace WPTalkingAdventures.FrotzControls
{
    public partial class WP7Screen_Fixed : BaseWP7Screen, Frotz.Screen.IZScreen
    {
        AppSettings settings = AppSettings.Default;

        private bool _cursorVisibility = false;

        string gameFile = "";
        static string fullGamePath = "";

        private Frotz.Screen.ScreenMetrics _metrics;

        StringBuilder tbCursorText = new StringBuilder();

        StringBuilder _scrollback = new StringBuilder();



        protected new int ScreenX
        {
            get { return base.ScreenX; }
            set
            {
                updateCursorPane(' ');

                base.ScreenX = value;
            }
        }

        protected new int ScreenY
        {
            get
            {
                return base.ScreenY;
            }
            set
            {
                updateCursorPane(' ');

                base.ScreenY = value;
            }
        }

        static WP7Screen_Fixed()
        {
            Frotz.os_.ReadLineStarted += new EventHandler(os__ReadLineStarted);
        }

        static void os__ReadLineStarted(object sender, EventArgs e)
        {
            Frotz.SaveMachineState.SaveState(fullGamePath);
        }

        public WP7Screen_Fixed(int FontSize, int FontHeight, int FontWidth)
        {
            InitializeComponent();

            // TODO Determine font info at runtime
            fontHeight = FontHeight;
            fontWidth = FontWidth;

            _scrollback = new StringBuilder();

            if (settings.UseSIP)
            {
                int showAutoCorrect = 0;
                if (settings.ShowAutocorrect == true)
                {
                    tbInput.InputScope = new System.Windows.Input.InputScope()
                    {
                        Names = { new InputScopeName() { NameValue = InputScopeNameValue.Text } }
                    };


                    showAutoCorrect = 50;
                }
                gInput.Height = new GridLength(0);
                if (settings.OrientationLandscape == true)
                {
                    gBlank.Height = new GridLength(256 + showAutoCorrect);
                }
                else
                {
                    // gBlank.Height = new GridLength(336);
                    gBlank.Height = new GridLength(336 + showAutoCorrect);
                }
            }

            _currentRun = new Run();
            tbStory.Inlines.Add(_currentRun);

            tbStory.FontSize = FontSize;
            tbCursor.FontSize = FontSize;

            tbScrollback.FontSize = FontSize;

            this.SizeChanged += new SizeChangedEventHandler(WP7Screen_SizeChanged);

            if (settings.UseSIP)
            {
                tbInput.Focus();
            }

            this.MouseLeftButtonDown += new MouseButtonEventHandler(WP7Screen_MouseLeftButtonDown);

            saveSelection.SaveFileSelected += new RoutedEventHandler(saveSelection_SaveSelectionMade);
            restoreSelection.RestoreFileSelected += new RoutedEventHandler(restoreSelection_LoadFileSelected);

            App.Screen = this;

            tbInput.GotFocus += new RoutedEventHandler(tbInput_GotFocus);
            tbInput.LostFocus += new RoutedEventHandler(tbInput_LostFocus);

            tbCursor.Visibility = System.Windows.Visibility.Collapsed;

            // TO DO: Set the color to the actual phone background
            //LayoutRoot.Background = new SolidColorBrush(SzurgotUtilities.WP7.PhoneTheme.Current.PhoneBackgroundColor);
            LayoutRoot.Background = new SolidColorBrush(Colors.Black);
            
            if (Helpers.DarkTheme)
            {
                tbStory.Foreground = new SolidColorBrush(Color.FromArgb(255, 173, 216, 230));
            }
            else
            {
                tbStory.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            }

            tbCursor.Foreground = tbStory.Foreground;

            System.Diagnostics.Debug.WriteLine("Color:" + tbStory.Foreground);
        }

        void tbInput_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCursor.Visibility = System.Windows.Visibility.Collapsed;
        }

        void tbInput_GotFocus(object sender, RoutedEventArgs e)
        {
            tbCursor.Visibility = System.Windows.Visibility.Visible;
        }

        void restoreSelection_LoadFileSelected(object sender, RoutedEventArgs e)
        {
            SendPulse();
        }

        void saveSelection_SaveSelectionMade(object sender, RoutedEventArgs e)
        {
            SendPulse();
        }

        private void GetInfoForTombstoning(out int X, out int Y, out List<StringBuilder> Lines)
        {
            Lines = _lines;
            X = ScreenX;
            Y = ScreenY;
        }

        void WP7Screen_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (settings.UseSIP)
            {
                tbInput.Focus();
            }
        }

        void WP7Screen_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            height = (int)gStory.ActualHeight;
            width = (int)e.NewSize.Width;

            SendPulse();
        }

        public override void addInputChar(char c)
        {
            DisplayChar(c);
        }

        public override void DisplayChar(char c)
        {
            if (_activeWindow == 0)
            {
                _scrollback.Append(c);
            }

            StringBuilder sb = _lines[ScreenY];
            if (sb.Length < ScreenX + 1)
            {
                sb.Append(new String(' ', (ScreenX + 1) - sb.Length));
            }
            sb[ScreenX++] = c;
            if (inInputMode || c == ']') // Handle when [more] is displayed
            {
                updateDisplay();
            }
        }

        public override void RefreshScreen()
        {
            updateDisplay();
        }

        private void updateDisplay()
        {
            String text = "";
            lock (_lines)
            {
                foreach (var sb1 in _lines)
                {
                    text += sb1.ToString() + "\r\n";
                }
            }
            Dispatcher.BeginInvoke(() =>
            {
                tbStory.Text = text;
                tbCursor.Text = tbCursorText.ToString();
            });
        }

        public override Frotz.Screen.ScreenMetrics GetScreenMetrics()
        {
            WaitForLock();
            int rows = (int)(height / fontHeight);
            int cols = (int)(width / fontWidth);

            _lines = new List<StringBuilder>();
            tbCursorText = new StringBuilder();

            for (int i = 0; i < rows; i++)
            {
                _lines.Add(new StringBuilder());
                tbCursorText.Append(new String(' ', cols + 1) + "\r\n");
            }

            _metrics = new Frotz.Screen.ScreenMetrics(new Frotz.Screen.ZSize(fontHeight, fontWidth),
                new Frotz.Screen.ZSize(rows * fontHeight, cols * fontWidth),
                rows, cols, 1);

            return _metrics;
        }

        public override void HandleFatalError(string Message)
        {
            //Dispatcher.BeginInvoke(() =>
            //{
            //    MessageBox.Show("Fatal error: " + Message);
            //}
            //);

            throw new Exception("Bailing on Z-Machine:" + Message);
            // TODO Give up and return to source
        }

        public override void SetInputMode(bool InputMode, bool CursorVisibility, bool SingleKey)
        {
            if (_cursorVisibility == true)
            {
                updateCursorPane(' ');
            }

            _cursorVisibility = CursorVisibility;
            inInputMode = InputMode;
            Dispatcher.BeginInvoke(() =>
            {
                _currentRun = new Run();
                tbStory.Inlines.Add(_currentRun);
            });

            if (_cursorVisibility == true)
            {
                updateCursorPane('_');
            }

            updateDisplay();
        }

        public override void SetInputColor()
        {
            // throw new NotImplementedException();
        }

        public override void RemoveChars(int count)
        {
            StringBuilder sb = _lines[ScreenY];
            for (int i = 0; i < count; i++)
            {
                if (inInputMode)
                {
                    sb[ScreenX + 1] = ' ';
                    ScreenX--;
                }
                else
                {
                    sb[ScreenX - 1] = ' ';
                    ScreenX--;
                }
            }

            updateDisplay();
        }

        private void tbStory_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            tbInput.Focus();
        }

        public override System.IO.Stream OpenNewOrExistingFile(string defaultName, string Title, string Filter, string defaultExtension)
        {
            Dispatcher.BeginInvoke(() =>
            {
                saveSelection.Visibility = System.Windows.Visibility.Visible;
            });

            WaitForLock();

            Dispatcher.BeginInvoke(() =>
            {
                saveSelection.Visibility = Visibility.Collapsed;
            });

            if (String.IsNullOrEmpty(saveSelection.SaveName))
            {
                return null;
            }

            String name = saveSelection.SaveName;
            name = name + ".sav";

            name = Helpers.GetPathForFile(gameFile, name);

            var isf = IsolatedStorageFile.GetUserStoreForApplication();
            IsolatedStorageFileStream isfs = isf.OpenFile(name, System.IO.FileMode.OpenOrCreate);
            return isfs;
        }

        public override System.IO.Stream OpenExistingFile(string defaultName, string Title, string Filter)
        {
            Dispatcher.BeginInvoke(() =>
            {
                restoreSelection.populateList(gameFile);
                restoreSelection.Visibility = System.Windows.Visibility.Visible;
            });

            WaitForLock();

            Dispatcher.BeginInvoke(() =>
            {
                restoreSelection.Visibility = Visibility.Collapsed;
            });

            if (String.IsNullOrEmpty(restoreSelection.RestoreName)) return null;

            String name = restoreSelection.RestoreName;
            name = Helpers.GetPathForFile(gameFile, name + ".sav");

            var isf = IsolatedStorageFile.GetUserStoreForApplication();
            if (!isf.FileExists(name)) return null;

            IsolatedStorageFileStream isfs = isf.OpenFile(name, System.IO.FileMode.Open);
            return isfs;
        }

        public override void StoryStarted(string StoryName, Frotz.Blorb.BlorbFile BlorbFile, int Version)
        {
            gameFile = StoryName.Substring(StoryName.LastIndexOf("\\") + 1);
            fullGamePath = StoryName;

            if (Version == 6)
                throw new GameNotSupportedException("This interpreter currently doesn't support V6 games");

            try
            {
                var sms = Frotz.SaveMachineState.attemptRestore();
                if (sms != null)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        int x = sms.X;
                        int y = sms.Y;
                        String[] lines = sms.Lines;

                        for (int i = 0; i < lines.Length; i++)
                        {
                            _lines[i] = new StringBuilder(lines[i]);
                        }

                        ScreenX = x;
                        ScreenY = y;

                        cursorX = x;
                        cursorY = y;

                        updateDisplay();
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("EX while restoring");
                    throw ex;
                });
            }
        }

        public override void SetCursorPosition(int x, int y)
        {
            updateCursorPane(' ');
            if (this.cursorY != y)
            {
                updateDisplay();
            }

            if (_activeWindow == 0 && y != cursorY)
            {
                _scrollback.Append("\r\n");
            }

            base.SetCursorPosition(x, y);
            updateCursorPane('_');
        }


        Frotz.Screen.ZPoint lastCursorPosition = new Frotz.Screen.ZPoint(0, 0);

        private void updateCursorPane(char c)
        {
            int x = ScreenX;
            int y = ScreenY;

            if (_cursorVisibility)
            {
                updateCursorPane(lastCursorPosition.X, lastCursorPosition.Y, ' ');

                updateCursorPane(x, y, '_');

                lastCursorPosition = new Frotz.Screen.ZPoint(x, y);
            }
        }

        private void updateCursorPane(int x, int y, char c)
        {

            int pos = (y * (_metrics.Columns + 3)) + x + 1;
            try
            {
                tbCursorText[pos] = c;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Issue at cursorPane:" + ex.Message + ":" + x + ":" + y);
            }
        }

        public override void ClearArea(int top, int left, int bottom, int right)
        {
            _scrollback.Append("\r\n");

            int startingX = this.ScreenX;
            int startingY = this.ScreenY;

            int b = bottom / fontHeight;
            int r = right / fontWidth;

            for (int y = top - 1; y < b; y++)
            {
                for (int x = left - 1; x < r; x++)
                {
                    this.ScreenX = x;
                    this.ScreenY = y;

                    DisplayChar(' ');
                }
            }
        }

        private void tbInput_TextChanged(object sender, TextChangedEventArgs e)
        {

            lock (this)
            {
                tbInput.TextChanged -= new TextChangedEventHandler(tbInput_TextChanged);

                if (tbInput.Text.Length > 0)
                {
                    char c = tbInput.Text[0];
                    this.SendKeyPress(c);

                    tbInput.Text = "";
                }
                
                tbInput.TextChanged += new TextChangedEventHandler(tbInput_TextChanged);
            }
        }

        private void tbInput_KeyDown(object sender, KeyEventArgs e)
        {
            char c = (char)e.PlatformKeyCode;
            if (c == 10) c = (char)13;

            switch (c)
            {
                case (char)8:
                case (char)13:
                    this.SendKeyPress(c);
                    e.Handled = true;
                    break;
            }
        }

        private void LayoutRoot_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (e.TotalManipulation.Translation.X > 100)
            {
                tbScrollback.Text = _scrollback.ToString();
                tbInput.Visibility = System.Windows.Visibility.Collapsed;
                tbScrollback.Visibility = Visibility.Visible;

                daExpand.From = tbScrollback.ActualWidth;
                daExpand.To = this.ActualWidth;
                daExpand.Duration = new Duration(new TimeSpan(0, 0, 0, 0, 500));

                expandViewer.Begin();
            }

            if (e.TotalManipulation.Translation.X < -100)
            {
                tbInput.Visibility = Visibility.Visible;
                daCollapse.From = tbScrollback.ActualWidth;
                daCollapse.To = 5;
                daCollapse.Duration = new Duration(new TimeSpan(0, 0, 0, 0, 500));

                collapseViewer.Begin();
            }
        }

        public override void ScrollLines(int top, int height, int lines)
        {
            base.ScrollLines(top, height, lines);

            _scrollback.Append("\r\n");
        }

        private void daCollapse_Completed(object sender, EventArgs e)
        {
            tbScrollback.Visibility = System.Windows.Visibility.Collapsed;

            tbInput.Focus();
        }


        public override Frotz.SaveMachineState SaveGameState()
        {
            var state = Frotz.SaveMachineState.GetState();
            if (state == null) return null;

            int x;
            int y;
            List<System.Text.StringBuilder> lines;
            GetInfoForTombstoning(out x, out y, out lines);

            state.X = x;
            state.Y = y;

            String[] linesAsText = new string[lines.Count];

            for (int i = 0; i < lines.Count; i++)
            {
                linesAsText[i] = lines[i].ToString();
            }
            state.Lines = linesAsText;

            return state;
        }
    }
}