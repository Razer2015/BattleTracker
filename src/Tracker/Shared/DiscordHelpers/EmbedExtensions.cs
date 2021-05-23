using Discord;
using System.Linq;

namespace Shared.DiscordHelpers
{
    public static class EmbedExtensions
    {
        public static WebhookEmbed ToModel(this Discord.Embed entity)
        {
            if (entity == null) return null;

            var model = new WebhookEmbed {
                //Type = entity.Type,
                Title = entity.Title,
                Description = entity.Description,
                Url = entity.Url,
                Timestamp = entity.Timestamp?.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                Color = entity.Color?.RawValue
            };
            if (entity.Author != null)
                model.Author = entity.Author.Value.ToModel();
            model.Fields = entity.Fields.Select(x => x.ToModel()).ToArray();
            if (entity.Footer != null)
                model.Footer = entity.Footer.Value.ToModel();
            //if (entity.Image != null)
            //    model.Image = entity.Image.Value.ToModel();
            //if (entity.Provider != null)
            //    model.Provider = entity.Provider.Value.ToModel();
            if (entity.Thumbnail != null)
                model.Thumbnail = entity.Thumbnail.Value.ToModel();
            if (entity.Video != null)
                model.Video = entity.Video.Value.ToModel();
            return model;
        }

        public static Author ToModel(this EmbedAuthor entity)
        {
            return new Author { Name = entity.Name, Url = entity.Url, IconUrl = entity.IconUrl };
        }

        public static Field ToModel(this EmbedField entity)
        {
            return new Field { Name = entity.Name, Value = entity.Value, Inline = entity.Inline };
        }

        public static Footer ToModel(this EmbedFooter entity)
        {
            return new Footer { Text = entity.Text, IconUrl = entity.IconUrl };
        }

        public static Thumbnail ToModel(this EmbedThumbnail entity)
        {
            return new Thumbnail { Url = entity.Url };
        }

        public static Video ToModel(this EmbedVideo entity)
        {
            return new Video { Url = entity.Url };
        }
    }
}
