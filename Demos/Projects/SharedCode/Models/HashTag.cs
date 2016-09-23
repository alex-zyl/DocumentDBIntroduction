using Newtonsoft.Json;

namespace SharedCode.Models
{
    public class HashTag
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("indices")]
        public int[] Indices { get; set; }
    }
}
