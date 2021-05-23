using Newtonsoft.Json;

namespace Shared.DiscordHelpers
{
    public class WebhookModel
    {
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("embeds")]
        public WebhookEmbed[] Embeds { get; set; }
    }

    public class WebhookEmbed
    {
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("color")]
        public uint? Color { get; set; }
        [JsonProperty("author")]
        public Author Author { get; set; }
        [JsonProperty("fields")]
        public Field[] Fields { get; set; }
        [JsonProperty("thumbnail")]
        public Thumbnail Thumbnail { get; set; }
        [JsonProperty("video")]
        public Video Video { get; set; }
        [JsonProperty("footer")]
        public Footer Footer { get; set; }
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }
    }

    public class Author
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }
    }

    public class Thumbnail
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class Video
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class Footer
    {
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }
    }

    public class Field
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
        [JsonProperty("inline")]
        public bool? Inline { get; set; }
    }
}
