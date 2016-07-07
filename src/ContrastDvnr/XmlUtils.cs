using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace ContrastDvnr
{
    public class XmlUtils
    {
        public static string XmlSerializeToString(object objectInstance)
        {

            XmlSerializer serializer = new XmlSerializer(objectInstance.GetType());
            var sb = new StringBuilder();

            using (var writer = XmlWriter.Create(sb,
                new XmlWriterSettings()
                {
                    OmitXmlDeclaration = true, // dont write the <?xml ..> element
                    Indent = true,
                    NewLineHandling = NewLineHandling.None,
                    CheckCharacters = false
                }))
            {
                // dont write the namespaces
                var ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                serializer.Serialize(writer, objectInstance, ns);

                return sb.ToString();
            }
        }
    }
}
