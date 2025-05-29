namespace arasakagram.Models
{
    public class ChatModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LastMessage { get; set; }
        public bool IsOnline { get; set; }
        public byte[] Avatar { get; set; } // Для хранения изображения
        public System.Windows.Media.Imaging.BitmapImage AvatarImage
        {
            get
            {
                if (Avatar == null || Avatar.Length == 0)
                    return null;
                using (var ms = new System.IO.MemoryStream(Avatar))
                {
                    var image = new System.Windows.Media.Imaging.BitmapImage();
                    image.BeginInit();
                    image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
        }
        public int ID_ChatType { get; set; } // 1 — личный, 2 — групповой
        public int CreatedByUserId { get; set; } // ID создателя чата
    }
} 