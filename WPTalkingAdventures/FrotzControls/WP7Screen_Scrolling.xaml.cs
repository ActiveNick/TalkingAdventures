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
using Windows.Phone.Speech.Synthesis;
using PortableFrotz.Screen;
using Windows.Phone.Speech.Recognition;
using Windows.Foundation;

namespace WPTalkingAdventures.FrotzControls
{
    public partial class WP7Screen_Scrolling : BaseWP7Screen, Frotz.Screen.IZScreen
    {
        private const int MAX_HEIGHT = 2048;

        AppSettings settings = AppSettings.Default;

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

        bool focusOnStory = true;

        // SPEECH SYNTHESIS AND RECOGNITION DECLARATIONS

        SpeechSynthesizer synth = new SpeechSynthesizer();

        // Declare the SpeechRecognizer object at the class level.
        SpeechRecognizer myRecognizer;

        IAsyncOperation<SpeechRecognitionResult> recoOperation;

        bool recoEnabled = false;
        
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

        static WP7Screen_Scrolling()
        {
            Frotz.os_.ReadLineStarted += new EventHandler(os__ReadLineStarted);
        }

        public WP7Screen_Scrolling(int FontSize, int FontHeight, int FontWidth)
        {
            InitializeComponent();

            // TODO Determine font info at runtime
            fontHeight = FontHeight;
            fontWidth = FontWidth;

            if (AppSettings.Default.AutoFocusInput == true)
            {
                focusOnStory = false;
            }

            if (settings.ShowAutocorrect == true)
            {
                tbInput.InputScope = new System.Windows.Input.InputScope()
                {
                    Names = { new InputScopeName() { NameValue = InputScopeNameValue.Text } }
                };
            }

            _currentRun = new Run();
            tbHeader.Inlines.Add(_currentRun);

            tbHeader.FontSize = FontSize;

            this.SizeChanged += new SizeChangedEventHandler(WP7Screen_SizeChanged);

            saveSelection.SaveFileSelected += new RoutedEventHandler(saveSelection_SaveSelectionMade);
            restoreSelection.RestoreFileSelected += new RoutedEventHandler(restoreSelection_LoadFileSelected);

            App.Screen = this;
#if COLOR
            backColor = new SolidColorBrush(SzurgotUtilities.WP7.PhoneTheme.Current.PhoneBackgroundColor);
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
#else

            backColor = new SolidColorBrush(AppSettings.Default.Theme.Background);
            LayoutRoot.Background = backColor;

            foreColor = new SolidColorBrush(AppSettings.Default.Theme.Foreground);
            tbHeader.Foreground = backColor;
            bHeader.Background = foreColor;
            cursorColor = new SolidColorBrush(AppSettings.Default.Theme.Input);

//            this.Background = backColor;
            spStory.Background = backColor;
            
#if TEMP
            gInput.Background = backColor;
            tbInput.Foreground = new SolidColorBrush(Colors.Black);
            tbInput.Background = new SolidColorBrush(Colors.White);
            tbInput.BorderBrush = foreColor;
            tbInput.BorderThickness = new Thickness(2);

            btnClearInput.Foreground = foreColor;
            // tbInput.Background = foreColor;
#endif

#endif
            setFocus();

            if (AppSettings.Default.InputBottom == true)
            {
                gInput.SetValue(Grid.RowProperty, 3);
            }

            SetLock();

            // Initialize the SpeechRecognizer
            myRecognizer = new SpeechRecognizer();

            // Allow for 8 seconds of silence to let the user think, no pressure
            myRecognizer.Settings.InitialSilenceTimeout = TimeSpan.FromSeconds(8);

            // Allow for non-speech input such as background noise
            myRecognizer.Settings.BabbleTimeout = TimeSpan.FromSeconds(8);

            // Set EndSilenceTimeout to give users more time to complete speaking a phrase.
            myRecognizer.Settings.EndSilenceTimeout = TimeSpan.FromSeconds(1.2);
            
            Uri actionList = new Uri("ms-appx:///Grammars/TextAdventure.grxml", UriKind.Absolute);
            myRecognizer.Grammars.AddGrammarFromUri("srgsActions", actionList);

            // Subscribe to the AudioCaptureStateChanged event.
            myRecognizer.AudioCaptureStateChanged += myRecognizer_AudioCaptureStateChanged;

            myRecognizer.AudioProblemOccurred += myRecognizer_AudioProblemOccurred;

            // Pre-load the grammar to save time while the initial text is read out loud
            myRecognizer.PreloadGrammarsAsync();

            synth.SpeakTextAsync("Speech recognition is now active.");
        }

