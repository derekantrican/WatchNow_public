using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;

namespace WatchNow.Helpers
{
    public static class RssHelpers
    {
        public static string GetExtensionValue(this SyndicationItem item, string[] elements, string attributeName = null)
        {
            string topLevelElement = elements[0];
            string[] innerElements = elements.Skip(1).ToArray();

            var ext = item.ElementExtensions.FirstOrDefault(e => e.OuterName == topLevelElement);

            if (ext == null) 
                return null;

            var xml = ext.GetObject<XmlElement>();

            // Todo: process innerElements
            if (innerElements.Length > 0)
            {
                xml = SelectPath(xml, innerElements);
            }

            return attributeName == null
                ? xml.InnerText
                : xml.GetAttribute(attributeName);
        }

        private static XmlElement SelectPath(XmlElement root, params string[] path)
        {
            XmlElement current = root;

            foreach (var segment in path)
            {
                if (current == null) return null;

                current = current.ChildNodes
                    .OfType<XmlElement>()
                    .FirstOrDefault(e => e.LocalName == segment);
            }

            return current;
        }
    }
}
