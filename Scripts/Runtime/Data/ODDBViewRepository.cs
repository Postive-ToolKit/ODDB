using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TeamODD.ODDB.Runtime.Data.Interfaces;

namespace TeamODD.ODDB.Runtime.Data
{
    public class ODDBViewRepository<T> : ODDBRepository<T> where T : class, IODDBView ,new()
    {
        protected override T CreateInternal(string id)
        {
            var view = new T();
            view.Key = id;
            return view;
        }

        public override bool TrySerialize(out string data)
        {
            try
            {
                var dataList = new List<string>();
                foreach (var view in GetAll())
                    if (view.TrySerialize(out var serializedView))
                        dataList.Add(serializedView);
                var serializer = new XmlSerializer(typeof(List<string>));
                using var stringWriter = new System.IO.StringWriter();
                serializer.Serialize(stringWriter, dataList);
                data = stringWriter.ToString();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                data = null;
                return false;
            }
        }

        public override bool TryDeserialize(string data)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<string>));
                using var stringReader = new System.IO.StringReader(data);
                var viewDataList = (List<string>)serializer.Deserialize(stringReader);
                foreach (var viewData in viewDataList)
                {
                    var view = new T();
                    view.TryDeserialize(viewData);
                    Update(view.Key, view);
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}