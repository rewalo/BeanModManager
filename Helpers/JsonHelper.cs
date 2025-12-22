using System.Web.Script.Serialization;

namespace BeanModManager.Helpers
{
    public static class JsonHelper
    {
        private static readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public static T Deserialize<T>(string json)
        {
            try
            {
                return _serializer.Deserialize<T>(json);
            }
            catch
            {
                throw;
            }
        }

        public static string Serialize(object obj)
        {
            try
            {
                return _serializer.Serialize(obj);
            }
            catch
            {
                throw;
            }
        }
    }
}

