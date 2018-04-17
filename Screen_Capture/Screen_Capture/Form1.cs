using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace Screen_Capture
{
    public partial class Form1 : Form
    {
        private bool isClick = false;
        private bool stop = false;
        private System.Drawing.Point top;
        private System.Drawing.Point bottom;
        private int disp_num;
        private int[] ex_params = new int[4];//{X Coordinate, Y Coordinate, Width, Height}
        private List<Bitmap> bmplist;

        private VideoWriter writer;

        // ======================================
        // Initialize : 初期化
        public Form1()
        {
            InitializeComponent();
            label1.Visible = false;
            disp_num = 0;
            this.ClientSize = new System.Drawing.Size(Screen.PrimaryScreen.Bounds.Width / 2, Screen.PrimaryScreen.Bounds.Height / 2 + menuStrip1.Height);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            isClick = false;
            stop = false;
            bmplist = new List<Bitmap>();

            Draw_Thread.RunWorkerAsync();
        }
        // ======================================


        // ======================================
        // Drawing processing thread : 描画処理用スレッド
        private void Draw_Thread_DoWork(object sender, DoWorkEventArgs e)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            long[] time = new long[2] { 0, 0 };
            try
            {
                while (!stop)
                {
                    sw.Start();
                    Bitmap src = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                    Bitmap dst = new Bitmap(pictureBox1.Width, pictureBox1.Height);
                    Pen redpen = new Pen(Brushes.Red, 5);
                    Graphics src_g = Graphics.FromImage(src);
                    Graphics dst_g = Graphics.FromImage(dst);
                    src_g.CopyFromScreen(new System.Drawing.Point(src.Width * disp_num, 0), new System.Drawing.Point(0, 0), src.Size, CopyPixelOperation.SourceCopy);
                    dst_g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    if (top != null && bottom != null && top.X != bottom.X && top.Y != bottom.Y && !isClick)
                    {
                        if (top.X < bottom.X && top.Y < bottom.Y) ex_params = new int[4] { top.X, top.Y, bottom.X - top.X, bottom.Y - top.Y };
                        else if (top.X > bottom.X && top.Y < bottom.Y) ex_params = new int[4] { bottom.X, top.Y, top.X - bottom.X, bottom.Y - top.Y };
                        else if (top.X < bottom.X && top.Y > bottom.Y) ex_params = new int[4] { top.X, bottom.Y, bottom.X - top.X, top.Y - bottom.Y };
                        else ex_params = new int[4] { bottom.X, bottom.Y, top.X - bottom.X, top.Y - bottom.Y };

                        int[] ratio = new int[2] { dst.Width / ex_params[2], dst.Height / ex_params[3] };
                        Rectangle rect = new Rectangle(ex_params[0] * 2, ex_params[1] * 2, ex_params[2] * 2, ex_params[3] * 2);
                        Bitmap dst_cut = src.Clone(rect, src.PixelFormat);
                        if (dst_cut.Width * ratio[1] > pictureBox1.Width)
                        {
                            int h = ex_params[3] * ratio[0];
                            dst_g.DrawImage(dst_cut, 0, (pictureBox1.Height - h) / 2, dst.Width, h);
                        }
                        else
                        {
                            int w = ex_params[2] * ratio[1];
                            dst_g.DrawImage(dst_cut, (pictureBox1.Width - w) / 2, 0, w, dst.Height);
                        }
                    }
                    else
                    {
                        dst_g.DrawImage(src, 0, 0, src.Width / 2, src.Height / 2);
                        if (isClick)
                        {
                            System.Drawing.Point now = new System.Drawing.Point();
                            Invoke((MethodInvoker)delegate { now = PointToClient(Cursor.Position); });
                            now.Y -= menuStrip1.Height;

                            if (top.X != now.X && top.Y != now.Y)
                            {
                                Rectangle rect = new Rectangle();
                                if (top.X < now.X && top.Y < now.Y) rect = new Rectangle(top, new System.Drawing.Size(now.X - top.X, now.Y - top.Y));
                                else if (top.X < now.X && top.Y < now.Y) rect = new Rectangle(top, new System.Drawing.Size(now.X - top.X, now.Y - top.Y));
                                else if (top.X > now.X && top.Y < now.Y) rect = new Rectangle(now.X, top.Y, top.X - now.X, now.Y - top.Y);
                                else if (top.X < now.X && top.Y > now.Y) rect = new Rectangle(top.X, now.Y, now.X - top.X, top.Y - now.Y);
                                else if (top.X > now.X && top.Y > now.Y) rect = new Rectangle(now, new System.Drawing.Size(top.X - now.X, top.Y - now.Y));

                                dst_g.DrawRectangle(redpen, rect);
                            }
                            else
                            {
                                dst_g.DrawLine(redpen, top, now);
                            }
                        }
                    }
                    src_g.Dispose();
                    dst_g.Dispose();

                    bmplist.Add(src);
                    Invoke((MethodInvoker)delegate { pictureBox1.Image = dst; });

                    time[0] += sw.ElapsedMilliseconds;
                    time[1]++;
                    sw.Reset();
                }
                sw.Stop();
            }
            catch (Exception str)
            {
                Console.WriteLine(str);
            }


            Invoke((MethodInvoker)delegate { draw_label(0); });
            writer = new VideoWriter();
            if (!Directory.Exists(@"C:\Temp")) { Directory.CreateDirectory(@"C:\Temp"); }
            writer.Open(@"C:\Temp\" + DateTime.Now.ToString("yyyymmddhhmmss") + ".avi", FourCC.MJPG, 1000 / 
                (double)(time[0] / time[1]), new OpenCvSharp.Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));

            for (int n = 0; n < bmplist.Count; n++)
            {
                Mat mframe = new Mat();
                mframe = bmplist[n].ToMat();
                unsafe
                {
                    byte* b = mframe.DataPointer;
                    for (int i = 0; i < mframe.Width; i++)
                    {
                        for (int j = 0; j < mframe.Height; j++)
                        {
                            byte p = b[0];
                            b[0] = b[2];
                            b[2] = p;
                            b = b + 4;
                        }
                    }
                }
                writer.Write(mframe);
                BeginInvoke((MethodInvoker)delegate { draw_label(n); });
            }
        }
        // ======================================


        // ======================================
        // Exit button : 終了ボタン
        private void 終了EToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stop = true;
            while (Draw_Thread.IsBusy) { Application.DoEvents(); }
            if (writer.IsOpened()) writer.Dispose();
            System.Diagnostics.Process.Start( "EXPLORER.EXE", @"C:\Temp");
            Application.Exit();
        }
        // ======================================


        // ======================================
        // When mouse button pressed : マウス押下時
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (!isClick)
            {
                isClick = true;
                top = new System.Drawing.Point();
                top = PointToClient(Cursor.Position);
                top.Y -= menuStrip1.Height;
            }
        }
        // ======================================


        // ======================================
        // when mouse buton is releaseed : マウス解放時
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (isClick) {
                isClick = false;
                bottom = new System.Drawing.Point();
                bottom = PointToClient(Cursor.Position);
                bottom.Y -= menuStrip1.Height;
            }
        }
        // ======================================


        // ======================================
        // When pressed keyboard : キーボード押下時
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Left)
            {
                if (disp_num - 1 > -1) disp_num--;
            }
            else if(e.KeyCode == Keys.Right)
            {
                if (disp_num + 1 < Screen.AllScreens.Length) disp_num++;
            }
        }
        // ======================================


        // ======================================
        // Label upfate function : ラベル更新用関数
        void draw_label(int count)
        {
            if(!label1.Visible)
            {
                label1.Location = new System.Drawing.Point((pictureBox1.Width - label1.Width) / 2, (pictureBox1.Height - label1.Height) / 2);
                label1.Visible = true;
                label1.Text =  "0%";
            }
            else
            {
                label1.Text = (Math.Round((decimal)(((double)count / (double)bmplist.Count) * 100),0)).ToString() + "%";
            }
        }
        // ======================================
    }
}
