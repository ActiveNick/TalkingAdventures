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
using PortableFrotz.Screen;

namespace WPTalkingAdventures.FrotzControls
{
    public partial class WP7Screen_Mango : BaseWP7Screen, Frotz.Screen.IZScreen
    {
        AppSettings settings = AppSettings.Default;

#if MANGO
        Paragraph _mainParagraph = new Paragraph();
#endif

        string gameFile = "";
        static string fullGamePath = "";

        private Frotz.Screen.ScreenMetrics _metrics;

        StringBuilder _storyText = new StringBuilder();

        public List<ScrollingText> _storyRuns = new List<ScrollingText>();

        public List<List<ScrollingText>> _scrollbackRuns = new List<List<ScrollingText>>();

        bool readSingleKey = false;

        byte _currentStyle = Frotz.Constants.ZStyles.NORMAL_STYLE;

        SolidColorBrush foreColor = null;
        SolidColorBrush backColor = null;
        SolidColorBrush cursorColor = null;

        protected new int ScreenX
        {
            get { return base.ScreenX; }
            set
            {
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
                base.ScreenY = value;
            }
        }

        static void os__ReadLineStarted(object sender, EventArgs e)
        {
            Frotz.SaveMachineState.SaveState(fullGamePath);
        }

        static WP7Screen_Mango()
        {
            Frotz.os_.ReadLineStarted += new EventHandler(os__ReadLineStarted);
        }

        public WP7Screen_Mango(int FontSize, int FontHeight, int FontWidth)
        {
            InitializeComponent();

            // TODO Determine font info at runtime
            fontHeight = FontHeight;
            fontWidth = FontWidth;

#if TEMP
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
            }
#endif
            _currentRun = new Run();
            tbHeader.Inlines.Add(_currentRun);

            tbHeader.FontSize = FontSize;

            this.SizeChanged += new SizeChangedEventHandler(WP7Screen_SizeChanged);

            saveSelection.SaveFileSelected += new RoutedEventHandler(saveSelection_SaveSelectionMade);
            restoreSelection.RestoreFileSelected += new RoutedEventHandler(restoreSelection_LoadFileSelected);

            App.Screen = this;

            // TO DO: Extract the actual phone back color
            backColor = new SolidColorBrush(Colors.Black);
            LayoutRoot.Background = backColor;

            if (Helpers.DarkTheme)
            {
                foreColor = new SolidColorBrush(Color.FromArgb(255, 173, 216, 230));
                tbHeader.Foreground = backColor;
                bHeader.Background = foreColor;
                cursorColor = new SolidColorBrush(Colors.White);
            }
            else
            {
                foreColor = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                tbHeader.Foreground = backColor;
                bHeader.Background = foreColor;

                cursorColor = new SolidColorBrush(Colors.Green);
            }

#if MANGO
            if (!Microsoft.Phone.Info.DeviceStatus.IsKeyboardDeployed)
            {
                svStory.Focus();
            }
#else
            svStory.Focus();
#endif

            if (AppSettings.Default.InputBottom == true)
            {
                tbInput.SetValue(Grid.RowProperty, 3);
            }

