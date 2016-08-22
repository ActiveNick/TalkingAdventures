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
using WPTalkingAdventures;

namespace WPTalkingAdventures.FrotzControls
{
    public abstract class BaseWP7Screen : WPTalkingAdventures.SharedScreen, Frotz.Screen.IZScreen
    {
        public abstract Frotz.SaveMachineState SaveGameState();
        public abstract void DisplayChar(char c);
        public abstract Frotz.Screen.ScreenMetrics GetScreenMetrics();
        public abstract void HandleFatalError(string Message);
        public abstract void RemoveChars(int count);
        public abstract void SetInputColor();
        public abstract void SetInputMode(bool InputMode, bool CursorVisibility, bool SingleKey);
        public abstract void addInputChar(char c);
        public virtual void Restarting() { } 

    }
}
