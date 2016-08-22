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

namespace WPTalkingAdventures
{
    public class GameNotSupportedException : Exception
    {
        public GameNotSupportedException(String Message) : base(Message) { }
    }

    public class ExitingPhoneAppException : Exception
    {
        public ExitingPhoneAppException() : base() { }
    }
}