            SetLock();
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
            if (_activeWindow == 0) // && !inInputMode)
            {
                _storyText.Append(c);
            }
            else
            {
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
                tbHeader.Text = text;
            });
        }

        public override Frotz.Screen.ScreenMetrics GetScreenMetrics()
        {
            WaitForLock();
            int rows = (int)(height / fontHeight);
            int cols = (int)(width / fontWidth);

            _lines = new List<StringBuilder>();

            for (int i = 0; i < rows; i++)
            {
                _lines.Add(new StringBuilder());
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
            readSingleKey = SingleKey;

            addTextToDisplay(false);

            if (InputMode == true)
            {
                sendTextToScreen();
            }

            inInputMode = InputMode;
            Dispatcher.BeginInvoke(() =>
            {
                _currentRun = new Run();
                tbHeader.Inlines.Add(_currentRun);

                if (InputMode == false
#if MANGO
                && !Microsoft.Phone.Info.DeviceStatus.IsKeyboardDeployed
#endif
)
                {
                    svStory.Focus();
                }
            });

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
                throw new GameNotSupportedException("This interpreter doesn't support V6 games");

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

                        if (sms.ScrollBack != null)
                        {
                            foreach (var run in sms.ScrollBack)
                            {
                                sendRunsToScreen(run);
                            }
                        }
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
            addTextToDisplay((y != this.cursorY));

            if (this.cursorY != y)
            {
                updateDisplay();
            }

            base.SetCursorPosition(x, y);
        }


        Frotz.Screen.ZPoint lastCursorPosition = new Frotz.Screen.ZPoint(0, 0);

        public override void ClearArea(int top, int left, int bottom, int right)
        {
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
                if (readSingleKey == true)
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
        }

        private void tbInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (readSingleKey)
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
            else
            {
                char c = (char)e.PlatformKeyCode;
                if (c == 10 || c == 13)
                {
                    foreach (var c1 in tbInput.Text)
                    {
                        this.SendKeyPress(c1);
                    }
                    this.SendKeyPress((char)13);
                    tbInput.Text = "";
                }
            }
        }

        public override void ScrollLines(int top, int height, int lines)
        {
            base.ScrollLines(top, height, lines);

            this.cursorY = this.cursorY - (height);
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

            state.ScrollBack = _scrollbackRuns;

            return state;
        }

        public override void SetWindowSize(int win, int top, int left, int height, int width)
        {
            if ((win == 1 || win == 7) && height != 0)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    gHeader.Height = new GridLength(height);
                    svStory.InvalidateArrange();
                    svStory.InvalidateScrollInfo();
                });
            }
        }

        private ScrollingText _lastScrollingText = new ScrollingText();
        private String _lastText = null;

        private void addTextToDisplay(bool addNewLine)
        {
            if (_activeWindow == 0 && !inInputMode)
            {
                _lastScrollingText = new ScrollingText()
                    {
                        Text = _storyText.ToString(),
                        FontStyle = _currentStyle
                    };
                _storyRuns.Add(_lastScrollingText);

#if MANGO
                _storyText.Clear();
#else
                _storyText.Length = 0;
#endif

                if (addNewLine) _storyRuns.Add(
                    new ScrollingText()
                    {
                        Text = "\r\n",
                        FontStyle = 0
                    });
            }
        }

#if MANGO
        RichTextBox _activeRtb = null;
        Paragraph _activeParagraph;
#else
        TextBlock _activeRtb = null;
