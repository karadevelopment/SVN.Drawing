using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SVN.Drawing.Casting
{
    public static class Extensions
    {
        public static byte[] ToBytes(this Image param)
        {
            using (var ms = new MemoryStream())
            {
                param.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }

        public static Image ToImage(this byte[] param)
        {
            if (param.Length == 0)
            {
                return new Bitmap(1, 1);
            }
            return Image.FromStream(new MemoryStream(param));
        }

        public static string ToBase64String(this byte[] param)
        {
            return "data:image;base64," + Convert.ToBase64String(param);
        }
    }
}