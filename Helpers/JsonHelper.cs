using System;
using System.IO;
using System.Text;
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
            catch //(Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"JSON deserialization error: {ex.Message}");
                throw;
            }
        }

        public static string Serialize(object obj)
        {
            try
            {
                return _serializer.Serialize(obj);
            }
            catch //(Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"JSON serialization error: {ex.Message}");
                throw;
            }
        }
    }
}

