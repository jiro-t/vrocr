using System;
using System.Windows.Forms;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Globalization;
using Windows.Media.Ocr;
using System.Linq;

namespace ocr
{
    public partial class Form1 : Form
    {
        Windows.Media.Ocr.OcrEngine ocr_engine = OcrEngine.TryCreateFromLanguage(new Language("ja"));
        System.IO.MemoryStream ms = new System.IO.MemoryStream();
        System.Drawing.Bitmap capBmp = new System.Drawing.Bitmap(400, 200);

        string translateText = "";

        public Form1()
        {
            InitializeComponent();
        }

        public async Task<Windows.Storage.Streams.IRandomAccessStream> ConvertToRandomAccessStream(System.IO.MemoryStream memoryStream)
        {
            var randomAccessStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            var outputStream = randomAccessStream.GetOutputStreamAt(0);
            var dw = new Windows.Storage.Streams.DataWriter(outputStream);
            var task = new Task(() => dw.WriteBytes(memoryStream.ToArray()));
            task.Start();
            await task;
            await dw.StoreAsync();
            await outputStream.FlushAsync();
            return randomAccessStream;
        }

        private async Task<Windows.Graphics.Imaging.SoftwareBitmap> LoadImage(Windows.Storage.Streams.IRandomAccessStream stream)
        {
            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
            var bitmap = await decoder.GetSoftwareBitmapAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
            return bitmap;
        }

        private async Task<Windows.Graphics.Imaging.SoftwareBitmap> LoadImage(System.IO.MemoryStream s)
        {
            var stream = await ConvertToRandomAccessStream(s);
            var bitmap = await LoadImage(stream);
            return bitmap;
        }

        private async Task<Windows.Graphics.Imaging.SoftwareBitmap> LoadImage(string path)
        {
            var fs = System.IO.File.OpenRead(path);
            var buf = new byte[fs.Length];
            fs.Read(buf, 0, (int)fs.Length);
            var mem = new System.IO.MemoryStream(buf);
            mem.Position = 0;

            var stream = await ConvertToRandomAccessStream(mem);
            var bitmap = await LoadImage(stream);
            return bitmap;
        }

        private async void loadBMP()
        {
            var bmp = await LoadImage(ms);
            var result = await ocr_engine.RecognizeAsync(bmp);
            translateText = result.Text;
            System.Windows.Forms.MessageBox.Show(result.Text);
            bmp = null;
        }

        private void takeScreenshot()
        {
            System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(capBmp);
            graphics.CopyFromScreen(new System.Drawing.Point(750, 300), new System.Drawing.Point(0, 0), capBmp.Size);
            graphics.Dispose();

            capBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);

            pictureBox1.Image = capBmp;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            loadBMP();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            takeScreenshot();
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            string text = await client.GetStringAsync("http://translate.weblio.jp/?lp=EJ&lpf=EJ&originalText="+translateText);
            int i = text.IndexOf("class=transExpB>");
            if(i >= 0)
            {
                text = text.Remove(0, i+16);
                int j = text.IndexOf("</div>");
                if (j > 0)
                {
                    //translated text
                    text = text.Remove(j, text.Count() - j);

                    //<li></li> -> CRLF
                    text = text.Replace("<ul>", "");
                    text = text.Replace("</ul>", "");
                    text = text.Replace("<li>", "");
                    text = text.Replace("</li>", "\r\n");
                }
            }
            else
            {
                i = text.IndexOf("class=translatedTextAreaLn");
                if (i > 0)
                {
                    text = text.Remove(0, i + 33);
                    int j = text.IndexOf("</span>");
                    if (j > 0)
                    {
                        //translated text
                        text = text.Remove(j, text.Count() - j);
                    }
                }
            }

            System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(capBmp);
            System.Drawing.Font font = new System.Drawing.Font("MS UI Gothic", 20);
            graphics.DrawString(text,font, System.Drawing.Brushes.Red,50,10);
            font.Dispose();
            graphics.Dispose();
            pictureBox1.Image = capBmp;

            capBmp.Save("tmp.bmp");
        }
    }
}
