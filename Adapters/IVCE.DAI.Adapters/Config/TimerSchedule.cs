
using System.Text.Json.Serialization;

namespace IVCE.DAI.Adapters.Config
{
    public class TimerSchedule
    {
        [JsonPropertyName("ScheduledTime")]
        public string ScheduledTime { get; set; }

        [JsonPropertyName("ScheduledApplication")]
        public string ScheduledApplication { get; set; }
    }
}
