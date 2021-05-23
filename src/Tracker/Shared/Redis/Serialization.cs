using Newtonsoft.Json;
using System.Text;

namespace Shared.Redis
{
    public static class Serialization
    {
        public static byte[] ToByteArray(this object obj)
        {
            if (obj == null)
            {
                return null;
            }

            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
        }
        public static T FromByteArray<T>(this byte[] data) where T : class
        {
            if (data == null)
            {
                return default;
            }

            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data));
        }
    }
}
