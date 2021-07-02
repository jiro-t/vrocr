using System;
using System.Windows.Forms;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Globalization;
using Windows.Media.Ocr;

namespace ocr
{
    public partial class Form1 : Form
    {
        Windows.Media.Ocr.OcrEngine ocr_engine = OcrEngine.TryCreateFromLanguage(new Language("ja"));

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
            var bmp = await LoadImage("tmp.bmp");
            var result = await ocr_engine.RecognizeAsync(bmp);
            System.Windows.Forms.MessageBox.Show(result.Text);
        }

        private void takeScreenshot()
        {
            // プライマリスクリーン全体
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(400,200);
            System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bmp);
            // 画面全体をコピーする
            graphics.CopyFromScreen(new System.Drawing.Point(750, 300), new System.Drawing.Point(0, 0), bmp.Size);
            graphics.Dispose();

            pictureBox1.Image = bmp;
            bmp.Save("tmp.bmp");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            loadBMP();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            takeScreenshot();
        }
    }
}
