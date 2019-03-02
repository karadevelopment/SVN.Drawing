using SVN.Math2;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace SVN.Drawing.Helpers
{
    public class ImageData
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        private List<Color> Colors { get; } = new List<Color>();

        private Bitmap Bitmap
        {
            get
            {
                var result = new Bitmap(this.Width, this.Height);

                var i = default(int);
                foreach (var color in this.Colors)
                {
                    var x = i % this.Width;
                    var y = ((double)i / this.Width).FloorToInt();
                    result.SetPixel(x, y, color);
                    i++;
                }

                return result;
            }
        }

        private ImageData()
        {
        }

        public static ImageData From(Bitmap bitmap)
        {
            var input = new ImageData();
            input.Apply(bitmap);
            return input;
        }

        public static ImageData FromFile(string path)
        {
            using (var bitmap = new Bitmap(path))
            {
                return ImageData.From(bitmap);
            }
        }

        public void ToFile(string path)
        {
            var directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var bitmap = this.Bitmap)
            {
                bitmap.Save(path);
            }
        }

        private void SetPixels(Bitmap bitmap)
        {
            this.Colors.Clear();

            for (var y = 1; y <= bitmap.Height; y++)
            {
                for (var x = 1; x <= bitmap.Width; x++)
                {
                    this.Colors.Add(bitmap.GetPixel(x - 1, y - 1));
                }
            }
        }

        private void Apply(Bitmap bitmap)
        {
            this.Width = bitmap.Width;
            this.Height = bitmap.Height;
            this.SetPixels(bitmap);
        }

        public void ApplyConvolutionFilter(double factor = 1, int bias = 0, bool grayscale = true)
        {
            var filterMatrix = new double[,]
            {
                { -1, -1, -1, },
                { -1,  8, -1, },
                { -1, -1, -1, },
            };

            var bitmap = this.Bitmap;
            var sourceRect = new Rectangle(default(int), default(int), bitmap.Width, bitmap.Height);
            var sourceData = bitmap.LockBits(sourceRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var pixelBuffer = new byte[sourceData.Stride * sourceData.Height];
            var resultBuffer = new byte[sourceData.Stride * sourceData.Height];

            Marshal.Copy(sourceData.Scan0, pixelBuffer, default(int), pixelBuffer.Length);
            bitmap.UnlockBits(sourceData);

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

            for (var offsetY = filterOffset; offsetY < bitmap.Height - filterOffset; offsetY++)
            {
                for (var offsetX = filterOffset; offsetX < bitmap.Width - filterOffset; offsetX++)
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

            using (var resultBitmap = new Bitmap(bitmap.Width, bitmap.Height))
            {
                var resultRect = new Rectangle(default(int), default(int), resultBitmap.Width, resultBitmap.Height);
                var resultData = resultBitmap.LockBits(resultRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                Marshal.Copy(resultBuffer, default(int), resultData.Scan0, resultBuffer.Length);
                resultBitmap.UnlockBits(resultData);

                this.Apply(resultBitmap);
            }
        }

        private double GetNoise(Bitmap bitmap, int widthIndex, int heightIndex, int width, int height)
        {
            var result = new List<double>();

            var offsetX = widthIndex * width;
            var offsetY = heightIndex * height;

            for (var x = 1; x <= width; x++)
            {
                for (var y = 1; y <= height; y++)
                {
                    var color = bitmap.GetPixel(offsetX + x - 1, offsetY + y - 1);
                    var brightness = color.Brightness();
                    result.Add(brightness);
                }
            }

            return result.Average();
        }

        public double[] GetNoises()
        {
            var result = new List<double>();

            using (var bitmap = this.Bitmap)
            {
                var width = ((double)this.Width / 3).FloorToInt();
                var height = ((double)this.Height / 3).FloorToInt();

                for (var y = 1; y <= 3; y++)
                {
                    for (var x = 1; x <= 3; x++)
                    {
                        var noise = this.GetNoise(bitmap, x - 1, y - 1, width, height);
                        result.Add(noise);
                    }
                }
            }

            var maxNoise = result.Max();
            result = result.Select(x => x / maxNoise).ToList();
            return result.ToArray();
        }
    }
}