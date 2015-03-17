using Newtonsoft.Json;

namespace uSignalR.Hubs
{
    public static class HubProxyExtensions
    {
        public static T GetValue<T>(IHubProxy proxy, string name)
        {
            object value = proxy[name];
            return Convert<T>(value);
        }

        private static T Convert<T>(object obj)
        {
            if (obj == null)
                return default(T);

            if (obj is T)
                return (T) obj;

            return JsonConvert.DeserializeObject<T>(obj.ToString());
        }
    }
}