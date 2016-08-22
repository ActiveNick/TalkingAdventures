using System;
using System.IO;
using System.Windows.Media;
using System.Windows;
using Microsoft.Phone.Controls;

namespace WPTalkingAdventures
{
    public static class Helpers
    {
        public static String GetFileNameWithoutExt(String FileName)
        {
            String name = GetFileName(FileName);
            int index = name.LastIndexOf(".");
            if (index != -1)
            {
                name = name.Substring(0, index);
            }

            return name;
        }

        public static String GetFileName(String FileName)
        {
            String name = TrimToChar(FileName, '\\');
            name = TrimToChar(name, '/');

            return name;
        }

        private static String TrimToChar(String s, char c)
        {
            int index = s.LastIndexOf(c);
            if (index != -1)
            {
                return s.Substring(index + 1);
            }
            else
            {
                return s;
            }
        }


        private static String GamesDirectory { get { return AppSettings.FOLDER_LOCALGAMES; } }

        public static String GetGameDirectory()
        {
            return GamesDirectory;
        }

        public static String GetGamePath(String FileName)
        {
            //TO DO: Temporarily disable putting each game in its own folders for debugging
            //String name = FileName;
            //if (name.Contains("\\")) name = name.Substring(name.LastIndexOf("\\") + 1);
            //int index = name.LastIndexOf(".");
            //if (index != -1)
            //{
            //    name = name.Substring(0, index);
            //}
            //name = Path.Combine(GamesDirectory, name);

            //return name;
            return AppSettings.FOLDER_LOCALGAMES;
        }

        public static String GetPathForFile(String GameName, String FileName)
        {
            String name = GetGamePath(GameName);
            name = Path.Combine(name, FileName);

            return name;
        }

        public static bool DarkTheme
        {
            get
            {
                //if (SzurgotUtilities.WP7.PhoneTheme.Current.PhoneBackgroundColor != null)
                //{
                //    Color c = SzurgotUtilities.WP7.PhoneTheme.Current.PhoneBackgroundColor;
                //    if (c.B >= 200)
                //    {
                //        return false;
                //    }
                //    else
                //    {
                //        return true;
                //    }
                //}
                return true;
            }
        }
    }
}
