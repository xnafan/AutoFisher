using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WindowsInput;

namespace AutoFisher
{
    public partial class Form1 : Form
    {
        #region Variables and properties
        List<bool> _latestStates = new List<bool>();
        SolidBrush _transparentBrush = new SolidBrush(Color.Fuchsia);
        private int _msTickInterval = 150;
        int _ticksBeforeRightClick = 0;
        int _stateBorderThickness = 2;
        int _maxStates = 10;
        Rectangle _innerRectangle;
        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        private InputSimulator _inputSimulator = new InputSimulator();
        public enum State { NotFishing, Fishing }
        private State _currentState;
        StringBuilder _builder = new StringBuilder();
        public State CurrentState
        {
            set
            {
                _currentState = value;
                Invalidate();
                _latestStates.Clear();
                this.Text = $"{AppName} v.{version.Major}.{version.Minor} [" + (CurrentState == State.Fishing ? "fishing" : "not fishing") + "]";
            }
            get { return _currentState; }
        }

        private string AppName { get; set; } = "Minecraft automatic fisher";
        #endregion

        #region Form initialization and events
        public Form1() => InitializeComponent();
        private void Form1_Load(object sender, EventArgs e)
        {
            tmrScreenshot.Tick += TmrScreenshot_Tick;
            tmrScreenshot.Interval = _msTickInterval;
            PlaceWindowAboveCenter();
        }
        private void Form1_Shown(object sender, EventArgs e)
        {
            Thread.Sleep(1000);
            ProcessFocuser.SetFocusOnProcessIfExists("minecraft");
            Thread.Sleep(800);
            PressEscape();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            var brushToUse = CurrentState == State.Fishing ? Brushes.LawnGreen : Brushes.Red;
            e.Graphics.FillRectangle(brushToUse, ClientRectangle);
            e.Graphics.FillRectangle(_transparentBrush, GetTransparentInnerRectangleSize());
        } 
        #endregion

        private void PlaceWindowAboveCenter()
        {
            var screen = Screen.FromControl(this).Bounds;
            Left = (screen.Width - this.Width) / 2;
            Top = (int)(screen.Height / 2 - this.Height) - 20;
        }
        private void RightClick()
        {
            _inputSimulator.Mouse.RightButtonClick();
        }
        private void PressEscape()
        {
            _inputSimulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.ESCAPE);
        }
        private void TmrScreenshot_Tick(object sender, EventArgs e)
        {
            PrintStates();
            if (_ticksBeforeRightClick >= 0)
            {
                _ticksBeforeRightClick--;
            }
            if (_ticksBeforeRightClick == 0)
            {
                RightClick();
                _latestStates.Clear();
            }

            tmrScreenshot.Enabled = false;
            _latestStates.Add(GrabAndAnalyzeScreenshotUnsafe());

            if (CurrentState == State.Fishing && CompareWithLatestStates(false, 1))
            {
                RightClick();

                CurrentState = State.NotFishing;

                _ticksBeforeRightClick = 5;
            }
            else if (CompareWithLatestStates(true, 5))
            {
                CurrentState = State.Fishing;
            }

            tmrScreenshot.Enabled = true;
            while (_latestStates.Count > _maxStates)
            {
                _latestStates.RemoveAt(0);
            }
        }
        private void PrintStates()
        {
            _builder.Clear();
            foreach (var state in _latestStates)
            {
                _builder.Append(state ? "T," : "F,");
            }
            Debug.Print(_builder.ToString());
        }
        private bool PixelIsRed(Color pixel) => pixel.R > 150 && pixel.G < 50 && pixel.B < 50;
        private bool PixelIsBlue(Color pixel)
        {
            float GreenBlueAverage = (pixel.B + pixel.G) / 2;
            return GreenBlueAverage / (pixel.R + 1) > 1.3f;
        }
        private Bitmap CaptureInternal()
        {
            var innerRectangle = GetTransparentInnerRectangleSize();
            innerRectangle.Location = PointToScreen(innerRectangle.Location);
            Bitmap bmp = new Bitmap(innerRectangle.Width, innerRectangle.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(innerRectangle.Left, innerRectangle.Top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
                //bmp.Save(@"c:\temp\screenshot.png");
            }
            return bmp;
        }
        private Rectangle GetTransparentInnerRectangleSize()
        {
            var innerRectangle = ClientRectangle;
            innerRectangle.Inflate(-_stateBorderThickness * 2, -_stateBorderThickness * 2);
            return innerRectangle;
        }
        public Rectangle GetClientAreaRectangle()
        {
            // Get the top-left corner of the client area in screen coordinates
            Point clientTopLeft = PointToScreen(new Point(0, 0));

            // Get the client area's width and height
            Size clientSize = ClientSize;

            // Create and return the rectangle
            return new Rectangle(clientTopLeft, clientSize);
        }
        private bool CompareWithLatestStates(bool value, int numberOfConsecutiveStatesNeeded)
        {
            if (_latestStates.Count < numberOfConsecutiveStatesNeeded) { return false; }
            for (int i = 0; i < numberOfConsecutiveStatesNeeded; i++)
            {
                if (_latestStates[_latestStates.Count - i - 1] != value)
                {
                    return false;
                }
            }
            return true;
        }
        unsafe bool GrabAndAnalyzeScreenshotUnsafe()
        {
            using (var image = CaptureInternal())
            {
                BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                int bytesPerPixel = 3;

                byte* scan0 = (byte*)imageData.Scan0.ToPointer();
                int stride = imageData.Stride;
                var step = 4;
                bool redFound = false;
                int blueCount = 0;

                bool looksToBeFishing = false;

                int totalPixelsToCheck = (int)(((float)image.Width / step) * ((float)image.Height / step));
                for (int y = 0; y < imageData.Height; y += step)
                {
                    if (y < _stateBorderThickness || y > imageData.Height - _stateBorderThickness) { continue; }
                    byte* row = scan0 + (y * stride);
                    for (int x = 0; x < imageData.Width; x += step)
                    {
                        if (x < _stateBorderThickness || x > imageData.Width - _stateBorderThickness) { continue; }
                        // Watch out for actual order (BGR)!
                        int bIndex = x * bytesPerPixel;
                        int gIndex = bIndex + 1;
                        int rIndex = gIndex + 1;

                        byte pixelR = row[rIndex];
                        byte pixelG = row[gIndex];
                        byte pixelB = row[bIndex];

                        var pixel = Color.FromArgb(pixelR, pixelG, pixelB);

                        if (PixelIsBlue(pixel)) { blueCount++; }
                        else if (PixelIsRed(pixel))
                        {
                            Debug.Print($"Red found: at ({x},{y}) " + pixel.ToString());
                            redFound = true;
                        }
                    }
                }
                float percentageOfBlue = (float)blueCount / (float)totalPixelsToCheck;
                looksToBeFishing = redFound && percentageOfBlue > .4f;
                image.UnlockBits(imageData);
                return looksToBeFishing;
            }
        }
    }
}