using Newtonsoft.Json;
using System.Globalization;
using System.IO;

namespace PicturesToGpx
{
    public static class ConfigReader
    {

        public static Settings ReadConfig(string configFile)
        {
            return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(configFile), new JsonSerializerSettings
            {
                Culture = CultureInfo.InvariantCulture,
                DateParseHandling = DateParseHandling.DateTimeOffset,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
            });
        }
    }
}