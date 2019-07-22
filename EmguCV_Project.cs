using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.Cuda;
using Emgu.CV.Features2D;

namespace CannyVideo
{
    public partial class EmguCV_Project : Form
    {
        //Video setup
        VideoCapture capWebcam;
        bool blnCapturingInProcess = false;
        int threshold1;
        int threshold2;
        Mat imgOriginal;
        Image<Bgr, byte> img;
        Image<Gray, byte> imgGrayScale;

        //Cal distance
        private double c_val;
        private double m_val;
        double _width;
        double _height;
        
        //Crop image
        Rectangle rect;
        Point StartLocation;
        Point EndLcation;
        bool IsMouseDown = false;

        public EmguCV_Project()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                capWebcam = new VideoCapture(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("unable to read from webcam, error: " + Environment.NewLine + Environment.NewLine +
                                ex.Message + Environment.NewLine + Environment.NewLine +
                                "exiting program");
                Environment.Exit(0);
                return;
            }
            Application.Idle += processFrameAndUpdateGUI;
            blnCapturingInProcess = true;
        }

        private double FindDistanceA(int _pixelA)
        {
            double del_y = 2.6;
            double del_x = 99;
            m_val = del_y / del_x;
            c_val = del_y;

            _width = m_val * (_pixelA + c_val);
            return _width;
        }

        private double FindDistanceB(int _pixelB)
        {
            double del_y = 2.6;
            double del_x = 99;
            m_val = del_y / del_x;
            c_val = del_y;

            _height = m_val * (_pixelB + c_val);
            return _height;
        }

        private void processFrameAndUpdateGUI(object sender, EventArgs e)
        {
            threshold1 = Convert.ToInt32(numericUpDown1.Value);
            threshold2 = Convert.ToInt32(numericUpDown2.Value);
            
            imgOriginal = capWebcam.QueryFrame();

            img = imgOriginal.ToImage<Bgr, byte>();
            imgGrayScale = img.Convert<Gray, byte>().ThresholdBinary(new Gray(threshold1), new Gray(threshold2));

            // ----------------------Find Canny Edge Detaction-------------------------------
            //Mat imgGrayScale = new Mat(imgOriginal.Size, DepthType.Cv8U, 1);
            //Mat imgBlurred = new Mat(imgOriginal.Size, DepthType.Cv8U, 1);
            //Mat imgCanny = new Mat(imgOriginal.Size, DepthType.Cv8U, 1);
            //CvInvoke.CvtColor(imgOriginal, imgGrayScale, ColorConversion.Bgr2Gray);
            //CvInvoke.GaussianBlur(imgGrayScale, imgBlurred, new Size(5, 5), 1.5);
            //CvInvoke.Canny(imgBlurred, imgCanny, threshold1, threshold2);
            //---------------------------------End--------------------------------------------

            Emgu.CV.Util.VectorOfVectorOfPoint contours = new Emgu.CV.Util.VectorOfVectorOfPoint();

            Mat hier = new Mat();

            CvInvoke.FindContours(imgGrayScale, contours, hier, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
            //CvInvoke.DrawContours(imgOriginal, contours, -1, new MCvScalar(255, 0, 0),5);        

            for (int i = 0; i < contours.Size; i++) //Loop of Contour size
            {
                double perimeter = CvInvoke.ArcLength(contours[i], true);
                VectorOfPoint approx = new VectorOfPoint();
                CvInvoke.ApproxPolyDP(contours[i], approx, 0.04 * perimeter, true);

                //------------------Center Point of Shape (Centroid of Object)------------------//
                // x and y are centroid of object
                var moment = CvInvoke.Moments(contours[i]);
                int x = (int)(moment.M10 / moment.M00);
                int y = (int)(moment.M01 / moment.M00);

                rect = CvInvoke.BoundingRectangle(contours[i]);
                double ar = (double)(rect.Width / rect.Height);

                label4.Text = (contours.Size).ToString();

                if (approx.Size == 3) //The contour has 3 vertices.
                {
                    //CvInvoke.PutText(imgOriginal,"Triangle",new Point(x,y),Emgu.CV.CvEnum.FontFace.HersheySimplex,0.5, new MCvScalar(255,0,0),4);                  
                }

                if (approx.Size == 4) //The contour has 4 vertices.
                {
                                      
                    if ( (ar>=0.95) && (ar<=1.05) )
                    {
                        //CvInvoke.PutText(imgOriginal, "Square", new Point(x, y), Emgu.CV.CvEnum.FontFace.HersheySimplex, 1, new MCvScalar(255, 0, 0), 3);
                    }
                    else
                    {
                        //CvInvoke.PutText(imgOriginal, "Rectangle", new Point(x, y), Emgu.CV.CvEnum.FontFace.HersheySimplex, 1, new MCvScalar(255, 0, 0), 3);
                    }
                }

                if (approx.Size > 5) //The contour has 5 vertices.
                {
                    //CvInvoke.PutText(imgOriginal, "Circle", new Point(x + 100, y), Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.5, new MCvScalar(255, 0, 0), 3);
                }

                CvInvoke.Rectangle(imgOriginal, rect, new MCvScalar(0, 255, 0), 3);

                FindDistanceA(rect.Width);
                FindDistanceB(rect.Height);

                CvInvoke.PutText(imgOriginal, "Width:" + _width.ToString("n2") + "cm", new Point(x, (y-rect.Height/2)-2), Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.70, new MCvScalar(0, 0, 255), 2);
                CvInvoke.PutText(imgOriginal, "Height:" + _height.ToString("n2") + "cm", new Point((x+rect.Width/2)+2, y), Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.70, new MCvScalar(0, 0, 255), 2);
                
            }
            
            imageBox1.Image = imgOriginal;
            imageBox2.Image = imgGrayScale;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (blnCapturingInProcess == true)
            {
                Application.Idle -= processFrameAndUpdateGUI;
                blnCapturingInProcess = false;
                button1.Text = " Resume ";
            }
            else
            {
                Application.Idle += processFrameAndUpdateGUI;
                blnCapturingInProcess = true;
                button1.Text = " Pause ";
            }
        }

        private void ImageBox1_MouseDown(object sender, MouseEventArgs e)
        {
            Point StartLocation;
            Point EndLcation;
            bool IsMouseDown = false;
        }

        private void ImageBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseDown == true)
            {
                EndLcation = e.Location;
                imageBox1.Invalidate();
            }
        }

        private void ImageBox1_Paint(object sender, PaintEventArgs e)
        {
            if (rect != null)
            {
                e.Graphics.DrawRectangle(Pens.AliceBlue, GetRectangle());
            }
        }

        private Rectangle GetRectangle()
        {
            rect = new Rectangle();
            rect.X = Math.Min(StartLocation.X, EndLcation.X);
            rect.Y = Math.Min(StartLocation.Y, EndLcation.Y);
            rect.Width = Math.Abs(StartLocation.X - EndLcation.X);
            rect.Height = Math.Abs(StartLocation.Y - EndLcation.Y);

            return rect;
        }

        private void ImageBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (IsMouseDown == true)
            {
                EndLcation = e.Location;
                IsMouseDown = false;
                if (rect != null)
                {
                    img.ROI = rect;
                    Image<Bgr, byte> temp = img.CopyBlank();
                    img.CopyTo(temp);
                    img.ROI = Rectangle.Empty;
                    imageBox3.Image = temp;
                }
            }
        }
    }
}
