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
    public class ThemeColors
    {
        [DataMember]
        public Color Background { get; set; }
        [DataMember]
        public Color Foreground { get; set; }
        [DataMember]
        public Color Input { get; set; }
        [DataMember]
        public String Name { get; set; }

        public ThemeColors(String Name, Color Background, Color Foreground, Color Input)
        {
            this.Name = Name;
            this.Background = Background;
            this.Foreground = Foreground;
            this.Input = Input;
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            if (obj is ThemeColors)
            {
                return this.Name == ((ThemeColors)obj).Name;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public static System.Collections.Generic.List<ThemeColors> DefaultThemes = new System.Collections.Generic.List<ThemeColors>()
        {
            new ThemeColors("Dark", Colors.Black, Colors.White, Colors.LightGray), 
            new ThemeColors("Light", Colors.White, Colors.Black, Colors.DarkGray),
            new ThemeColors("Gray", Colors.LightGray, Colors.Black, Colors.Blue),
            new ThemeColors("Frotz.NET", Colors.Black,
                Color.FromArgb(255, 173, 216, 230),
                Colors.White),
            new ThemeColors("Infocom", 
                Color.FromArgb(255, 1, 1, 128),
                Color.FromArgb(255, 192, 192, 192),
                Colors.White)
        };
    }
}
