using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class QuickTests
    {
        [Test]
        public void Test()
        {
            var xml = new XmlDocument();
            xml.Load(@"d:\GDYR\MESPOD\Doc\svn\DB\1.txt");

            var map = new Dictionary<string, string>();
            foreach (XmlNode entry in xml.SelectNodes("//entry[@kind='file']"))
            {
                var name = entry.FirstChild.InnerText;
                var revision = entry.SelectSingleNode("commit/@revision").InnerText;

                map.Add(name, revision);
            }

            var qva = 1;
        }
    }
}
