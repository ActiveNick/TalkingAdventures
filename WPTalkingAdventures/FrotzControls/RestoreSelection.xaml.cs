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

namespace WPTalkingAdventures.FrotzControls
{
    public partial class RestoreSelection : UserControl
    {
        public RestoreSelection()
        {
            InitializeComponent();
        }

        public void populateList(String defaultName)
        {
            var isf = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication();
            var names = isf.GetFileNames(Helpers.GetPathForFile(defaultName, "*.sav")); // TODO Confirm default name is correct here

            lpSaveGames.Items.Clear();

            System.Diagnostics.Debug.WriteLine("Restore name:" + RestoreName);

            bool foundDefault = false;

            foreach (String name in names)
            {
                System.Diagnostics.Debug.WriteLine("Name:" + name);

                if (name.StartsWith("default.sav")) foundDefault = true;
                if (!name.EndsWith(".sav")) continue;

                String temp = Helpers.GetFileNameWithoutExt(name);
                lpSaveGames.Items.Add(temp);
            }

            if (!String.IsNullOrEmpty(RestoreName))
            {
                lpSaveGames.SelectedItem = RestoreName;
            }

            btnDefault.IsEnabled = foundDefault;
        }

        private void btnDefault_Click(object sender, RoutedEventArgs e)
        {
            OnRestoreFileSelected("default");
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            OnRestoreFileSelected(lpSaveGames.SelectedItem.ToString());
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            OnRestoreFileSelected(null);
        }

        public String RestoreName { get; set; }
        public event RoutedEventHandler RestoreFileSelected;
        protected void OnRestoreFileSelected(String Name) {
            RestoreName = Name;
            if (RestoreFileSelected != null) {
                RestoreFileSelected(this, new RoutedEventArgs() );
            }
        }

        private void lpSaveGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnLoad.IsEnabled = true;
        }
    }
}
