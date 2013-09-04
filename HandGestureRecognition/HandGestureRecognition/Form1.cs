using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Emgu.CV.Structure;
using Emgu.CV;
using HandGestureRecognition.SkinDetector;
using System.Runtime.InteropServices;


namespace HandGestureRecognition
{
    public partial class Form1 : Form
    {
        
       
         private void button1_Click_1(object sender, EventArgs e)
        {
            imageBoxFrameGrabber.Show();
            imageBoxSkin.Show();
            label1.Hide();
                Application.Idle += new EventHandler(FrameGrabber);
                Application.Idle -= new EventHandler(FaceGrabber);
                Application.Idle -= new EventHandler(ColorGrabber);
               
        }
         private void button2_Click(object sender, EventArgs e)
         {
             imageBoxFrameGrabber.Hide();
             imageBoxSkin.Hide();
             label1.Hide();
             button2.ResumeLayout();
             Application.Idle -= new EventHandler(FrameGrabber);
             Application.Idle -= new EventHandler(FaceGrabber);
             Application.Idle -= new EventHandler(ColorGrabber);
         }

         private void button3_Click(object sender, EventArgs e)
         {
             imageBoxFrameGrabber.Show();
             imageBoxSkin.Show();
             label1.Hide();
             Application.Idle -= new EventHandler(FrameGrabber);
             Application.Idle -= new EventHandler(FaceGrabber);
             Application.Idle += new EventHandler(ColorGrabber);

         }

         private void button4_Click(object sender, EventArgs e)
         {

             imageBoxSkin.Show();
             imageBoxFrameGrabber.Hide();
             Application.Idle += new EventHandler(FaceGrabber);
             Application.Idle -= new EventHandler(FrameGrabber);
             Application.Idle -= new EventHandler(ColorGrabber);
         }

        IColorSkinDetector skinDetector;

        Image<Bgr, Byte> currentFrame;
        Image<Bgr, Byte> currentFrameCopy;
                
        Capture grabber;
        AdaptiveSkinDetector detector;
        
        int frameWidth;
        int frameHeight;
        
        Hsv hsv_min;
        Hsv hsv_max;
        Ycc YCrCb_min;
        Ycc YCrCb_max;
        
        Seq<Point> hull;
        Seq<Point> filteredHull;
        Seq<MCvConvexityDefect> defects;
        MCvConvexityDefect[] defectArray;
        Rectangle handRect;
        MCvBox2D box;
       // Ellipse ellip;
        int blue;
        private HaarCascade _face;
        private HaarCascade eyes;
        private HaarCascade reye;
        private HaarCascade leye;
              

        public Form1()
        {
           
            InitializeComponent();
            grabber = new Emgu.CV.Capture();          
            grabber.QueryFrame();
            frameWidth = grabber.Width;
            frameHeight = grabber.Height;            
            detector = new AdaptiveSkinDetector(1, AdaptiveSkinDetector.MorphingMethod.NONE);
            hsv_min = new Hsv(0, 45, 0); 
            hsv_max = new Hsv(20, 255, 255);            
            YCrCb_min = new Ycc(0, 131, 80);
            YCrCb_max = new Ycc(255, 185, 135);
            box = new MCvBox2D();
            // ellip = new Ellipse();
            _face = new HaarCascade("haarcascade_frontalface_alt_tree.xml");
            eyes = new HaarCascade("haarcascade_mcs_eyepair_big.xml");
            reye = new HaarCascade("haarcascade_mcs_lefteye.xml");
            leye = new HaarCascade("haarcascade_mcs_righteye.xml");
            label1.Hide();
        }

        private Stopwatch sw = new Stopwatch();

        void FrameGrabber(object sender, EventArgs e)
        {
            currentFrame = grabber.QueryFrame();
            if (currentFrame != null)
            {
                currentFrameCopy = currentFrame.Copy();
              
                skinDetector = new YCrCbSkinDetector(); 
                
                Image<Gray, Byte> skin = skinDetector.DetectSkin(currentFrameCopy,YCrCb_min,YCrCb_max);

                ExtractContourAndHull(skin);
                                
                DrawAndComputeFingersNum();

                imageBoxSkin.Image = skin;
                imageBoxFrameGrabber.Image = currentFrame;
            }
        }



