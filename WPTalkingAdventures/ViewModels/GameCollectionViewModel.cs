using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;

namespace WPTalkingAdventures
{
    [DataContract]
    public class GameCollectionViewModel : INotifyPropertyChanged
    {
        [DataMember]
        public System.Collections.ObjectModel.ObservableCollection<GameViewModel> Games { get; set; }

        public int IndexOf(GameViewModel gvm)
        {
            return Games.IndexOf(gvm);
        }

        public GameCollectionViewModel()
        {
            Games = new System.Collections.ObjectModel.ObservableCollection<GameViewModel>();
        }

        public void AddGame(String name)
        {
            foreach (var v in Games)
            {
                if (v.FileName == name)
                {
                    addOrUpdateGame(v, name);
                    return;
                }
            }

            var gvm = new GameViewModel()
            {
                Title = name,
                FileName = name,
                Description = "",
                LongDescription = ""
            };
            addOrUpdateGame(gvm, name);

            addSorted(gvm);
        }

        private void addSorted(GameViewModel gvm)
        {

            for (int i = 0; i < Games.Count; i++)
            {
                if (gvm.Title.CompareTo(Games[i].Title) < 0)
                {
                    Games.Insert(i, gvm);
                    return;
                }
            }

            Games.Add(gvm);
        }

        private void addOrUpdateGame(GameViewModel ivm, String name)
        {
            if (name.ToLower().EndsWith(".zblorb"))
            {
                var iso = IsolatedStorageFile.GetUserStoreForApplication();

                var s = iso.OpenFile(Helpers.GetPathForFile(name, name), System.IO.FileMode.Open);
                byte[] buffer = new byte[s.Length];
                s.Read(buffer, 0, buffer.Length);
                var b = Frotz.Blorb.BlorbReader.ReadBlorbFile(buffer);
                s.Close();

                var md = new Frotz.Blorb.BlorbMetadata(b);

                ivm.Title = md.Title;
                ivm.Description = md.Headline;
                ivm.LongDescription = md.Description;

                if (String.IsNullOrEmpty(ivm.Description))
                {
                    ivm.Description = "Interactive Fiction";
                }

                var sb = new System.Text.StringBuilder();
                sb.Append("<body bgcolor=black><font color=white>\r\n");
                if (md.CoverPicture != null)
                {
                    String format = md.PictureInfo.Format;
                    s = iso.OpenFile(Helpers.GetPathForFile(name, "cover." + format), System.IO.FileMode.Create);
                    s.Write(md.CoverPicture, 0, md.CoverPicture.Length);
                    s.Close();
                    sb.Append("<img src=cover." + format + " width=300 align=left><br />\r\n");
                }

                sb.Append(ivm.LongDescription);
                sb.Append("</font></body>");

                s = iso.OpenFile(Helpers.GetPathForFile(name, "about.htm"), System.IO.FileMode.Create);
                var sw = new System.IO.StreamWriter(s);
                sw.Write(sb.ToString());
                sw.Close();
            }
        }

        public void RemoveGame(GameViewModel gvm)
        {
            try
            {
                String path = Helpers.GetGamePath(gvm.FileName);

                var isf = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication();
                foreach (String name in isf.GetFileNames(path + "\\*"))
                {
                    isf.DeleteFile(System.IO.Path.Combine(path, name));
                }

                isf.DeleteDirectory(path);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Removing exception:" + ex);
            }
            Games.Remove(gvm);
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void LoadData()
        {
            // Sample data; replace with real data
            this.Games.Add(new GameViewModel() { Title = "Adventure", FileName = "Advent.z5", Description = "The classic original text adventure that spawned an entire genre." });
            this.Games.Add(new GameViewModel() { Title = "Pirate Adventure", FileName = "PIRATE.z5", Description = "Only by exploring this strange island will you be able to uncover the clues necessary to lead you to your elusive goal -- recovering the lost treasures of Long John Silver." });
            this.Games.Add(new GameViewModel() { Title = "Zork I: The Great Underground Empire", FileName = "ZORK1.Z5", Description = "Discover the 'Great Underground Empire' and find out how gruesome a 'Grue' is!" });
            this.Games.Add(new GameViewModel() { Title = "Zork II: The Wizard of Frobozz", FileName = "ZORK2.Z5", Description = "Enter the realm of the Wizard of Frobozz, in a long hidden region of the Empire. Adventure galore awaits you!" });
            this.Games.Add(new GameViewModel() { Title = "Zork III: The Dungeon Master", FileName = "ZORK3.Z5", Description = "Enter the deepest, most mysterious parts of the realm and meet the Dungeon Master, who possibly holds the solution to all!" });

            this.IsDataLoaded = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}