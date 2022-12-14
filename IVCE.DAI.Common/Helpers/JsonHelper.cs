
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using System.Xml;

namespace IVCE.DAI.Common.Helpers
{



    public static class JsonXmlConverter<T>
    {
        public static string JsonToXml(string json)
        {
            var obj = JsonSerializer.Deserialize<T>(json)!;

            return ObjectToXml(obj);
        }

     public  static string ObjectToXml<T>(T obj)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));

            var sb = new StringBuilder();
            using var xmlWriter = XmlWriter.Create(sb);

            var ns = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            xmlSerializer.Serialize(xmlWriter, obj, ns);

            return sb.ToString();
        }
    }

  


}