        private void setFocus()
        {
            if (focusOnStory)
            {
                svStory.Focus();
            }
            else
            {
                tbInput.Focus();
            }
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
            throw new Exception("Bailing on Z-Machine:" + Message);
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

                if (InputMode == false)
                {
                    setFocus();
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

        private void btnStartReco_Click(object sender, RoutedEventArgs e)
        {
            synth.SpeakTextAsync("Speech recognition is now active.");
            PerformRecognition();
        }

        private async void PerformRecognition()
        {
            // Change the button text. 
            if (this.recoEnabled)
            {
                this.recoOperation.Cancel();
            }
            else
            {
                this.recoEnabled = true;
            }

            while (this.recoEnabled)
            {
                try
                {
                    // Start recognition.
                    this.recoOperation = myRecognizer.RecognizeAsync();
                    var recoResult = await this.recoOperation;

                    // Check to see if speech input was rejected and prompt the user.
                    if (recoResult.TextConfidence == SpeechRecognitionConfidence.Rejected)
                    {
                        //displayText.Text = "Sorry, didn't catch that. \n\nSay again.";
                    }

                    // Check to see if speech input was recognized with low confidence and prompt the user to speak again.
                    else if (recoResult.TextConfidence == SpeechRecognitionConfidence.Low)
                    {
                        //displayText.Text = "Not sure what you said. \n\nSay again.";
                    }

                    // Check to see if speech input was recognized and confirm the result.
                    else if (recoResult.TextConfidence == SpeechRecognitionConfidence.High ||
                            recoResult.TextConfidence == SpeechRecognitionConfidence.Medium)
                    {
                        this.recoEnabled = false;

                        if (recoResult.TextConfidence == SpeechRecognitionConfidence.High)
                        {
                            synth.SpeakTextAsync("You said: " + recoResult.Text);
                        }
                        else
                        {
                            synth.SpeakTextAsync("I think you said: " + recoResult.Text);
                        }

                        Dispatcher.BeginInvoke(() =>
                            {
                                foreach (var c1 in recoResult.Text)
                                {
                                    this.SendKeyPress(c1);
                                }
                                this.SendKeyPress((char)13);
                            });
                        break;
                    }
                }
                catch (System.Threading.Tasks.TaskCanceledException)
                {
                    // Ignore the cancellation exception of the recoOperation.
                }
                catch (Exception exc)
                {
                    // Handle the speech privacy policy error.
                    const int privacyPolicyHResult = unchecked((int)0x80045509);

                    if (exc.HResult == privacyPolicyHResult)
                    {
                        MessageBox.Show("You must accept the speech privacy policy to continue.");
                        this.recoEnabled = false;
                        break;
                    }
                    else
                    {
                        string errorMsg = "An error has occurred: \n\n";
                        if (exc.InnerException != null)
                        {
                            errorMsg += exc.InnerException.Message;
                            synth.SpeakTextAsync(exc.InnerException.Message);
                        }
                        else
                        {
                            errorMsg += exc.Message;
                            synth.SpeakTextAsync(exc.Message);
                        }
                        errorMsg += "\n\nRecognition stopped.";

                        MessageBox.Show(errorMsg);
                        this.recoEnabled = false;
                        break;
                    }
                }
            }
        }

        // Detect capture state changes and write the capture state to the text block.
        void myRecognizer_AudioCaptureStateChanged(SpeechRecognizer sender, SpeechRecognizerAudioCaptureStateChangedEventArgs args)
        {
            if (args.State == SpeechRecognizerAudioCaptureState.Capturing)
            {
                //this.Dispatcher.BeginInvoke(delegate { statusText.Text = "Listening"; });
            }
            else if (args.State == SpeechRecognizerAudioCaptureState.Inactive)
            {
                //this.Dispatcher.BeginInvoke(delegate { statusText.Text = "Thinking"; });
            }
        }

        void myRecognizer_AudioProblemOccurred(SpeechRecognizer sender, SpeechAudioProblemOccurredEventArgs args)
        {
            // If input speech is too quiet, prompt the user to speak louder.
            if (args.Problem == SpeechRecognitionAudioProblem.TooQuiet)
            {
                //synth.SpeakTextAsync("Try speaking louder");
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

                _storyText.Length = 0;

                if (addNewLine) _storyRuns.Add(
                    new ScrollingText()
                    {
                        Text = "\r\n",
                        FontStyle = 0
                    });
            }
        }

        TextBlock _activeRtb = null;
        Run _cursorRun = null;

        private void sendTextToScreen()
        {
            Dispatcher.BeginInvoke(() =>
            {
                sendRunsToScreen(_storyRuns);
            });
        }

        private async void sendRunsToScreen(List<ScrollingText> runs)
        {
            _scrollbackRuns.Add(new List<ScrollingText>(runs));

            StringBuilder pb = new StringBuilder();

            lock (this)
            {
                if (_cursorRun != null && runs.Count > 0)
                {
                    var st = runs.First();
                    runs.RemoveAt(0);

                    _cursorRun.Text = st.Text;
                    _activeRtb.Inlines.Add(new LineBreak());
                }


                while (runs.Count > 0 && runs[0].Text.Trim() == "")
                {
                    runs.RemoveAt(0);
                }

                while (runs.Count > 0 && runs.Last().Text.Trim() == "")
                {
                    runs.RemoveAt(runs.Count - 1);
                }
                
                double top = spStory.DesiredSize.Height;

                var newRuns = new List<RunAndSize>();

                TextBlock tb = new TextBlock();
                tb.TextWrapping = TextWrapping.Wrap;
                tb.Foreground = foreColor;
                tb.Margin = new Thickness(0);
                tb.Width = 480;

                //if (runs.Count > 0)
                //{
                //    pb.AppendLine("<voice xml:lang=\"en-US\">");
                //}

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

                    Inline r = null;

                    if (st.Text == "\r\n")
                    {
                        {
                            r = new LineBreak();
                        }
                    }
                    else
                    {
                        r = createRun(st);

                        // TOD DO: Provide an option to skip copyrights or not
                        string textline = st.Text.ToLower();
                        if (textline.Contains("trademark") || textline.Contains("copyright") || textline.Contains("serial number"))
                        {
                            // Do nothing, we don't want to say those out loud
                        }
                        else
                        {
                            pb.AppendLine(textline);
                        }
                    }

                    // Measure the inlines and add them to a list
                    var size = MeasureText(r);

                    newRuns.Add(new RunAndSize() { Run = r, Height = size.Height });
                }

                // Take the previous newlines and add them, creating a new block if the current one will make it too long
                double height = MAX_HEIGHT + 1;

                foreach (var ras in newRuns)
                {
                    if (height > 0 && height + ras.Height > MAX_HEIGHT)
                    {
                        createNewTextBlock();
                        height = 0;
                    }

                    if (ras.Run is LineBreak && _activeRtb.Inlines.Count == 0)
                    {
                        continue;
                    }
                    _activeRtb.Inlines.Add(ras.Run);

                    height += ras.Height;
                }

                _cursorRun = new Run();
                _cursorRun.Foreground = cursorColor;
                _cursorRun.Text = "_";
                _activeRtb.Inlines.Add(_cursorRun);

                svStory.UpdateLayout();

                svStory.ScrollToVerticalOffset(top);
            }

            if (pb.Length > 0)
            {
                //pb.AppendLine("</voice>");

                await synth.SpeakTextAsync(pb.ToString());
            }

            PerformRecognition();
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
            _activeRtb = new TextBlock();
            _activeRtb.TextWrapping = TextWrapping.Wrap;
            _activeRtb.Foreground = foreColor;
            _activeRtb.Margin = new Thickness(0);

            spStory.Children.Add(_activeRtb);

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
        private System.Windows.Size MeasureText(Inline i)
        {
            TextBlock tb = this.GetTextBlockOnly();
            // textBlock.Text = value;
            tb.Inlines.Add(i);
            var s = new System.Windows.Size(tb.ActualWidth, tb.ActualHeight);
            tb.Inlines.Clear();

            return s;
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

        private void btnClearInput_Click(object sender, RoutedEventArgs e)
        {
            tbInput.Text = "";
            tbInput.Focus();
        }
    }

    public class RunAndSize
    {
        public Inline Run { get; set; }
        public double Height { get; set; }
    }
}

// Add in font change support
