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
using Microsoft.Phone.Controls;
using System.Threading;
using System.Windows.Navigation;

using WPTalkingAdventures;
using WPTalkingAdventures.FrotzControls;

namespace WPTalkingAdventures.Pages
{
    public partial class PlayGame : BasePage
    {
        BaseWP7Screen _screen;
        String name = null;
        // Thread zThread = null;
        GameViewModel _game;

        public const String saveStateText = "savestate.blob";

        System.ComponentModel.BackgroundWorker zWorker = null;

        public PlayGame()
            : base()
        {
            InitializeComponent();

            tbSize1.FontSize = AppSettings.Default.FixedFontSize;

            this.Loaded += new RoutedEventHandler(PlayGame_Loaded);
        }

        void PlayGame_Loaded(object sender, RoutedEventArgs e)
        {
            Start(name, FileData);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            String indexS;
            if (NavigationContext.QueryString.TryGetValue("index", out indexS))
            {
                _game = AppSettings.Default.GamesList.Games[Convert.ToInt32(indexS)];

                name = Helpers.GetPathForFile(_game.FileName, _game.FileName);

                var isf = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication();
                var s = isf.OpenFile(name, System.IO.FileMode.Open);
                FileData = new byte[s.Length];
                s.Read(FileData, 0, FileData.Length);
                s.Close();

                String temp = name;

                int index = temp.LastIndexOf("\\");
                if (index != -1)
                {
                    temp = temp.Substring(index + 1);
                }
                index = temp.LastIndexOf(".");
                if (index != -1)
                {
                    temp = temp.Substring(0, index);
                }

                ApplicationTitle.Text = "TALKING ADVENTURES - " + temp;
            }

            if (++AppSettings.Default.StartupCount == 10)
            {
                MessageBoxResult mbResult = MessageBox.Show("You have started games over 10 times while in trial mode. Go to the Marketplace to purchase the full product?\r\nCanceling will not stop the game.)",
                "Trial Mode", MessageBoxButton.OKCancel);
                if (mbResult == MessageBoxResult.OK)
                {
                    var detailTask = new Microsoft.Phone.Tasks.MarketplaceDetailTask();
                    detailTask.Show();
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (!e.Uri.ToString().Contains("external"))
            {
                App.Screen = null;
            }
        }

        private void Start(String Name, byte[] FileData)
        {
            int fontHeight = (int)Math.Ceiling(tbSize1.ActualHeight);
            int fontWidth = (int)Math.Ceiling(tbSize1.ActualWidth);

            _screen = new WP7Screen_Scrolling(AppSettings.Default.FixedFontSize, fontHeight, fontWidth);
            //_screen = new WP7Screen_Fixed(AppSettings.Default.FixedFontSize, fontHeight, fontWidth);
            Frotz.os_.SetScreen(_screen);
            _screen.FileName = Name;
            _screen.FileData = FileData;

            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(_screen);
            _screen.Focus();

            // zThread = new Thread(ZMachineThread);
            // zThread.Start();
            zWorker = new System.ComponentModel.BackgroundWorker();
            zWorker.WorkerSupportsCancellation = true;
            zWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(zWorker_DoWork);
            zWorker.RunWorkerAsync();
        }

        void zWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            String error = null;
            try
            {
                Frotz.Generic.main.MainFunc(null);
            }
            catch (GameNotSupportedException gnse)
            {
                error = gnse.Message;
            }
            catch (NotImplementedException ex)
            {
                error = "Not implemented exception:" + ex.Message;
            }
            catch (Exception ex)
            {
                error = "There was an issue with the Z-Machine:" + ex.Message;
            }

            var sc = _screen as WP7Screen_Scrolling;
            if (sc != null)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var s in sc._storyRuns)
                {
                    sb.Append(s.Text);
                }

                String s1 = sb.ToString().Trim();
#if MANGO
                if (!String.IsNullOrWhiteSpace(s1) && s1 != "y" && s1 != "yes")
#else
                if (!String.IsNullOrEmpty(s1) && s1 != "y" && s1 != "yes")
#endif
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        MessageBox.Show(s1, "Final Text", MessageBoxButton.OK);
                    });
                }
            }

            Dispatcher.BeginInvoke(() =>
            {
                if (error != null)
                {
                    MessageBox.Show(error);
                }
                AppSettings.Default.SkipOverDetailPage = true;
                NavigationService.GoBack();
            });
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            //' _screen.SendKeyPress((char)e.PlatformKeyCode);
        }


        public byte[] FileData { get; private set; }
        public String FileName { get; private set; }

        private void loadFromResources(String expectedName)
        {
            var assm = System.Reflection.Assembly.GetExecutingAssembly();
            String[] games = assm.GetManifestResourceNames();
            foreach (String name in games)
            {
                if (name.EndsWith(expectedName, StringComparison.OrdinalIgnoreCase))
                {
                    var s = assm.GetManifestResourceStream(name);
                    FileData = new byte[s.Length];
                    s.Read(FileData, 0, FileData.Length);
                    s.Close();

                    FileName = expectedName;
                    //' OnGameSelected(name, FileData);

                    return;
                }
            }

            FileData = null;
            FileName = null;
        }

        private void BasePage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (AppSettings.Default.SaveStateOnBack == true)
            {
                var sms = _screen.SaveGameState();
                if (sms == null) return;
                var ser = new System.Runtime.Serialization.DataContractSerializer(typeof(Frotz.SaveMachineState));

                // Ensure that required application state is persisted here.
                var iso = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication();

                String name = Helpers.GetPathForFile(_game.FileName, saveStateText);

                var s = iso.CreateFile(name);
                ser.WriteObject(s, sms);
                s.Close();

                e.Cancel = abortGame();
            }
            else
            {
                if (MessageBox.Show("This will exit the current game. Do you wish to return to the game screen?", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    e.Cancel = abortGame();
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        private bool abortGame()
        {
            if (zWorker != null)
            {

                Frotz.Generic.main.abort_game_loop = true;
                // TODO Inject a character into the stream in the event of a [more] prompt
                _screen.SendKeyPress(' ');
                zWorker.CancelAsync();
                zWorker = null;

                return true;
            }
            return false;
        }
    }
}