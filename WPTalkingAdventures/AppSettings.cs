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
using System.Runtime.Serialization;

namespace WPTalkingAdventures
{
    [DataContract]
    public class AppSettings
    {
        public const string FOLDER_INSTALLEDGAMES = "IFGames";
        public const string FOLDER_LOCALGAMES = "IFGames";

        public const double CurrentVersion = 1.0;

        private static Object lockObject = new object();

        private static AppSettings _default = null;
        public static AppSettings Default
        {
            get
            {
                lock (lockObject)
                {
                    if (_default == null)
                    {
                        try
                        {
                            DataContractSerializer ser = new DataContractSerializer(typeof(AppSettings));

                            var iso = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication();
                            if (iso.FileExists(fileName))
                            {
                                var s = iso.OpenFile(fileName, System.IO.FileMode.Open);
                                var obj = ser.ReadObject(s);
                                s.Close();

                                _default = obj as AppSettings;

                                _default.lastVersion = _default.Version;

                                setDefaults(_default);

                                _default.Version = CurrentVersion;
                            }

                            if (_default != null && _default.GamesList != null && _default.GamesList.Games != null)
                            {
                                // TODO This is an awful HACK and I'd like to know why it's necessary
                                foreach (var g in _default.GamesList.Games)
                                {
                                    g.ShowDelete = Visibility.Collapsed;
                                }
                            }
                        }
                        catch (SerializationException)
                        {
                            // TODO Handle changing of the settings class (It might be handled by default as long as you don't rename the type :) )
                            // throw ex;
                        }
                    }
                    if (_default == null)
                    {
                        _default = new AppSettings();
                        _default.lastVersion = CurrentVersion;
                    }

                    return _default;
                }
            }
        }

        private static void setDefaults(AppSettings _default)
        {
            _default.FixedFontSize = 12;
            _default.StoryFontSize = 16;
            _default.AutoFocusInput = false;
            _default.SaveStateOnBack = true;
            _default.ShowAutocorrect = false;
            _default.InputBottom = false;
            _default.InputTop = true;
            _default.Theme = ThemeColors.DefaultThemes[3]; // Should be Frotz.NET colors
        }

        private const String fileName = "/appsettings.xml";

        public void Save()
        {
            DataContractSerializer ser = new DataContractSerializer(this.GetType());

            var iso = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication();
            var s = iso.CreateFile(fileName);
            ser.WriteObject(s, this);
            s.Close();
        }

        private AppSettings()
        {
            LastUrl = null;
            OrientationLandscape = false;
            OrientationPortrait = true;
            UseSIP = true;
            ShowAutocorrect = true;

            TestOrientation = Microsoft.Phone.Controls.PageOrientation.Portrait;
            OpCodes = new System.Collections.Generic.Dictionary<int, string>();

            SkipOverDetailPage = false;

            GamesList = new GameCollectionViewModel();

            FixedFontSize = 12;
            StoryFontSize = 16;

            InputBottom = false;
            InputTop = true;

            AutoFocusInput = false;

            setDefaults(this);

            Version = CurrentVersion;
        }

        public double lastVersion = 0;

        [DataMember]
        public ThemeColors Theme { get; set; }

        [DataMember]
        public double Version { get; set; }

        [DataMember]
        public int FixedFontSize { get; set; }

        [DataMember]
        public int StoryFontSize { get; set; }

        [DataMember]
        public String LastUrl { get; set; }

        [DataMember]
        public bool InputTop { get; set; }
        [DataMember]
        public bool InputBottom { get; set; }
        
        [DataMember]
        public bool OrientationLandscape { get; set; }
        [DataMember]
        public bool OrientationPortrait { get; set; }
        [DataMember]
        public bool UseSIP { get; set; }
        [DataMember]
        public Microsoft.Phone.Controls.PageOrientation TestOrientation { get; set; }
        [DataMember]
        public System.Collections.Generic.Dictionary<int, String> OpCodes { get; set; }
        [DataMember]
        public int StartupCount { get; set; }
        [DataMember]
        public bool ShowAutocorrect { get; set; }
        [DataMember]
        public bool AutoFocusInput { get; set; }

        [DataMember]
        public GameCollectionViewModel GamesList { get; set; }

        //[DataMember]
        //public SzurgotUtilities.WP7.Downloads.DownloadSettings DownloadSettings { get; set; }

        [DataMember]
        public bool SaveStateOnBack { get; set; }

        public bool SkipOverDetailPage { get; set; }

        //public bool IsTrial { get; set; }

        public Microsoft.Phone.Controls.PageOrientation Orientation
        {
            get
            {
                if (OrientationLandscape == true)
                {
                    return Microsoft.Phone.Controls.PageOrientation.Landscape;
                }
                else
                {
                    return Microsoft.Phone.Controls.PageOrientation.Portrait;
                }
            }
        }
    }
}
