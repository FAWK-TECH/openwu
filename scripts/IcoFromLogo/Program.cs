using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

// Classic ICO (32bpp BMP + AND mask) — required for CSC ApplicationIcon
class Prog {
  static Bitmap Square(Image img, int size) {
    var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.SmoothingMode = SmoothingMode.HighQuality;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    g.Clear(Color.FromArgb(255, 28, 28, 30));
    int side = Math.Min(img.Width, img.Height);
    int sx = (img.Width - side) / 2, sy = (img.Height - side) / 2;
    g.DrawImage(img, new Rectangle(0,0,size,size), new Rectangle(sx,sy,side,side), GraphicsUnit.Pixel);
    return bmp;
  }

  static byte[] BitmapToIconImage(Bitmap bmp) {
    int w = bmp.Width, h = bmp.Height;
    // XOR: 32bpp BGRA bottom-up; AND: 1bpp padded rows
    int xorStride = w * 4;
    int andRow = ((w + 31) / 32) * 4;
    int xorSize = xorStride * h;
    int andSize = andRow * h;
    int dibSize = 40 + xorSize + andSize;
    var buf = new byte[dibSize];
    using var ms = new MemoryStream(buf);
    using var bw = new BinaryWriter(ms);
    // BITMAPINFOHEADER
    bw.Write(40);
    bw.Write(w);
    bw.Write(h * 2);
    bw.Write((short)1);
    bw.Write((short)32);
    bw.Write(0); // BI_RGB
    bw.Write(xorSize + andSize);
    bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
    // pixels bottom-up
    var data = bmp.LockBits(new Rectangle(0,0,w,h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    try {
      int srcStride = data.Stride;
      var line = new byte[Math.Abs(srcStride)];
      for (int y = h - 1; y >= 0; y--) {
        Marshal.Copy(IntPtr.Add(data.Scan0, y * srcStride), line, 0, w * 4);
        // ARGB from GDI+ is BGRA in memory already for Format32bppArgb
        bw.Write(line, 0, w * 4);
      }
    } finally { bmp.UnlockBits(data); }
    // AND mask zeros (opaque)
    bw.Write(new byte[andSize]);
    return buf;
  }

  static void WriteIco(string path, (int size, byte[] dib)[] frames) {
    using var fs = File.Create(path);
    using var bw = new BinaryWriter(fs);
    bw.Write((short)0); bw.Write((short)1); bw.Write((short)frames.Length);
    int offset = 6 + 16 * frames.Length;
    foreach (var f in frames) {
      byte dim = f.size >= 256 ? (byte)0 : (byte)f.size;
      bw.Write(dim); bw.Write(dim);
      bw.Write((byte)0); bw.Write((byte)0);
      bw.Write((short)1); bw.Write((short)32);
      bw.Write(f.dib.Length); bw.Write(offset);
      offset += f.dib.Length;
    }
    foreach (var f in frames) bw.Write(f.dib);
  }

  static int Main(string[] a) {
    string src = a[0], icoOut = a[1], pngOut = a[2];
    using var img = Image.FromFile(src);
    // CSC is happiest with modest sizes; include 256 as classic DIB too
    int[] sizes = { 16, 32, 48, 64, 128, 256 };
    var frames = new (int, byte[])[sizes.Length];
    for (int i = 0; i < sizes.Length; i++) {
      using var b = Square(img, sizes[i]);
      frames[i] = (sizes[i], BitmapToIconImage(b));
    }
    using (var b512 = Square(img, 512)) b512.Save(pngOut, ImageFormat.Png);
    WriteIco(icoOut, frames);
    Console.WriteLine($"ICO {new FileInfo(icoOut).Length}");
    // verify
    using var ico = new Icon(icoOut);
    Console.WriteLine($"Load OK {ico.Width}x{ico.Height}");
    return 0;
  }
}