        void ColorGrabber(object sender, EventArgs e)
        {
            currentFrame = grabber.QueryFrame();
            if (currentFrame != null)
            {
                currentFrameCopy = currentFrame.Copy();
     

                Image<Hsv, Byte> hsvimg = currentFrameCopy.Convert<Hsv, Byte>();
                Image<Gray, Byte>[] channels = hsvimg.Split();
                Image<Gray, Byte> imghue = channels[0];
                Image<Gray, Byte> huefilter = currentFrame.InRange(new Bgr(100,0,0),new Bgr(256,100,100));

                                    
                imageBoxSkin.Image = huefilter;
                imageBoxFrameGrabber.Image = currentFrame;
                ExtractContourAndHull(huefilter);
               // DrawAndComputeFingersNum();
                if(huefilter == currentFrame.InRange(new Bgr(100,0,0),new Bgr(256,100,100)) )
                { blue = 6; }

            }
        }

        void FaceGrabber(object sender, EventArgs e)
        {
           
            currentFrame = grabber.QueryFrame();
            if (currentFrame != null)
            {
                label1.Show();
                currentFrameCopy = currentFrame.Copy();

                Image<Bgr, Byte> frame = grabber.QueryFrame();
                Image<Gray, Byte> grayFrame = frame.Convert<Gray, Byte>();
                grayFrame._EqualizeHist();

                MCvAvgComp[][] facesDetected = grayFrame.DetectHaarCascade(_face, 1.1, 1, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT, new Size(20, 20));
                //  MCvAvgComp[][] lefteyeDeteced = grayFrame.DetectHaarCascade(leye, 1.1, 1, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT, new Size(20, 20));
                if (facesDetected[0].Length == 1)
                {

                    MCvAvgComp face = facesDetected[0][0];

                    #region on Face Metric Estimation --- based on empirical measuraments on a couple of photos ---  a really trivial heuristic

                    //// Our Region of interest where find eyes will start with a sample estimation using face metric
                    Int32 yCoordStartSearchEyes = face.rect.Top + (face.rect.Height * 3 / 11);
                    Point startingPointSearchEyes = new Point(face.rect.X, yCoordStartSearchEyes);
                    //Point endingPointSearchEyes = new Point((face.rect.X + face.rect.Width), yCoordStartSearchEyes);
                    //richTextBox1.Text = face.rect.Top.ToString();
                    Size searchEyesAreaSize = new Size(face.rect.Width * 5 / 2, (face.rect.Height * 2 / 5));
                    //Point lowerEyesPointOptimized = new Point(face.rect.X, yCoordStartSearchEyes + searchEyesAreaSize.Height);
                    Size eyeAreaSize = new Size(face.rect.Width / 2, (face.rect.Height * 2 / 9));
                    Point startingLeftEyePointOptimized = new Point(face.rect.X + face.rect.Width / 2, yCoordStartSearchEyes);

                    Rectangle possibleROI_eyes = new Rectangle(startingPointSearchEyes, searchEyesAreaSize);
                    Rectangle possibleROI_rightEye = new Rectangle(startingPointSearchEyes, eyeAreaSize);
                    Rectangle possibleROI_leftEye = new Rectangle(startingLeftEyePointOptimized, eyeAreaSize);

                    #endregion


                    int widthNav = (frame.Width / 10 * 2);
                    int heightNav = (frame.Height / 10 * 2);

                    Rectangle nav = new Rectangle(new Point(frame.Width / 25 - widthNav / 2, frame.Height / 2 - heightNav / 2), searchEyesAreaSize);
                    frame.Draw(nav, new Bgr(Color.Lavender), 3);
                    Point cursor = new Point(face.rect.X + searchEyesAreaSize.Width / 2, yCoordStartSearchEyes);


                    grayFrame.ROI = possibleROI_eyes;
                    MCvAvgComp[][] EyesDetected = grayFrame.DetectHaarCascade(eyes, 1.15, 3, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));
                    grayFrame.ROI = Rectangle.Empty;

                    if (EyesDetected[0].Length != 0)
                    {
                        sw.Reset();
                        frame.Draw(face.rect, new Bgr(Color.Yellow), 1);

                        foreach (MCvAvgComp eye in EyesDetected[0])
                        {
                            Rectangle eyeRect = eye.rect;
                            eyeRect.Offset(possibleROI_eyes.X, possibleROI_eyes.Y);
                            grayFrame.ROI = eyeRect;
                            frame.Draw(eyeRect, new Bgr(Color.DarkSeaGreen), 2);
                            frame.Draw(possibleROI_eyes, new Bgr(Color.DeepPink), 2);

                            if (nav.Left < cursor.X && cursor.X < (nav.Left + 20 * nav.Width) && nav.Top < cursor.Y && cursor.Y < nav.Top + 3 * nav.Height)
                            {
                                LineSegment2D CursorDraw = new LineSegment2D(cursor, new Point(cursor.X, cursor.Y + 1));
                                frame.Draw(CursorDraw, new Bgr(Color.White), 3);
                                int right = (frame.Width * (cursor.X - nav.Right)) / nav.Width;
                                int xCoord = (frame.Width * (cursor.X - nav.Left)) / nav.Width + 2 * right;
                                int yCoord = (frame.Width * (cursor.Y - nav.Top)) / nav.Height;
                              Cursor.Position = new Point(xCoord, yCoord);
                            }
                        }
                    }
                    grayFrame.ROI = possibleROI_leftEye;
                    MCvAvgComp[][] leftEyesDetected = grayFrame.DetectHaarCascade(leye, 1.15, 3, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));
                    grayFrame.ROI = Rectangle.Empty;

                    grayFrame.ROI = possibleROI_rightEye;
                    MCvAvgComp[][] rightEyesDetected = grayFrame.DetectHaarCascade(reye, 1.15, 3, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));
                    grayFrame.ROI = Rectangle.Empty;

