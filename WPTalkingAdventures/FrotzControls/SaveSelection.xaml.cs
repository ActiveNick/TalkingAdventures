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
    public partial class SaveSelection : UserControl
    {
        public String SaveName { get; set; }

        public SaveSelection()
        {
            InitializeComponent();
        }

        private void btnDefaultSave_Click(object sender, RoutedEventArgs e)
        {
            OnSaveFileSelected("default");
        }

        protected void OnSaveFileSelected(String Name)
        {
            SaveName = Name;
            if (SaveFileSelected != null)
            {
                SaveFileSelected(this, new RoutedEventArgs());
            }
        }

        public event RoutedEventHandler SaveFileSelected;

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            OnSaveFileSelected(tbName.Text);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            OnSaveFileSelected(null);
        }
    }
}
