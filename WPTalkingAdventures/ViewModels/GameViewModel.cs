using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WPTalkingAdventures
{
    [DataContract]
    public class GameViewModel : INotifyPropertyChanged
    {
        private string _title;
        [DataMember]
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                if (value != _title)
                {
                    _title = value;
                    NotifyPropertyChanged("Title");
                }
            }
        }

        private string _description;
        [DataMember]
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                if (value != _description)
                {
                    _description = value;
                    NotifyPropertyChanged("Description");
                }
            }
        }

        private string _fileName;
        [DataMember]
        public string FileName
        {
            get
            {
                return _fileName;
            }
            set
            {
                if (value != _fileName)
                {
                    _fileName = value;
                    NotifyPropertyChanged("FileName");
                }
            }
        }

        private string _longDescription;
        [DataMember]
        public string LongDescription
        {
            get
            {
                return _longDescription;
            }
            set
            {
                if (value != _longDescription)
                {
                    _longDescription = value;
                    NotifyPropertyChanged("LongDescription");
                }
            }
        }

        private DateTime _lastViewed = new DateTime(2010, 1, 1);
        [DataMember]
        public DateTime LastViewed
        {
            get { return _lastViewed; }
            set
            {
                if (value != _lastViewed)
                {
                    _lastViewed = value;
                    NotifyPropertyChanged("LastViewed");
                }
            }
        }

        private Visibility _showDelete = Visibility.Collapsed;
        public Visibility ShowDelete
        {
            get { return _showDelete; }
            set
            {
                if (value != _showDelete)
                {
                    _showDelete = value;
                    NotifyPropertyChanged("ShowDelete");
                }
            }
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