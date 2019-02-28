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
            var sourceBitmap = this.Bitmap;
            var filterMatrix = new double[,]
            {
                { -1, -1, -1, },
                { -1,  8, -1, },
                { -1, -1, -1, },
            };
            var rect = new Rectangle(default(int), default(int), sourceBitmap.Width, sourceBitmap.Height);
            var sourceData = sourceBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var pixelBuffer = new byte[sourceData.Stride * sourceData.Height];
            var resultBuffer = new byte[sourceData.Stride * sourceData.Height];

            Marshal.Copy(sourceData.Scan0, pixelBuffer, default(int), pixelBuffer.Length);
            sourceBitmap.UnlockBits(sourceData);

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

            var red = default(double);
            var green = default(double);
            var blue = default(double);
            var filterWidth = filterMatrix.GetLength(1);
            var filterHeight = filterMatrix.GetLength(0);
            var filterOffset = (filterWidth - 1) / 2;
            var calcOffset = default(int);
            var byteOffset = default(int);

            for (var offsetY = filterOffset; offsetY < sourceBitmap.Height - filterOffset; offsetY++)
            {
                for (var offsetX = filterOffset; offsetX < sourceBitmap.Width - filterOffset; offsetX++)
                {
                    red = default(double);
                    green = default(double);
                    blue = default(double);

                    byteOffset = offsetY * sourceData.Stride + offsetX * 4;

                    for (var filterY = -filterOffset; filterY <= filterOffset; filterY++)
                    {
                        for (var filterX = -filterOffset; filterX <= filterOffset; filterX++)
                        {
                            calcOffset = byteOffset + (filterX * 4) + (filterY * sourceData.Stride);
                            red += pixelBuffer[calcOffset + 2] * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                            green += pixelBuffer[calcOffset + 1] * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                            blue += pixelBuffer[calcOffset] * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                        }
                    }

                    red = factor * red + bias;
                    green = factor * green + bias;
                    blue = factor * blue + bias;

                    red = Math.Max(red, byte.MinValue);
                    red = Math.Min(red, byte.MaxValue);
                    green = Math.Max(green, byte.MinValue);
                    green = Math.Min(green, byte.MaxValue);
                    blue = Math.Max(blue, byte.MinValue);
                    blue = Math.Min(blue, byte.MaxValue);

                    resultBuffer[byteOffset + 0] = (byte)blue;
                    resultBuffer[byteOffset + 1] = (byte)green;
                    resultBuffer[byteOffset + 2] = (byte)red;
                    resultBuffer[byteOffset + 3] = byte.MaxValue;
                }
            }

            var resultBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);
            rect = new Rectangle(default(int), default(int), resultBitmap.Width, resultBitmap.Height);
            var resultData = resultBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            Marshal.Copy(resultBuffer, default(int), resultData.Scan0, resultBuffer.Length);
            resultBitmap.UnlockBits(resultData);

            this.Apply(resultBitmap);
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