using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SceneManager.Utils
{
    internal static class Serializer
    {
        private static Dictionary<Type, XmlSerializer> _serializerCache = new Dictionary<Type, XmlSerializer>();
        
        private static XmlSerializer _getOrCreateSerializer<T>(XmlAttributeOverrides overrides = null)
        {
            if (_serializerCache.ContainsKey(typeof(T)))
            {
                Game.LogTrivialDebug("Serializer cache already contains " + typeof(T).Name);
                return _serializerCache[typeof(T)];
            }
            else
            {
                Game.LogTrivialDebug("Adding " + typeof(T).Name + " to serializer cache");
                Game.LogTrivialDebug("Overrides specified: " + (overrides != null));
                var s = new XmlSerializer(typeof(T), overrides);
                _serializerCache.Add(typeof(T), s);
                return s;
            }
        }

        public static void SaveToNode(string file, string node, string value)
        {
            XmlNode n = SelectNodeFromXml(file, node);

            if (n == null) throw new KeyNotFoundException($"{nameof(SaveToNode)}: specified node does not exists!");

            n.InnerText = value;
            var doc = new XmlDocument();
            doc.Save(file);
        }

        public static string ReadFromNode(string file, string node)
        {
            return SelectNodeFromXml(file, node).InnerText;
        }

        private static XmlNode SelectNodeFromXml(string filePath, string node)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException($"{nameof(SelectNodeFromXml)}(): specified file does not exist: {filePath}");

            using (TextReader reader = new StreamReader(filePath))
            {
                var doc = new XmlDocument();
                doc.Load(reader);
                return doc.SelectSingleNode(node);
            }
        }

        public static List<T> LoadAllXML<T>(string dirPath, SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (!Directory.Exists(dirPath)) throw new DirectoryNotFoundException($"{nameof(LoadAllXML)}(): specified directory could not be found: {dirPath}");

            string[] files = Directory.GetFiles(dirPath, "*.xml", searchOption);

            List<T> result = new List<T>();

            Array.ForEach(files, f => result.AddRange(LoadFromXML<T>(f)));

            return result;
        }

        public static void SaveToXML<T>(List<T> list, string filePath)
        {
            SaveItemToXML(list, filePath);
        }

        public static void SaveItemToXML<T>(T item, string path, XmlAttributeOverrides overrides = null)
        {
            Encoding utf8NoBom = new UTF8Encoding(false);
            using (TextWriter writer = new StreamWriter(path, false, utf8NoBom))
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");

                _getOrCreateSerializer<T>(overrides).Serialize(writer, item, ns);
            }
        }

        //public static void SaveItemToXML<T>(T item, string path)
        //{
        //    SaveItemToXML(item, path, null);
        //}

        public static T LoadItemFromXML<T>(string filePath, XmlAttributeOverrides overrides = null)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException($"{nameof(LoadItemFromXML)}(): specified file does not exist: {filePath}");

            using (TextReader reader = new StreamReader(filePath))
            {
                return (T)_getOrCreateSerializer<T>(overrides).Deserialize(reader);
            }
        }

        //public static T LoadItemFromXML<T>(string filePath)
        //{
        //    return LoadItemFromXML<T>(filePath, null);
        //}

        public static void ModifyItemInXML<T>(string filePath, Action<T> modification)
        {
            T item = LoadItemFromXML<T>(filePath);
            modification(item);
            SaveItemToXML<T>(item, filePath);
        }

        public static T GetSelectedListElementFromXml<T>(string file, Func<List<T>, T> selector)
        {
            List<T> deserialized = LoadItemFromXML<List<T>>(file);
            return selector(deserialized);
        }

        public static List<T> LoadFromXML<T>(string filePath)
        {
            return LoadItemFromXML<List<T>>(filePath);
        }

        public static void AppendToXML<T>(T objectToAdd, string path)
        {
            ModifyItemInXML<List<T>>(path, t => t.Add(objectToAdd));
        }

        internal static XmlAttributeOverrides DefineOverrides()
        {
            XmlAttributeOverrides overrides = new XmlAttributeOverrides();
            XmlAttributes attr = new XmlAttributes();
            attr.XmlRoot = new XmlRootAttribute("Paths");
            overrides.Add(typeof(List<Paths.Path>), attr);

            return overrides;
        }
    }
}
