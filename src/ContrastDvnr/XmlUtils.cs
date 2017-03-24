using System;
using System.Collections.Generic;
using System.IO;
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



        public static T XmlDeserializeFromString<T>(string objectData)
        {
            object result = null;

           
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (TextReader reader = new StringReader(objectData))
                {
                    using (XmlTextReader xmlReader = new XmlTextReader(reader))
                    {
                        xmlReader.XmlResolver = null;
                        xmlReader.DtdProcessing = DtdProcessing.Prohibit;
                        xmlReader.Normalization = true;
                        result = serializer.Deserialize(xmlReader);
                    }
                }

            return (T)result;
        }
    }
}
