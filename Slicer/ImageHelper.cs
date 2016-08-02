using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Slicer
{
    public static class ImageHelper
    {
        /// <summary>
        /// Gets the content of a known file as 2Bit-Image Bits
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static BitArray LoadBits(string path)
        {
            var ext = System.IO.Path.GetExtension(path);
            switch (ext.ToLower())
            {
                case ".dat":
                case ".dot":
                case ".mat":
                case ".mot":
                case ".sot":
                case ".sat":
                    //Direct import
                    var imageBytes = System.IO.File.ReadAllBytes(path);
                    var imageBits = OrderBits(new BitArray(imageBytes));

                    return imageBits;
                case ".bmp":
                    //Convert (todo)
                    break;
            }

            //Default
            return null;
        }

        /// <summary>
        /// Loads an Image initially from the BitData
        /// </summary>
        /// <param name="imageBits"></param>
        /// <returns></returns>
        public static WriteableBitmap LoadImage(BitArray imageBits)
        {
            var width = 0;
            var height = 0;

            if (imageBits.Length == 32768) //Full Screen Images (i.e. Flash Screen)
            {
                width = 128;
                height = 128;
            }
            else if (imageBits.Length == 4096)
            {
                width = 64;
                height = 32;
            }
            else if (imageBits.Length == 16384) //Thumbnail for Main Menu
            {
                width = 128;
                height = 64;
            }
            else if (imageBits.Length == 9216) //Shape, after it is selected
            {
                width = 96;
                height = 48;
            }
            else if (imageBits.Length == 1600) //Thumbnail for Menus
            {
                width = 40;
                height = 20;
            }
            else if (imageBits.Length == 3072) //Icon like Battery low
            {
                width = 16;
                height = 96;
            }
            else
            {
                throw new Exception("Unsupported format");
            }

            //Return the image
            return GetImage(imageBits, width, height);
        }

        /// <summary>
        /// Gets an Image from a BitArray
        /// </summary>
        /// <param name="imageBits"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static WriteableBitmap GetImage(BitArray imageBits, int width, int height)
        {
            //For saving the 2BitPerPixel-Image to 1BytePerPixel-Image
            var img1bpp = new byte[imageBits.Length / 2];
            //Go through the Bits. Each 2 Bits are 1 Pixel. 00 = Black, 01 = dark gray, 10 = light gray, 11 = White
            for (int i = 0; i < imageBits.Count; i += 2)
            {
                var colorNumber = (2 * (imageBits.Get(i) ? 1 : 0)) + (imageBits.Get(i + 1) ? 1 : 0);
                switch (colorNumber)
                {
                    case 0:
                        img1bpp[i / 2] = 0;
                        break;
                    case 1:
                        img1bpp[i / 2] = 90;
                        break;
                    case 2:
                        img1bpp[i / 2] = 150;
                        break;
                    case 3:
                        img1bpp[i / 2] = 255;
                        break;
                }
            }

            //Create the image
            var img = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256);
            img.WritePixels(new Int32Rect(0, 0, img.PixelWidth, img.PixelHeight), img1bpp, img.PixelWidth, 0);

            return img;
        }

        /// <summary>
        /// Cycles Bit 1+8, 2+7, 3+6, 4+5 in a byte
        /// </summary>
        /// <param name="orderImageBits"></param>
        /// <returns></returns>
        public static BitArray OrderBits(BitArray orderImageBits)
        {
            //Go through the Bits and Shift them to the correct order for further handling
            for (int i = 0; i < orderImageBits.Count - 1; i += 8)
            {
                var byteBits = new bool[]
                {
                    orderImageBits.Get(i),
                    orderImageBits.Get(i+1),
                    orderImageBits.Get(i+2),
                    orderImageBits.Get(i+3),
                    orderImageBits.Get(i+4),
                    orderImageBits.Get(i+5),
                    orderImageBits.Get(i+6),
                    orderImageBits.Get(i+7)
                };

                orderImageBits.Set(i, byteBits[6]);
                orderImageBits.Set(i + 1, byteBits[7]);
                orderImageBits.Set(i + 2, byteBits[4]);
                orderImageBits.Set(i + 3, byteBits[5]);
                orderImageBits.Set(i + 4, byteBits[2]);
                orderImageBits.Set(i + 5, byteBits[3]);
                orderImageBits.Set(i + 6, byteBits[0]);
                orderImageBits.Set(i + 7, byteBits[1]);
            }

            return orderImageBits;
        }
    }
}
