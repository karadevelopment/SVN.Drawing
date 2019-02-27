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
        private List<Color> Pixels { get; } = new List<Color>();

        private Bitmap Bitmap
        {
            get
            {
                var result = new Bitmap(this.Width, this.Height);

                var i = default(int);
                foreach (var pixel in this.Pixels)
                {
                    var x = i % this.Width;
                    var y = ((double)i / this.Width).FloorToInt();
                    result.SetPixel(x, y, pixel);
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

        private void Apply(Bitmap bitmap)
        {
            this.Width = bitmap.Width;
            this.Height = bitmap.Height;
            this.SetPixels(bitmap);
        }

        private void SetPixels(Bitmap bitmap)
        {
            this.Pixels.Clear();

            for (var y = 1; y <= bitmap.Height; y++)
            {
                for (var x = 1; x <= bitmap.Width; x++)
                {
                    this.Pixels.Add(bitmap.GetPixel(x - 1, y - 1));
                }
            }
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

            if (grayscale == true)
            {
                for (int i = 1; i <= pixelBuffer.Length; i += 4)
                {
                    var rgb = default(double);

                    rgb += pixelBuffer[i - 1] * 0.11f;
                    rgb += pixelBuffer[i + 0] * 0.59f;
                    rgb += pixelBuffer[i + 1] * 0.3f;

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
            int byteOffset = default(int);

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
            var resultData = resultBitmap.LockBits(new Rectangle(default(int), default(int), resultBitmap.Width, resultBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            Marshal.Copy(resultBuffer, default(int), resultData.Scan0, resultBuffer.Length);
            resultBitmap.UnlockBits(resultData);

            this.Apply(resultBitmap);
        }

        internal double[] GetArray()
        {
            return this.Pixels.Select(x => x.Sigmoid()).ToArray();
        }
    }
}