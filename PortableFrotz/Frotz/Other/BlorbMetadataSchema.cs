#if REMOVE
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace Frotz.Blorb
{
    public class BlorbMetadata
    {
        public String Title { get; private set; }
        public String Headline { get; private set; }
        public String Description { get; private set; }
        public Dictionary<String, String> Biblography { get; private set; }
        public byte[] CoverPicture { get; private set; }

        private String _nsName;
        XDocument doc;

        private string getFirstValue(String Name)
        {
            var e = getFirstElement(Name);
            if (e == null) return "";
            var xr = e.CreateReader();
            xr.MoveToContent();
            return xr.ReadInnerXml();
        }

        private XElement getFirstElement(String Name)
        {
            var desc = doc.Descendants(XName.Get(Name, _nsName));
            foreach (var e in desc)
            {
                return e;
            }
            return null;
        }

        public BlorbMetadata(Blorb b)
        {
            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(ifindex));
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(b.MetaData);
                var ms = new System.IO.MemoryStream(buffer);
            Object o = xs.Deserialize(ms);
            var ii = o as ifindex;

            this.Description = ii.story.bibliographic.description;
            this.Title = ii.story.bibliographic.title;
            this.Headline = ii.story.bibliographic.headline;

            if (ii.story.zcode.Length > 0)
            {
                String temp = ii.story.zcode[0].coverpicture;
                CoverPicture = b.Pictures[Convert.ToInt32(temp)].Image;
            }
            /*
            CoverPicture = null;
            Biblography = new Dictionary<string, string>();

            doc = XDocument.Parse(b.MetaData);
            var r = doc.Root;
            _nsName = r.Name.NamespaceName;

            String val = getFirstValue("coverpicture");
            if (!String.IsNullOrEmpty(val))
            {
                CoverPicture = b.Pictures[Convert.ToInt32(val)].Image;
            }

            var t = getFirstElement("description");
            var xr = t.CreateReader();
            xr.MoveToContent();
            String inner = xr.ReadInnerXml();

            this.Description = getFirstValue("description");
            this.Title = getFirstValue("title");
            this.Headline = getFirstValue("headline");

            var bio = getFirstElement("bibliographic");

            foreach (var e in bio.Elements())
            {
                Biblography.Add(e.Name.LocalName, e.Value);
            }
            */
        }
    }


    [XmlType(AnonymousType = true, Namespace = "http://babel.ifarchive.org/protocol/iFiction/")]
    [XmlRoot(Namespace = "http://babel.ifarchive.org/protocol/iFiction/", IsNullable = false)]
    public partial class ifindex
    {
        [XmlElement("story")]
        public ifindexStory story { get; set; }

        [XmlAttribute()]
        public string version { get; set; }
    }

    [XmlType(AnonymousType = true, Namespace = "http://babel.ifarchive.org/protocol/iFiction/")]
    public partial class ifindexStory
    {
        [XmlElement("bibliographic")]
        public ifindexStoryBibliographic bibliographic { get; set; }

        [XmlElement("identification")]
        public ifindexStoryIdentification[] identification { get; set; }

        [XmlElement("cover")]
        public ifindexStoryCover[] cover { get; set; }

        [XmlElement("contacts")]
        public ifindexStoryContacts[] contacts { get; set; }

        [XmlElement("zcode")]
        public ifindexStoryZcode[] zcode { get; set; }

        [XmlElement("colophon")]
        public ifindexStoryColophon[] colophon { get; set; }

        // TODO Implement Releases
    }


    [XmlType(AnonymousType = true, Namespace = "http://babel.ifarchive.org/protocol/iFiction/")]
    public partial class ifindexStoryIdentification
    {
        public string format { get; set; }

        [XmlElement("ifid", IsNullable = true)]
        public ifindexStoryIdentificationIfid[] ifid { get; set; }
    }

    [XmlType(AnonymousType = true, Namespace = "http://babel.ifarchive.org/protocol/iFiction/")]
    public partial class ifindexStoryIdentificationIfid
    {
        [XmlText()]
        public string Value { get; set; }
    }

    [XmlType(AnonymousType = true, Namespace = "http://babel.ifarchive.org/protocol/iFiction/")]
    public partial class ifindexStoryBibliographic
    {
        public string title { get; set; }
        public string author { get; set; }
        public string language { get; set; }
        public string headline { get; set; }
        public string firstpublished { get; set; }
        public string genre { get; set; }
        public string group { get; set; }
        public string series { get; set; }
        public string seriesnumber { get; set; }

#if !SILVERLIGHT
        [XmlAnyElement("description")]
        public System.Xml.XmlElement descriptionElement { get; set; }
#else
        [XmlAnyElement("description")]
        public System.Xml.Linq.XElement descriptionElement { get; set; } 
#endif

        public string description
        {
            get
            {
                if (descriptionElement != null)
                {
#if !SILVERLIGHT
                    string desc = descriptionElement.InnerXml;
                    string ns = string.Format(" xmlns=\"{0}\"", descriptionElement.NamespaceURI);
                    desc = desc.Replace(ns, "");
                    return desc;
#else
                    //string desc = descriptionElement
                    //string ns = string.Format(" xmlns=\"{0}\"", descriptionElement.NamespaceURI);
                    //desc = desc.Replace(ns, "");
                    //return desc;
                    String desc = descriptionElement.ToString();
                    int index = desc.IndexOf(">");
                    desc = desc.Substring(index + 1);
                    index = desc.LastIndexOf("<");
                    desc = desc.Substring(0, index - 1);
                    return desc;
#endif
                }
                else
                {
                    return null;
                }
            }
        }
    }

    [XmlType(AnonymousType = true, Namespace = "http://babel.ifarchive.org/protocol/iFiction/")]
    public partial class ifindexStoryCover
    {
        public string format { get; set; }
        public string height { get; set; }
        public string width { get; set; }
    }

    [XmlType(AnonymousType = true, Namespace = "http://babel.ifarchive.org/protocol/iFiction/")]
    public partial class ifindexStoryContacts
    {
        public string url { get; set; }
    }

    [XmlType(AnonymousType = true, Namespace = "http://babel.ifarchive.org/protocol/iFiction/")]
    public partial class ifindexStoryReleasesHistoryRelease
    {
        public string version { get; set; }
        public string releasedate { get; set; }
        public string compiler { get; set; }
        public string compilerversion { get; set; }
    }

    [XmlType(AnonymousType = true, Namespace = "http://babel.ifarchive.org/protocol/iFiction/")]
    public partial class ifindexStoryZcode
    {
        public string coverpicture { get; set; }
    }

    [XmlType(AnonymousType = true, Namespace = "http://babel.ifarchive.org/protocol/iFiction/")]
    public partial class ifindexStoryColophon
    {
        public string generator { get; set; }
        public string originated { get; set; }
    }
}
#endif