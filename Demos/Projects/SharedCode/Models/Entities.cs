using Newtonsoft.Json;

namespace SharedCode.Models
{
    public class Entities
    {
        [JsonProperty("hashtags")]
        public HashTag[] HashTags { get; set; }

        [JsonProperty("user_mentions")]
        public UserMention[] UserMentions { get; set; }
    }
}
