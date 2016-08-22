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
using Microsoft.Phone.Controls;

namespace WPTalkingAdventures.Pages
{
    public class BasePage : PhoneApplicationPage
    {
        public BasePage()
        {
            try
            {
                AppSettings settings = AppSettings.Default;
                if (settings.OrientationLandscape == true)
                {
                    this.SupportedOrientations = SupportedPageOrientation.Landscape;
                    this.Orientation = PageOrientation.Landscape;
                }
                else
                {
                    this.SupportedOrientations = SupportedPageOrientation.Portrait;
                    this.Orientation = PageOrientation.Portrait;
                }
            }
            catch (Exception)
            {
            }

            this.Loaded += new RoutedEventHandler(BasePage_Loaded);
        }

        void BasePage_Loaded(object sender, RoutedEventArgs e)
        {
            //SzurgotUtilities.WP7.Helpers.SetTransition(this);
        }
    }
}
