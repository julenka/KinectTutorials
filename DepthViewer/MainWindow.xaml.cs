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
using System.Windows.Threading;
using System.IO;

/// This is a very basic class which shows a depth view from the Kinect.
/// It uses the OpenNI framework to get the depth image and then dispalys
/// the depth image on an image in the frame. 
/// 
/// //////////////////// Important /////////////////////////////////////
/// Make sure to add ../lib/OpenNI.dll to your list of references otherwise
/// the program won't build. For instructions on how to install the kinect
/// drivers and OpenNI framework used in this program visit 
/// http://www.codingbeta.com/?p=10
/// 
/// Also, remember to set the "allow unsafe code" property to true in project properties.
/// by Julia Schwarz
/// http://www.codingbeta.com
/// http://juliaschwarz.net
namespace DepthViewer
{

    public partial class MainWindow : Window
    {
        #region Member Variables
        private Context context;                            // The OpenNI context used for most OpenNI-related operations
        private DepthGenerator depth;                       // This will generate the depth image for you
        private Bitmap bitmap;                              // We will copy the depth image to this bitmap
        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This method updates the image on the MainWindow page with the latest depth image.
        /// </summary>
        private unsafe void UpdateDepth()
        {
            // Get information about the depth image
            DepthMetaData depthMD = new DepthMetaData();

            // Lock the bitmap we will be copying to just in case. This will also give us a pointer to the bitmap.
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);
            BitmapData data = this.bitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            
            depth.GetMetaData(depthMD);

            // This will point to our depth image
            ushort* pDepth = (ushort*)this.depth.GetDepthMapPtr().ToPointer();

            // Go over the depth image and set the bitmap we're copying to based on our depth value.
            for (int y = 0; y < depthMD.YRes; ++y)
            {
                byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;
                for (int x = 0; x < depthMD.XRes; ++x, ++pDepth, pDest += 3)
                {
                    // Change the color of the bitmap based on the depth value. You can make this
                    // whatever you want, my particular version is not that pretty.
                    pDest[0] = (byte)(*pDepth >> 2);
                    pDest[1] = (byte)(*pDepth >> 3);
                    pDest[2] = (byte)(*pDepth >> 4);
                }
            }

            this.bitmap.UnlockBits(data);

            // Update the image to have the bitmap image we just copied
            image1.Source = getBitmapImage(bitmap);
        }

        /// <summary>
        /// This method gets executed when the window loads. In it, we initialize our connection with Kinect
        /// and set up the timer which will update our depth image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize the context from the configuration file
                this.context = new Context(@"..\..\data\openniconfig.xml");
                // Get the depth generator from the config file.
                this.depth = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
                if (this.depth == null)
                    throw new Exception(@"Error in Data\openniconfig.xml. No depth node found.");
                MapOutputMode mapMode = this.depth.GetMapOutputMode();
                this.bitmap = new Bitmap((int)mapMode.nXRes, (int)mapMode.nYRes, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing OpenNI.");
                MessageBox.Show(ex.Message);
                this.Close();
            }

            // Set the timer to update teh depth image every 10 ms.
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            dispatcherTimer.Start();
            Console.WriteLine("Finished loading");
        }

        /// <summary>
        /// This method gets executed every time the timer ticks, which is every 10 ms.
        /// In it we update the depth image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        }

        /// This method takes in a bitmap and returns
        /// a BitmapImage which can be used to set the image source
        /// of an Image object. It is an annoying but necessary method to correctly
        /// display the depth image.
        public static BitmapImage getBitmapImage(Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            bi.StreamSource = ms;
            bi.EndInit();
            return bi;
        }
    }
}
