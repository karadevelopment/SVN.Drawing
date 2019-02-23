using SVN.Drawing.Casting;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SVN.Drawing.Imaging
{
    public static class Extensions
    {
        public static string GetResolution(this byte[] param)
        {
            using (var image = param.ToImage())
            {
                return image.GetResolution();
            }
        }

        public static string GetResolution(this Image param)
        {
            return $"{param.Width} X {param.Height}";
        }

        public static void ApplyRatio(this Image param, Size size)
        {
            var ratio = (double)param.Width / param.Height;

            if (ratio < 0)
            {
                size.Height = (int)(size.Width * ratio);
            }
            else
            {
                size.Width = (int)(size.Height * ratio);
            }
        }

        public static byte[] Resize(this byte[] param, int width, int height)
        {
            return param.Resize(new Size(width, height));
        }

        public static byte[] Resize(this byte[] param, Size size)
        {
            if (param.Length == 0)
            {
                return param;
            }
            using (var image = param.ToImage())
            {
                if (size.Width > image.Width || size.Height > image.Height) return param;

                image.ApplyRatio(size);
                using (var destImage = new Bitmap(size.Width, size.Height))
                {
                    destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
                    image.DrawGraphics(destImage);
                    return destImage.ToBytes();
                }
            }
        }

        public static void DrawGraphics(this Image param, Image destImage)
        {
            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var attributes = new ImageAttributes())
                {
                    attributes.SetWrapMode(WrapMode.TileFlipXY);
                    var srcRect = new Rectangle(Point.Empty, param.Size);
                    var destRect = new Rectangle(Point.Empty, destImage.Size);
                    graphics.DrawImage(param, destRect, srcRect, GraphicsUnit.Pixel, attributes);
                }
            }
        }

        public static void DrawImage(this Graphics param, Image image, Rectangle destRect, Rectangle srcRect, GraphicsUnit srcUnit, ImageAttributes imageAttr)
        {
            param.DrawImage(image, destRect, srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, srcUnit, imageAttr);
        }
    }
}