                    //If we are able to find eyes inside the possible face, it should be a face, maybe we find also a couple of eyes
                    if (leftEyesDetected[0].Length != 0 && rightEyesDetected[0].Length != 0)
                    {
                        sw.Stop();
                        //draw the face
                        frame.Draw(face.rect, new Bgr(Color.Violet), 2);
                        grayFrame.ROI = possibleROI_leftEye;
                        grayFrame.ROI = Rectangle.Empty;
                        grayFrame.ROI = possibleROI_rightEye;
                        grayFrame.ROI = Rectangle.Empty;
                        

                    }
                    else

                        if (leftEyesDetected[0].Length == 0)
                        {

                            timer1.Enabled = true;

                            sw.Start();
                            //timer1_Tick();
                            if (label1.Text == "0:00:03")
                            {

                                DoMouseClick();
                                sw.Reset();

                            }

                        }



                    imageBoxSkin.Image = frame;

                }

            }
          
        }


        private void ExtractContourAndHull(Image<Gray, byte> skin)
        {
            using (MemStorage storage = new MemStorage())
            {

                Contour<Point> contours = skin.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST, storage);
                Contour<Point> biggestContour = null;

                Double Result1 = 0;
                Double Result2 = 0;
                while (contours != null)
                {
                    Result1 = contours.Area;
                    if (Result1 > Result2)
                    {
                        Result2 = Result1;
                        biggestContour = contours;
                    }
                    contours = contours.HNext;
                }
                
                if (biggestContour != null)
                {
                   // currentFrame.Draw(biggestContour, new Bgr(Color.Black), 2); 
                    Contour<Point> currentContour = biggestContour.ApproxPoly(biggestContour.Perimeter * 0.0025, storage);
                    //currentFrame.Draw(currentContour, new Bgr(Color.Red), 2);
                    biggestContour = currentContour;
                

                    hull = biggestContour.GetConvexHull(Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);
                    box = biggestContour.GetMinAreaRect();
                    PointF[] points = box.GetVertices();
                    handRect = box.MinAreaRect();
                    //int xx = (handRect.Width) / 2;
                    //int yy = (handRect.Height) / 2;   
                    currentFrame.Draw(handRect, new Bgr(200, 0, 0), 1);
                    
                    // currentFrame.Draw(new CircleF(new PointF(xx, yy), 3), new Bgr(200, 125, 75), 2);
                    Point[] ps = new Point[points.Length];
                    for (int i = 0; i < points.Length; i++)
                        ps[i] = new Point((int)points[i].X, (int)points[i].Y);

                    currentFrame.DrawPolyline(hull.ToArray(), true, new Bgr(200, 125, 75), 2);
                    currentFrame.Draw(new CircleF(new PointF(box.center.X, box.center.Y), 3), new Bgr(200, 125, 75), 2);
                   // currentFrame.Draw(new CircleF(new PointF(handRect.center.X, handRect.center.Y), 3), new Bgr(200, 125, 75), 2);


                    //ellip.MCvBox2D= CvInvoke.cvFitEllipse2(biggestContour.Ptr);
                    //currentFrame.Draw(new Ellipse(ellip.MCvBox2D), new Bgr(Color.LavenderBlush), 3);

                   // PointF center;
                    // float radius;
                    //CvInvoke.cvMinEnclosingCircle(biggestContour.Ptr, out  center, out  radius);
                    //currentFrame.Draw(new CircleF(center, radius), new Bgr(Color.Gold), 2);

                    //currentFrame.Draw(new CircleF(new PointF(ellip.MCvBox2D.center.X, ellip.MCvBox2D.center.Y), 3), new Bgr(100, 25, 55), 2);
                    //currentFrame.Draw(ellip, new Bgr(Color.DeepPink), 2);

                    //CvInvoke.cvEllipse(currentFrame, new Point((int)ellip.MCvBox2D.center.X, (int)ellip.MCvBox2D.center.Y), new System.Drawing.Size((int)ellip.MCvBox2D.size.Width, (int)ellip.MCvBox2D.size.Height), ellip.MCvBox2D.angle, 0, 360, new MCvScalar(120, 233, 88), 1, Emgu.CV.CvEnum.LINE_TYPE.EIGHT_CONNECTED, 0);
                    //currentFrame.Draw(new Ellipse(new PointF(box.center.X, box.center.Y), new SizeF(box.size.Height, box.size.Width), box.angle), new Bgr(0, 0, 0), 2);


                    filteredHull = new Seq<Point>(storage);
                    for (int i = 0; i < hull.Total; i++)
                    {
                        if (Math.Sqrt(Math.Pow(hull[i].X - hull[i + 1].X, 2) + Math.Pow(hull[i].Y - hull[i + 1].Y, 2)) > box.size.Width / 10)
                        {
                            filteredHull.Push(hull[i]);
                        }
                    }

                    defects = biggestContour.GetConvexityDefacts(storage, Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);

                    defectArray = defects.ToArray();
                }
                            MCvMoments moment = new MCvMoments();               // a new MCvMoments object

            try
            {
                moment = biggestContour.GetMoments();           // Moments of biggestContour
            }
            catch (NullReferenceException)
            {
                
            }
            int fingerNum = 0;
            CvInvoke.cvMoments(biggestContour, ref moment, 0);
            
            double m_00 = CvInvoke.cvGetSpatialMoment(ref moment, 0, 0);
            double m_10 = CvInvoke.cvGetSpatialMoment(ref moment, 1, 0);
            double m_01 = CvInvoke.cvGetSpatialMoment(ref moment, 0, 1);

            int current_X = Convert.ToInt32(m_10 / m_00) / 10;      // X location of centre of contour              
            int current_Y = Convert.ToInt32(m_01 / m_00) / 10;      // Y location of center of contour
            

             if (fingerNum == 1 || fingerNum == 0 || blue == 6)
             {
                 Cursor.Position = new Point(current_X * 10, current_Y * 10);
             }
             //Leave the cursor where it was and Do mouse click, if finger count >= 3
           
                }
}
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData,
           int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;          // mouse left button pressed 
        private const int MOUSEEVENTF_LEFTUP = 0x04;            // mouse left button unpressed
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;         // mouse right button pressed
        private const int MOUSEEVENTF_RIGHTUP = 0x10;           // mouse right button unpressed

        //this function will click the mouse using the parameters assigned to it
        public void DoMouseClick()
        {
            //Call the imported function with the cursor's current position
            uint X = Convert.ToUInt32( Cursor.Position.X);
            uint Y = Convert.ToUInt32( Cursor.Position.Y);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }         
   
        private void DrawAndComputeFingersNum()
        {
            int fingerNum = 0;

            #region hull drawing
            //for (int i = 0; i < filteredHull.Total; i++)
            //{
            //    PointF hullPoint = new PointF((float)filteredHull[i].X,
            //                                  (float)filteredHull[i].Y);
            //    CircleF hullCircle = new CircleF(hullPoint, 4);
            //    currentFrame.Draw(hullCircle, new Bgr(Color.Aquamarine), 2);
            //}
            #endregion

            #region defects drawing
            for (int i = 0; i < defects.Total; i++)
            {
                PointF startPoint = new PointF((float)defectArray[i].StartPoint.X,
                                                (float)defectArray[i].StartPoint.Y);

                PointF depthPoint = new PointF((float)defectArray[i].DepthPoint.X,
                                                (float)defectArray[i].DepthPoint.Y);

                PointF endPoint = new PointF((float)defectArray[i].EndPoint.X,
                                                (float)defectArray[i].EndPoint.Y);

                LineSegment2D startDepthLine = new LineSegment2D(defectArray[i].StartPoint, defectArray[i].DepthPoint);

                LineSegment2D depthEndLine = new LineSegment2D(defectArray[i].DepthPoint, defectArray[i].EndPoint);

                CircleF startCircle = new CircleF(startPoint, 5f);

                CircleF depthCircle = new CircleF(depthPoint, 5f);

                CircleF endCircle = new CircleF(endPoint, 5f);

                //Custom heuristic based on some experiment, double check it before use
                if ((startCircle.Center.Y < box.center.Y || depthCircle.Center.Y < box.center.Y) && (startCircle.Center.Y < depthCircle.Center.Y) && (Math.Sqrt(Math.Pow(startCircle.Center.X - depthCircle.Center.X, 2) + Math.Pow(startCircle.Center.Y - depthCircle.Center.Y, 2)) > box.size.Height / 6.5))
                {
                    fingerNum++;
                    currentFrame.Draw(startDepthLine, new Bgr(Color.Green), 2);
                    //currentFrame.Draw(depthEndLine, new Bgr(Color.Magenta), 2);
                }


                currentFrame.Draw(startCircle, new Bgr(Color.Red), 2);
                currentFrame.Draw(depthCircle, new Bgr(Color.Yellow), 5);
                //currentFrame.Draw(endCircle, new Bgr(Color.DarkBlue), 4);
            }
            #endregion
            
            if (fingerNum == 3)
            {
                // function clicks mouse left button
                DoMouseClick();
            }

            if (fingerNum == 5) 
            {
                //opens my computer
                Process.Start("::{20d04fe0-3aea-1069-a2d8-08002b30309d}");
            }

            MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_DUPLEX, 5d, 5d);
            currentFrame.Draw(fingerNum.ToString(), ref font, new Point(50, 150), new Bgr(Color.White));
         }
        private void imageBoxFrameGrabber_Click(object sender, EventArgs e)
        {

        }

              
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int hrs = sw.Elapsed.Hours, mins = sw.Elapsed.Minutes, secs = sw.Elapsed.Seconds;

            label1.Text = hrs + ":";
            if (mins < 10)
                label1.Text += "0" + mins + ":";
            else
                label1.Text += mins + ":";
            if (secs < 10)

                label1.Text += "0" + secs;
            else
                label1.Text += secs;
           
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
                                        
    }
}