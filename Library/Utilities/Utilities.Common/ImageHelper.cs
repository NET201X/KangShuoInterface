using System.Drawing;
using System.Drawing.Drawing2D;

namespace Utilities.Common
{

    public class ImageHelper
    {
        public Bitmap bmpobj;
        public ImageHelper(Bitmap pic)
        {
            bmpobj = new Bitmap(pic);    //转换为Format32bppRgb
        }

        /// <summary>
        /// 根据RGB，计算灰度值
        /// </summary>
        /// <param name="posClr">Color值</param>
        /// <returns>灰度值，整型</returns>
        private int GetGrayNumColor(System.Drawing.Color posClr)
        {
            return (posClr.R * 19595 + posClr.G * 38469 + posClr.B * 7472) >> 16;
        }

        /// <summary>
        /// 灰度转换
        /// </summary>
        public Bitmap GrayByPixels()
        {
            for (int i = 0; i < bmpobj.Height; i++)
            {
                for (int j = 0; j < bmpobj.Width; j++)
                {
                    int tmpValue = GetGrayNumColor(bmpobj.GetPixel(j, i));
                    bmpobj.SetPixel(j, i, Color.FromArgb(tmpValue, tmpValue, tmpValue));
                }
            }
            return bmpobj;
        }

        /// <summary>
        /// 去掉噪点
        /// </summary>
        /// <param name="dgGrayValue"></param>
        /// <param name="MaxNearPoints"></param>
        public Bitmap ClearNoise(int dgGrayValue, int MaxNearPoints)
        {

            Color piexl;
            int nearDots = 0;
            //逐点判断
            for (int i = 0; i < bmpobj.Width; i++)
                for (int j = 0; j < bmpobj.Height; j++)
                {
                    piexl = bmpobj.GetPixel(i, j);
                    if (piexl.R < dgGrayValue)
                    {
                        nearDots = 0;
                        //判断周围8个点是否全为空
                        if (i == 0 || i == bmpobj.Width - 1 || j == 0 || j == bmpobj.Height - 1)  //边框全去掉
                        {
                            bmpobj.SetPixel(i, j, Color.FromArgb(255, 255, 255));
                        }
                        else
                        {
                            if (bmpobj.GetPixel(i - 1, j - 1).R < dgGrayValue) nearDots++;
                            if (bmpobj.GetPixel(i, j - 1).R < dgGrayValue) nearDots++;
                            if (bmpobj.GetPixel(i + 1, j - 1).R < dgGrayValue) nearDots++;
                            if (bmpobj.GetPixel(i - 1, j).R < dgGrayValue) nearDots++;
                            if (bmpobj.GetPixel(i + 1, j).R < dgGrayValue) nearDots++;
                            if (bmpobj.GetPixel(i - 1, j + 1).R < dgGrayValue) nearDots++;
                            if (bmpobj.GetPixel(i, j + 1).R < dgGrayValue) nearDots++;
                            if (bmpobj.GetPixel(i + 1, j + 1).R < dgGrayValue) nearDots++;
                        }

                        if (nearDots < MaxNearPoints)
                            bmpobj.SetPixel(i, j, Color.FromArgb(255, 255, 255));   //去掉单点 && 粗细小3邻边点
                    }
                    else  //背景
                        bmpobj.SetPixel(i, j, Color.FromArgb(255, 255, 255));
                }
            return bmpobj;

        }
        /// <summary>
        /// 扭曲图片校正
        /// </summary>
        public Bitmap ReSetBitMap()
        {
            Graphics g = Graphics.FromImage(bmpobj);
            Matrix X = new Matrix();
            X.Shear((float)0.16666666667, 0);   //  2/12
            g.Transform = X;
            Rectangle cloneRect = new Rectangle(0, 0, bmpobj.Width, bmpobj.Height);
            Bitmap tmpBmp = bmpobj.Clone(cloneRect, bmpobj.PixelFormat);
            g.DrawImage(tmpBmp,
                new Rectangle(0, 0, bmpobj.Width, bmpobj.Height),
                 0, 0, tmpBmp.Width,
                 tmpBmp.Height,
                 GraphicsUnit.Pixel);

            return tmpBmp;
        }

    }
}
