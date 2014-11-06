using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Devices;
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Xna.Framework.Media;
using System.Windows.Media.Imaging;
using System.Threading;

namespace testtek
{

    public class My_ARGB
    {
        public int r;
        public int g;
        public int b;
        public int a;
        public static implicit operator My_ARGB(int ii)
        {
            try
            {
                My_ARGB mb = new My_ARGB();
                mb.a = ii >> 24;
                mb.r = (ii & 0x00ff0000) >> 16;
                mb.g = (ii & 0x0000ff00) >> 8;
                mb.b = (ii & 0x000000ff);
                return mb;
            }
            catch
            {
                return new My_ARGB();
            }
        }
        public static implicit operator int(My_ARGB mb)
        {
            try
            {
                return ((mb.a & 0xFF) << 24) | ((mb.r & 0xFF) << 16) | ((mb.g & 0xFF) << 8) | (mb.b & 0xFF); ;
            }
            catch
            {
                return ((0 & 0xFF) << 24) | ((0 & 0xFF) << 16) | ((0 & 0xFF) << 8) | (0 & 0xFF); ;
            }
        }

    }

    public class globalfff 
    {
        public int fff { get; set; }
    }

    public partial class MainPage : PhoneApplicationPage
    {
        // Конструктор
        PhotoCamera cam = new PhotoCamera();
        private static ManualResetEvent pauseFramesEvent = new ManualResetEvent(true);
        private WriteableBitmap wb;
        private Thread ARGBFramesThread;
        private bool pumpARGBFrames;
        int g1 = 0;
        int g2 = 0;
        globalfff type_filter = new globalfff();

        public MainPage()
        {
            InitializeComponent();
        }

        //Code for camera initialization event, and setting the source for the viewfinder
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {

            // Check to see if the camera is available on the phone.
            if ((PhotoCamera.IsCameraTypeSupported(CameraType.Primary) == true) ||
                 (PhotoCamera.IsCameraTypeSupported(CameraType.FrontFacing) == true))
            {
                // Initialize the default camera.
                cam = new Microsoft.Devices.PhotoCamera();

                //Event is fired when the PhotoCamera object has been initialized
                cam.Initialized += new EventHandler<Microsoft.Devices.CameraOperationCompletedEventArgs>(cam_Initialized);

                //Set the VideoBrush source to the camera
                viewfinderBrush.SetSource(cam);
            }
            else
            {
                // The camera is not supported on the phone.
                this.Dispatcher.BeginInvoke(delegate()
                {
                    // Write message.
                    txtDebug.Text = "A Camera is not available on this phone.";
                });

                // Disable UI.

                btnBorders.IsEnabled = false;
                btnOff.IsEnabled = false;
                btnColorSwap.IsEnabled = false;
            }
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (cam != null)
            {
                // Dispose of the camera to minimize power consumption and to expedite shutdown.
                cam.Dispose();

                // Release memory, ensure garbage collection.
                cam.Initialized -= cam_Initialized;
            }
        }

       

        //Update UI if initialization succeeds
        void cam_Initialized(object sender, Microsoft.Devices.CameraOperationCompletedEventArgs e)
        {
            if (e.Succeeded)
            {
                this.Dispatcher.BeginInvoke(delegate()
                {
                    txtDebug.Text = "Camera initialized";
                });

            }
        }