#endif
        Run _cursorRun = null;

        private void sendTextToScreen()
        {
            Dispatcher.BeginInvoke(() =>
            {
                sendRunsToScreen(_storyRuns);
            });
        }

        private void sendRunsToScreen(List<ScrollingText> runs)
        {
            _scrollbackRuns.Add(new List<ScrollingText>(runs));

#if MANGO
            lock (_mainParagraph)
#else
            lock (this)
#endif
            {
                if (_cursorRun != null && runs.Count > 0)
                {
                    var st = runs.First();
                    runs.RemoveAt(0);

                    _cursorRun.Text = st.Text;
#if MANGO
                    _activeParagraph.Inlines.Add(new LineBreak());
#else
                    _activeRtb.Inlines.Add(new LineBreak());
#endif
                }


                while (runs.Count > 0 && runs[0].Text.Trim() == "")
                {
                    runs.RemoveAt(0);
                }

                while (runs.Count > 0 && runs.Last().Text.Trim() == "")
                {
                    runs.RemoveAt(runs.Count - 1);
                }
                
                createNewTextBlock();

                double top = spStory.DesiredSize.Height;

                var newRuns = new List<RunAndSize>();

                while (runs.Count > 0)
                {
                    var st = runs.First();
                    runs.RemoveAt(0);

                    if (st.Text.Length == 0)
                    {
                        if (_lastText == "\r\n") continue;
                        st.Text = "\r\n";
                        continue;
                    }

                    _lastText = st.Text;

                    if (st.Text == "\r\n")
                    {
#if MANGO
                        // if (_activeRtb.Blocks.Count != 0 && runs.Count > 0)
                        if (_activeParagraph.Inlines.Count != 0)
                        {
                            _activeParagraph.Inlines.Add(new LineBreak());
                        }
#else
                        if (_activeRtb.Inlines.Count != 0)
                        {
                            _activeRtb.Inlines.Add(new LineBreak());
                        }
#endif
                        continue;
                    }

                    var r = createRun(st);

#if MANGO
                    _activeParagraph.Inlines.Add(r);

                    _activeRtb.UpdateLayout();

                    System.Diagnostics.Debug.WriteLine("Height:" + _activeRtb.ActualHeight + ":" + r.Text);
                    if (_activeRtb.ActualHeight > 550 && r.Text != ">")
                    {
                        createNewTextBlock();
                    }

                    // TODO Add all the inlines, calculating the height after each one (so the height for each one)
                    // then clear them out and add them so they are just the right height

                    // TODO Does it work to just create and return a TextBlock with text in it? If it does, I'm going to be pissed.
#else
                    System.Diagnostics.Debug.WriteLine("Before:" + _activeRtb.ActualHeight);

                    var size = MeasureText(r.Text);
                    System.Diagnostics.Debug.WriteLine("Size:" + size + ":" + r.Text);

                    _activeRtb.Inlines.Add(r);
                    _activeRtb.InvalidateMeasure();

                    System.Diagnostics.Debug.WriteLine("Height:" + _activeRtb.ActualHeight + ":" + r.Text);

                    if (_activeRtb.ActualHeight > 550)
                    {
                        createNewTextBlock();
                    }

#endif
                }

                _cursorRun = new Run();
                _cursorRun.Foreground = cursorColor;
                _cursorRun.Text = "_";
#if MANGO
                _activeParagraph.Inlines.Add(_cursorRun);
#else
                _activeRtb.Inlines.Add(_cursorRun);
#endif

                svStory.UpdateLayout();

                svStory.ScrollToVerticalOffset(top);
            }
        }

        private Run createRun(ScrollingText st)
        {
            Run r = new Run();
            r.Text = st.Text;

            _lastText = st.Text;

            if (st.FontStyle > 0)
            {
                if (ContainsFlag(st.FontStyle, Frotz.Constants.ZStyles.BOLDFACE_STYLE)) r.FontWeight = FontWeights.Bold;
                if (ContainsFlag(st.FontStyle, Frotz.Constants.ZStyles.EMPHASIS_STYLE)) r.FontStyle = FontStyles.Italic;
                if (ContainsFlag(st.FontStyle, Frotz.Constants.ZStyles.FIXED_WIDTH_STYLE))
                {
                    r.FontFamily = new FontFamily("Courier New");
                    r.FontSize = FontSize;
                }
                if (ContainsFlag(st.FontStyle, Frotz.Constants.ZStyles.REVERSE_STYLE)) r.TextDecorations = TextDecorations.Underline;
            }

            return r;
        }

        private void createNewTextBlock()
        {
#if MANGO
                _activeRtb = new RichTextBox();
#else
            _activeRtb = new TextBlock();
            _activeRtb.TextWrapping = TextWrapping.Wrap;
#endif
            _activeRtb.Foreground = foreColor;
            _activeRtb.Margin = new Thickness(0);

            spStory.Children.Add(_activeRtb);

#if MANGO
            _activeParagraph = new Paragraph();
            _activeRtb.Blocks.Add(_activeParagraph);
#endif
        }

        private bool ContainsFlag(int number, int flag)
        {
            return ((number & flag) > 0);
        }

        public override bool ShouldWrap()
        {
            switch (_activeWindow)
            {
                case 0:
                    return false;
                case 1:
                    return true;
                default:
                    return true;
            }
        }

        public override void SetTextStyle(int new_style)
        {
            if (new_style != _currentStyle && _activeWindow == 0)
            {
                addTextToDisplay(false);
                _currentStyle = (byte)new_style;
            }
        }

        public override void SetActiveWindow(int win)
        {
            if (win != _activeWindow && _activeWindow == 0)
            {
                addTextToDisplay(false);
            }
            base.SetActiveWindow(win);
        }

        public override void Restarting()
        {
            _scrollbackRuns.Clear();
            _storyRuns.Clear();

            Dispatcher.BeginInvoke(() =>
            {
                spStory.Children.Clear();
            });
        }



        // Grabbed from ScrollingTextBlock
        private Size MeasureText(string value)
        {
            TextBlock textBlock = this.GetTextBlockOnly();
            textBlock.Text = value;
            return new Size(textBlock.ActualWidth, textBlock.ActualHeight);
        }

        private TextBlock GetTextBlockOnly()
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Width = 480;
            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.FontSize = this.FontSize;
            textBlock.FontFamily = this.FontFamily;
            textBlock.FontWeight = this.FontWeight;
            return textBlock;
        }
    }
}

// Add in font change support
