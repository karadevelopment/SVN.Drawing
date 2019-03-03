using SVN.Math2;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace SVN.Drawing
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

        public static Bitmap[] Split(this Bitmap param)
        {
            var result = new List<Bitmap>();

            var width = ((double)param.Width / 3).FloorToInt();
            var height = ((double)param.Height / 3).FloorToInt();

            for (var j = 1; j <= 3; j++)
            {
                for (var i = 1; i <= 3; i++)
                {
                    var bitmap = new Bitmap(width, height);

                    var offsetX = (i - 1) * width;
                    var offsetY = (j - 1) * height;

                    for (var y = 1; y <= height; y++)
                    {
                        for (var x = 1; x <= width; x++)
                        {
                            var color = param.GetPixel(offsetX + x - 1, offsetY + y - 1);
                            bitmap.SetPixel(x - 1, y - 1, color);
                        }
                    }

                    result.Add(bitmap);
                }
            }

            return result.ToArray();
        }

        public static double Brightness(this Color param)
        {
            var value = param.R + param.G + param.B;
            var valueMax = byte.MaxValue * 3;
            return (double)value / valueMax;
        }

        public static double Brightness(this Bitmap param)
        {
            var result = new List<double>();

            for (var y = 1; y <= param.Height; y++)
            {
                for (var x = 1; x <= param.Width; x++)
                {
                    var color = param.GetPixel(x - 1, y - 1);
                    var brightness = color.Brightness();
                    result.Add(brightness);
                }
            }

            return result.Average();
        }

        public static double[] Brightnesses(this Bitmap param)
        {
            var result = new List<double>();

            foreach (var bitmap in param.Split())
            {
                var brightness = bitmap.Brightness();
                result.Add(brightness);
            }

            return result.Normalize().ToArray();
        }

        public static double[] BrightnessesHorizontal(this Bitmap param)
        {
            var result = new List<double>();
            var noises = param.Brightnesses();

            for (var i = 1; i <= 3; i++)
            {
                var brightness1 = noises[i - 1 + 0];
                var brightness2 = noises[i - 1 + 3];
                var brightness3 = noises[i - 1 + 6];
                var brightness = (brightness1 + brightness2 + brightness3) / 3;
                result.Add(brightness);
            }

            return result.Normalize().ToArray();
        }

        public static double[] BrightnessesVertical(this Bitmap param)
        {
            var result = new List<double>();
            var noises = param.Brightnesses();

            for (var i = 1; i <= 3; i++)
            {
                var brightness1 = noises[(i - 1) * 3 + 0];
                var brightness2 = noises[(i - 1) * 3 + 1];
                var brightness3 = noises[(i - 1) * 3 + 2];
                var brightness = (brightness1 + brightness2 + brightness3) / 3;
                result.Add(brightness);
            }

            return result.Normalize().ToArray();
        }

        public static double Noise(this Bitmap param)
        {
            using (var bitmap = param.ConvolutionFilter())
            {
                return bitmap.Brightness();
            }
        }

        public static double[] Noises(this Bitmap param)
        {
            var result = new List<double>();

            foreach (var bitmap in param.Split())
            {
                var noise = bitmap.Noise();
                result.Add(noise);
            }

            return result.Normalize().ToArray();
        }

        public static double[] NoisesHorizontal(this Bitmap param)
        {
            var result = new List<double>();
            var noises = param.Noises();

            for (var i = 1; i <= 3; i++)
            {
                var noise1 = noises[i - 1 + 0];
                var noise2 = noises[i - 1 + 3];
                var noise3 = noises[i - 1 + 6];
                var noise = (noise1 + noise2 + noise3) / 3;
                result.Add(noise);
            }

            return result.Normalize().ToArray();
        }

        public static double[] NoisesVertical(this Bitmap param)
        {
            var result = new List<double>();
            var noises = param.Noises();

            for (var i = 1; i <= 3; i++)
            {
                var noise1 = noises[(i - 1) * 3 + 0];
                var noise2 = noises[(i - 1) * 3 + 1];
                var noise3 = noises[(i - 1) * 3 + 2];
                var noise = (noise1 + noise2 + noise3) / 3;
                result.Add(noise);
            }

            return result.Normalize().ToArray();
        }

        public static void Rotate90(this Image param)
        {
            param.RotateFlip(RotateFlipType.Rotate90FlipNone);
        }

        public static void FlipVertical(this Image param)
        {
            param.RotateFlip(RotateFlipType.RotateNoneFlipY);
        }

        public static void ExifRotate(this Image param)
        {
            var exifOrientationId = 0x112;

            if (!param.PropertyIdList.Contains(exifOrientationId))
            {
                return;
            }

            var prop = param.GetPropertyItem(exifOrientationId);
            var value = BitConverter.ToUInt16(prop.Value, default(int));
            var rotation = RotateFlipType.RotateNoneFlipNone;

            if (value == 3 || value == 4)
            {
                rotation = RotateFlipType.Rotate180FlipNone;
            }
            else if (value == 5 || value == 6)
            {
                rotation = RotateFlipType.Rotate90FlipNone;
            }
            else if (value == 7 || value == 8)
            {
                rotation = RotateFlipType.Rotate270FlipNone;
            }
            if (value == 2 || value == 4 || value == 5 || value == 7)
            {
                rotation |= RotateFlipType.RotateNoneFlipX;
            }
            if (rotation != RotateFlipType.RotateNoneFlipNone)
            {
                param.RotateFlip(rotation);
            }
        }

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

        public static Bitmap ConvolutionFilter(this Bitmap param, double factor = 1, int bias = 0, bool grayscale = true)
        {
            var filterMatrix = new double[,]
            {
                { -1, -1, -1, },
                { -1,  8, -1, },
                { -1, -1, -1, },
            };
            
            var sourceRect = new Rectangle(default(int), default(int), param.Width, param.Height);
            var sourceData = param.LockBits(sourceRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var pixelBuffer = new byte[sourceData.Stride * sourceData.Height];
            var resultBuffer = new byte[sourceData.Stride * sourceData.Height];

            Marshal.Copy(sourceData.Scan0, pixelBuffer, default(int), pixelBuffer.Length);
            param.UnlockBits(sourceData);

            if (grayscale)
            {
                for (var i = 1; i <= pixelBuffer.Length; i += 4)
                {
                    var rgb = default(double);

                    rgb += pixelBuffer[i - 1] * 0.11f;
                    rgb += pixelBuffer[i + 0] * 0.59f;
                    rgb += pixelBuffer[i + 1] * 0.30f;

                    pixelBuffer[i - 1] = (byte)rgb;
                    pixelBuffer[i + 0] = pixelBuffer[i - 1];
                    pixelBuffer[i + 1] = pixelBuffer[i - 1];
                    pixelBuffer[i + 2] = byte.MaxValue;
                }
            }

            var filterWidth = filterMatrix.GetLength(1);
            var filterOffset = (filterWidth - 1) / 2;

            for (var offsetY = filterOffset; offsetY < param.Height - filterOffset; offsetY++)
            {
                for (var offsetX = filterOffset; offsetX < param.Width - filterOffset; offsetX++)
                {
                    var r = default(double);
                    var g = default(double);
                    var b = default(double);

                    var byteOffset = offsetY * sourceData.Stride + offsetX * 4;

                    for (var y = -filterOffset; y <= filterOffset; y++)
                    {
                        for (var x = -filterOffset; x <= filterOffset; x++)
                        {
                            var offset = byteOffset + (x * 4) + (y * sourceData.Stride);
                            b += pixelBuffer[offset + 0] * filterMatrix[y + filterOffset, x + filterOffset];
                            g += pixelBuffer[offset + 1] * filterMatrix[y + filterOffset, x + filterOffset];
                            r += pixelBuffer[offset + 2] * filterMatrix[y + filterOffset, x + filterOffset];
                        }
                    }

                    r = factor * r + bias;
                    g = factor * g + bias;
                    b = factor * b + bias;

                    r = Math.Max(r, byte.MinValue);
                    r = Math.Min(r, byte.MaxValue);
                    g = Math.Max(g, byte.MinValue);
                    g = Math.Min(g, byte.MaxValue);
                    b = Math.Max(b, byte.MinValue);
                    b = Math.Min(b, byte.MaxValue);

                    resultBuffer[byteOffset + 0] = (byte)b;
                    resultBuffer[byteOffset + 1] = (byte)g;
                    resultBuffer[byteOffset + 2] = (byte)r;
                    resultBuffer[byteOffset + 3] = byte.MaxValue;
                }
            }

            using (var resultBitmap = new Bitmap(param.Width, param.Height))
            {
                var resultRect = new Rectangle(default(int), default(int), resultBitmap.Width, resultBitmap.Height);
                var resultData = resultBitmap.LockBits(resultRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                Marshal.Copy(resultBuffer, default(int), resultData.Scan0, resultBuffer.Length);
                resultBitmap.UnlockBits(resultData);

                return new Bitmap(resultBitmap);
            }
        }
    }
}