using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

namespace Server.Logic
{
    // 영속 객체는 자신의 고유한 Id를 가져야 한다.
    // 따라서 PersistenceData class를 상속 받아야만 한다.
    [DataContract]
    class PersistenceData
    {
        [DataMember]
        public int ObjectId { get; set; }
    }

    // PersistenceData를 상속 받은 객체들을 xml 파일로 관리해준다.
    // 현재는 저장된 모든 정보를 모두 메모리에 올려놓는다.
    class Persistence
    {
        private const string DataDirectory = "Persist";

        private readonly Dictionary<Type, DataContractSerializer> _serMap = new Dictionary<Type, DataContractSerializer>();
        private readonly Dictionary<int /* objectId */, PersistenceData> _dataMap = new Dictionary<int, PersistenceData>();
        private int _idSerial;

        public Persistence()
        {
            if (!Directory.Exists(DataDirectory))
                Directory.CreateDirectory(DataDirectory);

            // 본 Assembly에 존재하는 모든 PersistenceData로 미리 DataContractSerializer를 만들어둔다.
            var dataType = typeof(PersistenceData);
            var loadSerMap = new Dictionary<string /* typename */, DataContractSerializer>();
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!dataType.IsAssignableFrom(type))
                    continue;

                var ser = new DataContractSerializer(type);
                _serMap.Add(type, ser);
                loadSerMap.Add(type.Name, ser);
            }

            // 저장된 xml 파일들을 읽어놓는다.
            var nameRegex = new Regex(@"([^_]+)_(\d+)", RegexOptions.Compiled);
            foreach (var xmlFile in Directory.GetFiles(DataDirectory, "*.xml"))
            {
                var fileName = Path.GetFileNameWithoutExtension(xmlFile);
                var nameMatch = nameRegex.Match(fileName);
                if (!nameMatch.Success)
                    continue;

                var typename = nameMatch.Groups[1].Value;
                if (!loadSerMap.ContainsKey(typename))
                    continue;

                using (var stream = new FileStream(xmlFile, FileMode.Open))
                using (var reader = XmlReader.Create(stream))
                {
                    var ser = loadSerMap[typename];
                    var data = (PersistenceData)ser.ReadObject(reader);
                    _dataMap.Add(data.ObjectId, data);
                }
            }

            // Serial이 겹치지 않도록 처리해준다.
            if (_dataMap.Count > 0)
                _idSerial = _dataMap.Select(e => e.Key).Max();
        }

        public T Find<T>(Func<T, bool> predicate) where T : PersistenceData
        {
            return FindAll(predicate).FirstOrDefault();
        }

        public IEnumerable<T> FindAll<T>(Func<T, bool> predicate) where T : PersistenceData
        {
            foreach (var value in _dataMap.Values.Where(e => e.GetType() == typeof(T)))
            {
                if (predicate((T)value))
                    yield return (T)value;
            }
        }

        public void Store(PersistenceData obj)
        {
            if (obj == null)
                return;

            var objType = obj.GetType();
            if (!_serMap.ContainsKey(objType))
                throw new InvalidOperationException("obj_should_inherit_PersistenceData");

            // PersistenceId가 없는 새 객체일 경우 새로 발급한다.
            if (obj.ObjectId == 0)
            {
                obj.ObjectId = ++_idSerial;
            }

            // 파일을 추가하고, 메모리에도 넣어둔다.
            var path = Path.Combine(DataDirectory, string.Format("{0}_{1}.xml", objType.Name, obj.ObjectId));
            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true }))
            {
                var ser = _serMap[objType];
                ser.WriteObject(writer, obj);
            }
            _dataMap.Add(obj.ObjectId, obj);
        }

        public void Delete(PersistenceData obj)
        {
            if (obj == null)
                return;

            if (obj.ObjectId == 0)
                return;

            // 파일을 삭제하고, 메모리에서도 제거한다.
            var objType = obj.GetType();
            var path = Path.Combine(DataDirectory, string.Format("{0}_{1}.xml", objType.Name, obj.ObjectId));
            if (File.Exists(path))
                File.Delete(path);

            _dataMap.Remove(obj.ObjectId);
        }
    }
}
