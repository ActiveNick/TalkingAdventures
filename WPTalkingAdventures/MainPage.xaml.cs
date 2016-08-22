using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPTalkingAdventures.Resources;
using System.IO.IsolatedStorage;
using System.IO;
using Windows.Storage;

namespace WPTalkingAdventures
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            CopyInstalledGamesToLocalStorage();

            // Set the data context of the listbox control to the sample data
            DataContext = AppSettings.Default.GamesList;
        }

        // Load data for the ViewModel Items
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!AppSettings.Default.GamesList.IsDataLoaded)
            {
                AppSettings.Default.GamesList.LoadData();
            }
        }

        protected async void CopyInstalledGamesToLocalStorage()
        {
            try
            {
                Windows.ApplicationModel.Package package = Windows.ApplicationModel.Package.Current;
                StorageFolder installedLocation = package.InstalledLocation;

                StorageFolder installedGamesFolder = await installedLocation.GetFolderAsync(AppSettings.FOLDER_INSTALLEDGAMES);

                // Obtain the virtual store for the application.
                IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();

                // Create a new folder named DataFolder.
                if (!iso.DirectoryExists(AppSettings.FOLDER_LOCALGAMES))
                    iso.CreateDirectory(AppSettings.FOLDER_LOCALGAMES);

                IReadOnlyList<StorageFile> fileList = await installedGamesFolder.GetFilesAsync();

                foreach (StorageFile gamefile in fileList)
                {
                    string filePath = Path.Combine(AppSettings.FOLDER_LOCALGAMES, gamefile.Name);

                    if (!iso.FileExists(filePath))
                    {
                        // Create a stream for the file in the installation folder.
                        using (Stream input = Application.GetResourceStream(new Uri(gamefile.Path, UriKind.Relative)).Stream)
                        {
                            // Create a stream for the new file in the local folder.
                            using (IsolatedStorageFileStream output = iso.CreateFile(filePath))
                            {
                                // Initialize the buffer.
                                byte[] readBuffer = new byte[4096];
                                int bytesRead = -1;

                                // Copy the file from the installation folder to the local folder. 
                                while ((bytesRead = input.Read(readBuffer, 0, readBuffer.Length)) > 0)
                                {
                                    output.Write(readBuffer, 0, bytesRead);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void llsGameList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (llsGameList.SelectedItem == null) return;

            var gameitem = llsGameList.SelectedItem as GameViewModel;

            //var index = AppSettings.Default.GamesList.IndexOf(gameitem);

            // Navigate to the new page
            //NavigationService.Navigate(new Uri("/Pages/DetailsPage.xaml?selectedItem=" + index, UriKind.Relative));

            NavigationService.Navigate(new Uri("/FrotzControls/PlayGame.xaml?index=" + AppSettings.Default.GamesList.IndexOf(gameitem), UriKind.Relative));

            // Reset selected index to -1 (no selection)
            //llsGameList.SelectedIndex = -1;
        }

    }
}