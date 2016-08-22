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
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace WPTalkingAdventures.Pages
{
    public class ParsingTextblock : UserControl
    {
        private TextBlock _tb;

        public ParsingTextblock()
        {
            _tb = new TextBlock();
            this.Content = _tb;
            _tb.TextWrapping = TextWrapping.Wrap;
        }

        private void parseString(String text)
        {
            _tb.Inlines.Clear();
        }

        public void Parse(String line)
        {
            _tb.Inlines.Clear();

            if (line == null) return;

            line = line.Replace("\r", " ");
            line = line.Replace("\n", " ");
            line = line.Replace("\t", " ");

            while (line.IndexOf("  ") != -1)
            {
                line = line.Replace("  ", " ");
            }

            line = line.Replace("&gt;", ">");


            Regex rx = new Regex(@"(\<(.*?)\>)");
            var mc = rx.Matches(line);

            List<String> values = new List<string>();
            int maxIndex = 0;

            foreach (Match m in mc)
            {
                if (m.Index > maxIndex)
                {
                    values.Add(line.Substring(maxIndex, m.Index - maxIndex));
                }

                values.Add(m.Value.ToUpper());
                maxIndex = m.Index + m.Value.Length;
            }

            if (maxIndex < line.Length)
            {
                values.Add(line.Substring(maxIndex));
            }

            foreach (String s in values)
            {
                if (s.StartsWith("<"))
                {
                    switch (s)
                    {
                        case "<BR>":
                        case "<BR />":
                        case "</UL>":
                        case "</DIV>":
                            _tb.Inlines.Add(new LineBreak());
                            _tb.Inlines.Add(new LineBreak());
                            break;
                        case "<I>":
                        case "<EM>":
                            italicLevel++;
                            break;
                        case "</I>":
                        case "</EM>":
                            italicLevel--;
                            break;
                        case "<B>":
                        case "<STRONG>":
                            boldLevel++;
                            break;
                        case "</B>":
                        case "</STRONG>":
                            boldLevel--;
                            break;
                        case "<UL>":
                        case "<P>":
                        case "<DIV>":
                            // just ignore these
                            break;

                        default:
                            AddRun(s);
                            break;
                    }
                }
                else
                {
                    AddRun(s);
                }
            }
        }

        int boldLevel = 0;
        int italicLevel = 0;

        private void AddRun(String text)
        {
            Run r = new Run();
            if (italicLevel > 0)
            {
                r.FontStyle = FontStyles.Italic;
            }
            if (boldLevel > 0)
            {
                r.FontWeight = FontWeights.Bold;
            }
            r.Text = text;
            _tb.Inlines.Add(r);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(String), typeof(ParsingTextblock),
            new PropertyMetadata("", new PropertyChangedCallback(textChanged)));
        public String Text
        {
            get { return (String)this.GetValue(TextProperty); }
            set
            {
                SetValue(TextProperty, value);
                if (value != null)
                {
                    Parse(value);
                }
                else
                {
                    Parse("");
                }
            }
        }

        public static void textChanged(Object sender, DependencyPropertyChangedEventArgs e)
        {
            ParsingTextblock ptb = sender as ParsingTextblock;
            ptb.Parse((String)e.NewValue);
        }

        public new Style Style
        {
            get { return _tb.Style; }
            set { _tb.Style = Style; }
        }

    }
}
