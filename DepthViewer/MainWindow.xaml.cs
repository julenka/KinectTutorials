using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using xn;
using System.Drawing;
using System.Drawing.Imaging;

namespace DepthViewer
{

    /// <summary>
    /// Step 1: draw a depth map on the depthImage picture in the app
    /// </summary>
    public partial class MainWindow : Window
    {
        private Context context;
        private DepthGenerator depth;
        private ImageGenerator rgb;
        private Bitmap bitmap;
        private int[] histogram;

        public MainWindow()
        {
            InitializeComponent();
        }

        private unsafe void UpdateRGB()
        {
            xn.ImageMetaData rgbMD = new xn.ImageMetaData();


            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);
            BitmapData data = this.bitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            this.rgb.GetMetaData(rgbMD);

            byte* pDepth = (byte*)this.rgb.GetImageMapPtr().ToPointer();

            // set pixels
            for (int y = 0; y < rgbMD.YRes; ++y)
            {
                byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;
                for (int x = 0; x < rgbMD.XRes; ++x, pDepth += 3, pDest += 3)
                {
                    byte r = *pDepth;// *pDepth;
                    byte g = *(pDepth + 1);
                    byte b = *(pDepth + 2);
                    pDest[0] = b;
                    pDest[1] = g;
                    pDest[2] = r;
                }
            }

            this.bitmap.UnlockBits(data);


            rgbImage.Source = Utils.getBitmapImage(bitmap);
        }
        private unsafe void UpdateDepth()
        {
            DepthMetaData depthMD = new DepthMetaData();


            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);
            BitmapData data = this.bitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            this.depth.GetMetaData(depthMD);

            CalcHist(depthMD);

            ushort* pDepth = (ushort*)this.depth.GetDepthMapPtr().ToPointer();

            // set pixels
            for (int y = 0; y < depthMD.YRes; ++y)
            {
                byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;
                for (int x = 0; x < depthMD.XRes; ++x, ++pDepth, pDest += 3)
                {
                    byte pixel = (byte)this.histogram[*pDepth];
                    pDest[0] = 0;
                    pDest[1] = pixel;
                    pDest[2] = pixel;
                }
            }

            this.bitmap.UnlockBits(data);


            depthImage.Source = Utils.getBitmapImage(bitmap);
        }


        private unsafe void UpdatePlane()
        {
            // Recalculate the normal to the plane
            normal = Utils.getNormal(p1, p2, p3);

            DepthMetaData depthMD = new DepthMetaData();
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);
            BitmapData data = this.bitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            this.depth.GetMetaData(depthMD);

            ushort* pDepth = (ushort*)this.depth.GetDepthMapPtr().ToPointer();

            // set pixels
            for (int y = 0; y < depthMD.YRes; ++y)
            {
                byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;
                for (int x = 0; x < depthMD.XRes; ++x, ++pDepth, pDest += 3)
                {
                    ushort z = *pDepth;
                    // see if this point is within 100 mm of the plane
                    Vector3D v = new Vector3D(x - p1.X, y - p1.Y, z - p1.Z);
                    double distance = Vector3D.DotProduct(normal, v);
                    if (Math.Abs(distance) < 100)
                    {
                        pDest[0] = 255;
                        pDest[1] = 255;
                        pDest[2] = 255;
                    }
                    else
                    {
                        pDest[0] = 0;
                        pDest[1] = 0;
                        pDest[2] = 0;
                    }

                }
            }

            this.bitmap.UnlockBits(data);


            planeImage.Source = Utils.getBitmapImage(bitmap);
        }

        private unsafe void CalcHist(DepthMetaData depthMD)
        {
            // reset
            for (int i = 0; i < this.histogram.Length; ++i)
                this.histogram[i] = 0;

            ushort* pDepth = (ushort*)depthMD.DepthMapPtr.ToPointer();

            int points = 0;
            for (int y = 0; y < depthMD.YRes; ++y)
            {
                for (int x = 0; x < depthMD.XRes; ++x, ++pDepth)
                {
                    ushort depthVal = *pDepth;
                    if (depthVal != 0)
                    {
                        this.histogram[depthVal]++;
                        points++;
                    }
                }
            }

            for (int i = 1; i < this.histogram.Length; i++)
            {
                this.histogram[i] += this.histogram[i - 1];
            }

            if (points > 0)
            {
                for (int i = 1; i < this.histogram.Length; i++)
                {
                    this.histogram[i] = (int)(256 * (1.0f - (this.histogram[i] / (float)points)));
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Step 1: initialize depth generator
            try
            {
                this.context = new Context(@"C:\Users\Julia\Documents\Visual Studio 2010\Projects\KinectTutorials\data\openniconfig.xml");
                this.depth = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
                this.rgb = context.FindExistingNode(NodeType.Image) as ImageGenerator;
                if (this.depth == null)
                    throw new Exception(@"Error in Data\openniconfig.xml. No depth node found.");
                if (this.rgb == null)
                    throw new Exception(@"Error in Data\openniconfig.xml. No rgb node found.");
                MapOutputMode mapMode = this.depth.GetMapOutputMode();
                this.histogram = new int[this.depth.GetDeviceMaxDepth()];
                this.bitmap = new Bitmap((int)mapMode.nXRes, (int)mapMode.nYRes, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            }
            catch (Exception ex)
            {
                ///
                /// - todo: proper error logging here
                /// 

                MessageBox.Show("Error initializing OpenNI.");
                MessageBox.Show(ex.Message);

                this.Close();
            }

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 30);
            dispatcherTimer.Start();
            Console.WriteLine("Finished loading");
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                this.context.WaitAndUpdateAll();
            }
            catch (Exception)
            {
            }
            UpdateDepth();
            UpdateRGB();
            UpdatePlane();

            PointsLabel.Content = String.Format("P1: ({0},{1},{2})\nP2: ({3},{4},{5})\nP3: ({6},{7},{8})", p1.X, p1.Y, p1.Z, p2.X, p2.Y, p2.Z, p3.X, p3.Y, p3.Z);
        }

    }
}
