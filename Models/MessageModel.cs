using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.IO;

namespace arasakagram.Models
{
    public class MessageModel
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public string Sender { get; set; }
        public string Text { get; set; }
        public DateTime Time { get; set; }
        public bool IsOwn { get; set; }
        public List<FileModel> Files { get; set; }
        public byte[] Avatar { get; set; }
        public BitmapImage AvatarImage
        {
            get
            {
                if (Avatar == null || Avatar.Length == 0) return null;
                using (var ms = new MemoryStream(Avatar))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
        }
    }
} 