        // ARGB frame pump
        void PumpARGBFrames()
        {
            // Create capture buffer.
            int h = (int)cam.PreviewResolution.Height;
            int w = (int)cam.PreviewResolution.Width;
            int[] ARGBPx = new int[h*w];

            //try
            //{
                PhotoCamera phCam = (PhotoCamera)cam;

                while (pumpARGBFrames)
                {
                    pauseFramesEvent.WaitOne();

                    // Copies the current viewfinder frame into a buffer for further manipulation.
                    phCam.GetPreviewBufferArgb32(ARGBPx);
                    // Conversion to grayscale.

                    My_ARGB[,] temp = transponr(ARGBPx, h, w);
                    if (type_filter.fff == 1)
                    {
                        for (int i = 0; i < w; i++)
                        {
                            for (int j = 0; j < h; j++)
                            {
                                temp[i, j] = borders(temp[i, j]);
                            }
                        }

                        for (int i = 0; i < w; i++)
                        {
                            for (int j = h - 1; j >= 0; j--)
                            {
                                temp[i, j] = borders(temp[i, j]);
                            }
                        }

                        for (int i = 0; i < h; i++)
                        {
                            for (int j = 0; j < w; j++)
                            {
                                temp[j, i] = borders(temp[j, i]);
                            }
                        }

                        for (int i = 0; i < h; i++)
                        {
                            for (int j = w - 1; j >= 0; j--)
                            {
                                temp[j, i] = borders(temp[j, i]);
                            }
                        }
                    }
                    else if(type_filter.fff == 2)
                    {
                        for (int i = 0; i < w; i++)
                        {
                            for (int j = 0; j < h; j++)
                            {
                                temp[i, j] = swap_color(temp[i, j]);
                            }
                        }
                    }

                    int q = 0;
                    for (int i = 0; i < w; i++)
                    {
                        for (int j = 0; j < h; j++)
                        {
                            ARGBPx[q] = temp[i, j];
                            q++;
                        }
                    }

                    pauseFramesEvent.Reset();
                    Deployment.Current.Dispatcher.BeginInvoke(delegate()
                    {
                        // Copy to WriteableBitmap.
                        ARGBPx.CopyTo(wb.Pixels, 0);
                        wb.Invalidate();

                        pauseFramesEvent.Set();
                    });
                }

            //}
            //catch (Exception e)
            //{
            //    this.Dispatcher.BeginInvoke(delegate()
            //    {
            //        // Display error message.
            //        txtDebug.Text = e.Message;
            //    });
            //}
        }

        internal My_ARGB borders(My_ARGB color)
        {
            if ((color.r <= 100) && (color.g <= 100) && (color.b <= 100))
            {
                g2 = 0;
                g1++;
                if (g1 < 5)
                {
                    color.r = 100;
                    color.g = 230;
                    color.b = 70;
                }
            }
            else
            {
                g2++;
                if (g2 > 5)
                {
                    g1 = 0;
                }
            }
            return color;
        }

        internal My_ARGB swap_color(My_ARGB color)
        {
            if ((color.r <= 100) && (color.g <= 100) && (color.b <= 100))
            {              
                    color.r = 100;
                    color.g = 230;
                    color.b = 70;
            }

            return color;
        }

        My_ARGB[,] transponr(int[] x, int h, int w)
        {
            My_ARGB[,] y = new My_ARGB[w, h];
            int q = 0;
            for(int i =0; i<w;i++)
            {
                for (int j = 0; j < h; j++)
                {
                    y[i,j] = x[q];
                    q++;
                }
            }
            return y;
        }


        private void GrayOn_Clicked(object sender, RoutedEventArgs e)
        {
            type_filter.fff = 1;
            btnColorSwap.Visibility = Visibility.Collapsed;
            btnBorders.Visibility = Visibility.Collapsed;
            btnOff.Visibility = Visibility.Visible;
            MainImage.Visibility = Visibility.Visible;
            pumpARGBFrames = true;
            ARGBFramesThread = new System.Threading.Thread(PumpARGBFrames);

            wb = new WriteableBitmap((int)cam.PreviewResolution.Width, (int)cam.PreviewResolution.Height);
            this.MainImage.Source = wb;

            // Start pump.
            ARGBFramesThread.Start();
            this.Dispatcher.BeginInvoke(delegate()
            {
                txtDebug.Text = "ARGB to Grayscale";
            });
        }

        // Stop ARGB to grayscale pump.
        private void GrayOff_Clicked(object sender, RoutedEventArgs e)
        {
            btnColorSwap.Visibility = Visibility.Visible;
            btnBorders.Visibility = Visibility.Visible;
            btnOff.Visibility = Visibility.Collapsed;
            MainImage.Visibility = Visibility.Collapsed;
            pumpARGBFrames = false;

            this.Dispatcher.BeginInvoke(delegate()
            {
                txtDebug.Text = "";
            });
        }

        private void btnColorSwap_Click_1(object sender, RoutedEventArgs e)
        {
            btnColorSwap.Visibility = Visibility.Collapsed;
            btnBorders.Visibility = Visibility.Collapsed;
            btnOff.Visibility = Visibility.Visible;
            type_filter.fff = 2;
            MainImage.Visibility = Visibility.Visible;
            pumpARGBFrames = true;
            ARGBFramesThread = new System.Threading.Thread(PumpARGBFrames);

            wb = new WriteableBitmap((int)cam.PreviewResolution.Width, (int)cam.PreviewResolution.Height);
            this.MainImage.Source = wb;

            // Start pump.
            ARGBFramesThread.Start();
            this.Dispatcher.BeginInvoke(delegate()
            {
                txtDebug.Text = "ARGB to Grayscale";
            });
        }

     


        

    }
}