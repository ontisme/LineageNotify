using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;

namespace LineageNotify
{
    public partial class Form1 : Form
    {

        [DllImport("User32.dll")]
        static extern int SetForegroundWindow(IntPtr point);

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr handle, ref Rectangle rect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public Form1()
        {
            InitializeComponent();
        }



        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (autoupdate.Checked == true)
            {
                timer1.Interval = int.Parse(uptime.Text) * 60000;
                timer1.Start();

            }

            if (autoupdate.Checked == false)
            {
                timer1.Stop();

            }

        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {
            if (uptime.Text == "")
            {
                uptime.Text = "1";
            }
            if (uptime.Text == null)
            {
                uptime.Text = "1";
            }


            if (int.Parse(uptime.Text) < 1)
            {
                uptime.Text = "1";
            }
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (checkfullscreen.Checked == true)
            {
                screenshot();
                lineNotify(" ", 0, 0, @"C:\\linscreen.jpg");
            }
            else
            {
                IntPtr hwnd = FindWindow(null, "Lineage");
                CaptureWindow(hwnd);

                FileInfo f = new FileInfo(@"C:\\linscreen.jpg");
                //string m = f.Length + "位元組\n" + "建立日期:" + f.CreationTime + "\n" + "修改日期:" + f.LastWriteTime + "\n" + "存取日期:" + f.LastAccessTime;

                // 判斷 test.txt 檔案是否存在
                string FileName = @"C:\\linscreen.jpg";

                if (System.IO.File.Exists(FileName))
                {
                    lineNotify(f.LastAccessTime.ToString(), 0, 0, null);
                    Thread.Sleep(1000);
                    lineNotify(" ", 0, 0, @"C:\\linscreen.jpg");
                }
                else
                {
                    lineNotify("沒有圖檔", 0, 0, null);
                }
            }


        }

        private void screenshot()
        {
            // Create a new thread for demonstration purposes.
            Thread thread = new Thread(() =>
            {
                // Determine the size of the "virtual screen", which includes all monitors.
                int screenLeft = SystemInformation.VirtualScreen.Left;
                int screenTop = SystemInformation.VirtualScreen.Top;
                int screenWidth = SystemInformation.VirtualScreen.Width;
                int screenHeight = SystemInformation.VirtualScreen.Height;

                // Create a bitmap of the appropriate size to receive the screenshot.
                using (Bitmap bmp = new Bitmap(screenWidth, screenHeight))
                {
                    // Draw the screenshot into our bitmap.
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(screenLeft, screenTop, 0, 0, bmp.Size);
                    }

                    // Do something with the Bitmap here, like save it to a file:
                    bmp.Save("C:\\linscreen.jpg", ImageFormat.Jpeg);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.LineToken = LineTokenText.Text;
            Properties.Settings.Default.Save();


        }

        private void lineNotify(string msg, int stickerPackageID, int stickerID, string pictureUrl)
        {
            string token = LineTokenText.Text;
            try
            {
                var formDataContent = new MultipartFormDataContent();
                var proxy = new WebProxy();
                var handler = new HttpClientHandler()
                {
                    Proxy = proxy
                };

                if (pictureUrl != null)
                {
                    byte[] bytesImage = ReadFile(pictureUrl);
                    var fileContent = new StreamContent(new MemoryStream(bytesImage))
                    {
                        Headers =
            {
               ContentLength = bytesImage.Length,
               ContentType = new MediaTypeHeaderValue("image/jpeg")
            }
                    };
                    formDataContent.Add(fileContent, "imageFile", "bmp");
                }

                formDataContent.Add(new StringContent(msg), "message");
                if (stickerPackageID > 0 && stickerID > 0)
                {
                    formDataContent.Add(new StringContent(stickerPackageID.ToString()), "stickerPackageId");
                    formDataContent.Add(new StringContent(stickerID.ToString()), "stickerId");
                }

                using (var client = new HttpClient(handler: handler, disposeHandler: true))
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                    using (var res = client.PostAsync("https://notify-api.line.me/api/notify", formDataContent))
                    {
                        //可以用它的方法 但我沒原本的方法就先用這個
                        res.Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public static byte[] ReadFile(string filePath)
        {
            byte[] buffer;
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            try
            {
                int length = (int)fileStream.Length;  // get file length
                buffer = new byte[length];            // create buffer
                int count;                            // actual number of bytes read
                int sum = 0;                          // total number of bytes read

                // read until Read method returns 0 (end of the stream has been reached)
                while ((count = fileStream.Read(buffer, sum, length - sum)) > 0)
                    sum += count;  // sum is a buffer offset for next reading
            }
            finally
            {
                fileStream.Close();
            }
            return buffer;
        }
        public void CaptureWindow(IntPtr handle)
        {
            // Get the size of the window to capture
            Rectangle rect = new Rectangle();
            GetWindowRect(handle, ref rect);

            // GetWindowRect returns Top/Left and Bottom/Right, so fix it
            rect.Width = rect.Width - rect.X;
            rect.Height = rect.Height - rect.Y;

            // Create a bitmap to draw the capture into
            using (Bitmap bitmap = new Bitmap(rect.Width, rect.Height))
            {
                // Use PrintWindow to draw the window into our bitmap
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    IntPtr hdc = g.GetHdc();
                    if (!PrintWindow(handle, hdc, 0))
                    {
                        int error = Marshal.GetLastWin32Error();
                        var exception = new System.ComponentModel.Win32Exception(error);
                        Debug.WriteLine("ERROR: " + error + ": " + exception.Message);
                        // TODO: Throw the exception?
                    }
                    g.ReleaseHdc(hdc);
                }

                // Save it as a .jpg just to demo this
                bitmap.Save("C:\\linscreen.jpg",ImageFormat.Jpeg);
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            timer1.Interval = int.Parse(uptime.Text) * 2000;
            timer1.Start();
        }

        private void Button2_Click_1(object sender, EventArgs e)
        {
            Process p = Process.GetProcessesByName("Lin.bin").FirstOrDefault();
            if (p != null)
            {
                IntPtr h = p.MainWindowHandle;
                SetForegroundWindow(h);
     
            }
        }
    }
}