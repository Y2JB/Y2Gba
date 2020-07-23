#define THREADED_RENDERER

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Windows.Input;
using System.Threading;

using Gba.Core;
using GbaDebugger;
using WinFormsRenderer;

namespace WinFormRender
{
    public partial class RenderWindow : Form
    {
        GameboyAdvance gba;
        GbaDebugConsole dbgConsole;
       
        ConsoleWindow consoleWindow;
        TtyConsole ttyConsole;
        GfxInspector gfxInspectorWindow;

        Stopwatch timer = new Stopwatch();
        long elapsedMs;
        int seconds;
        //long elapsedMsBgWin;
        int framesDrawn;
        int fps;

        Rectangle gameRect;
        Rectangle fpsRect;
        Point fpsPt;

        MenuStrip menu;

#if THREADED_RENDERER
        volatile bool drawFrame = false;
        bool exitThread = false;
        Thread renderThread;
#endif 

        BufferedGraphicsContext gfxBufferedContext;
        BufferedGraphics gfxBuffer;


        public RenderWindow()
        {
            InitializeComponent();
            
            Application.ApplicationExit += new EventHandler(this.OnApplicationExit);

            gba = new Gba.Core.GameboyAdvance();
            gba.PowerOn();
            gba.OnFrame = () => this.Draw();

            dbgConsole = new GbaDebugConsole(gba);
            
            consoleWindow = new ConsoleWindow(gba, dbgConsole);
            consoleWindow.VisibleChanged += ConsoleWindow_VisibleChanged;
            consoleWindow.Show();
           
            ttyConsole = new TtyConsole(gba);
            //ttyConsole.Show();

            gfxInspectorWindow = new GfxInspector(gba);

            this.Text = gba.Rom.RomName;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
      
            Controls.Add(menu = new MenuStrip
            {
                Items =
                {
                    new ToolStripMenuItem("Emulator")
                    {
                        DropDownItems =
                        {
                            new ToolStripMenuItem("Load ROM", null, (sender, args) => { }),
                            new ToolStripMenuItem("Reset", null, (sender, args) => {  }),
                        }
                    },
                    new ToolStripMenuItem("Window")
                    {
                        DropDownItems =
                        {
                            new ToolStripMenuItem("Console", null, (sender, args) => { consoleWindow.Visible = !consoleWindow.Visible; }),
                            new ToolStripMenuItem("Tty", null, (sender, args) => { ttyConsole.Visible = !ttyConsole.Visible; }),
                            new ToolStripMenuItem("Gfx Inspector", null, (sender, args) => { gfxInspectorWindow.Visible = !gfxInspectorWindow.Visible; })
                        }
                    }
                }
            });

            // 4X gameboy resolution
            Width = LcdController.Screen_X_Resolution * 4;
            Height = LcdController.Screen_Y_Resolution * 4 + menu.Height;
            DoubleBuffered = true;
            
            timer.Start();

            //Thread.CurrentThread.Priority = ThreadPriority.Highest;

#if THREADED_RENDERER
            drawFrame = false;
            renderThread = new Thread(new ThreadStart(RenderThread));
            renderThread.Start();
#endif
        }

        private void ConsoleWindow_VisibleChanged(object sender, EventArgs e)
        {
            Application.Idle -= new EventHandler(OnApplicationIdleDebugger);
            Application.Idle -= new EventHandler(OnApplicationIdle);

            if (consoleWindow.Visible)
            {
                Application.Idle += new EventHandler(OnApplicationIdleDebugger);
            }   
            else
            {
                Application.Idle += new EventHandler(OnApplicationIdle);
            }
        }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            consoleWindow.Location = new Point(Location.X + Width + 20, Location.Y);
            ttyConsole.Location = new Point(Location.X, Location.Y + Height + 40);

            // Gets a reference to the current BufferedGraphicsContext
            gfxBufferedContext = BufferedGraphicsManager.Current;

            // Creates a BufferedGraphics instance associated with this form, and with dimensions the same size as the drawing surface of Form1.
            gfxBuffer = gfxBufferedContext.Allocate(this.CreateGraphics(), this.DisplayRectangle);
        }


        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            fpsRect = new Rectangle(ClientRectangle.Width - 75, 5, 75, 30);
            fpsPt = new Point(ClientRectangle.Width - 75, 10);

            gameRect = new Rectangle(0, 0, ClientRectangle.Width, ClientRectangle.Height);
                       
            if(menu != null)
            {
                fpsPt.Y = 10 + menu.Height; 
                fpsRect.Y = 5 + menu.Height;
                gameRect.Y = menu.Height;
                gameRect.Height -= menu.Height;
            }

            if (gfxBufferedContext != null)
            {
                gfxBuffer = gfxBufferedContext.Allocate(this.CreateGraphics(), this.DisplayRectangle);
            }
        }


        private void OnKeyDown(Object o, KeyEventArgs a)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, KeyEventArgs>(OnKeyDown), o, a);
                return;
            }
            if (a.KeyCode == Keys.Up) gba.Joypad.UpdateKeyState(Joypad.GbaKey.Up, true);
            else if (a.KeyCode == Keys.Down) gba.Joypad.UpdateKeyState(Joypad.GbaKey.Down, true);
            else if (a.KeyCode == Keys.Left) gba.Joypad.UpdateKeyState(Joypad.GbaKey.Left, true);
            else if (a.KeyCode == Keys.Right) gba.Joypad.UpdateKeyState(Joypad.GbaKey.Right, true);
            else if (a.KeyCode == Keys.Z) gba.Joypad.UpdateKeyState(Joypad.GbaKey.B, true);
            else if (a.KeyCode == Keys.X) gba.Joypad.UpdateKeyState(Joypad.GbaKey.A, true);
            else if (a.KeyCode == Keys.A) gba.Joypad.UpdateKeyState(Joypad.GbaKey.L, true);
            else if (a.KeyCode == Keys.S) gba.Joypad.UpdateKeyState(Joypad.GbaKey.R, true);
            else if (a.KeyCode == Keys.Enter) gba.Joypad.UpdateKeyState(Joypad.GbaKey.Start, true);
            else if (a.KeyCode == Keys.Back) gba.Joypad.UpdateKeyState(Joypad.GbaKey.Select, true);
        }


        private void OnKeyUp(Object o, KeyEventArgs a)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, KeyEventArgs>(OnKeyUp), o, a);
                return;
            }

            if (a.KeyCode == Keys.Up) gba.Joypad.UpdateKeyState(Joypad.GbaKey.Up, false);
            else if (a.KeyCode == Keys.Down) gba.Joypad.UpdateKeyState(Joypad.GbaKey.Down, false);
            else if (a.KeyCode == Keys.Left) gba.Joypad.UpdateKeyState(Joypad.GbaKey.Left, false);
            else if (a.KeyCode == Keys.Right) gba.Joypad.UpdateKeyState(Joypad.GbaKey.Right, false);
            else  if (a.KeyCode == Keys.Z) gba.Joypad.UpdateKeyState(Joypad.GbaKey.B, false);
            else if (a.KeyCode == Keys.X) gba.Joypad.UpdateKeyState(Joypad.GbaKey.A, false);
            else if (a.KeyCode == Keys.A) gba.Joypad.UpdateKeyState(Joypad.GbaKey.L, false);
            else if (a.KeyCode == Keys.S) gba.Joypad.UpdateKeyState(Joypad.GbaKey.R, false);
            else if (a.KeyCode == Keys.Enter) gba.Joypad.UpdateKeyState(Joypad.GbaKey.Start, false);
            else if (a.KeyCode == Keys.Back) gba.Joypad.UpdateKeyState(Joypad.GbaKey.Select, false);
        }

        
        private void OnApplicationIdleDebugger(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, EventArgs>(OnApplicationIdle), sender, e);
                return;
            }

            while (IsApplicationIdle())
            {
                if (dbgConsole.EmulatorMode == GbaDebugConsole.Mode.Running)
                {
                    // Process several instructions before going to check the Windows message queue
                    for (int i = 0; i < 512; i++)
                    {
                        gba.Step();

                        if (dbgConsole.CheckForBreakpoints())
                        {
                            consoleWindow.RefreshEmuSnapshot();
                            break;
                        }
                    }
                }

                else if (dbgConsole.EmulatorMode == GbaDebugConsole.Mode.BreakPoint)
                {
                    Thread.Sleep(1);
                    if (dbgConsole.BreakpointStepAvailable)
                    {
                        dbgConsole.OnPreBreakpointStep();
                        gba.Step();
                        dbgConsole.PeekSequentialInstructions();
                        dbgConsole.OnPostBreakpointStep();
                        consoleWindow.RefreshEmuSnapshot();
                        consoleWindow.RefreshConsoleText();
                    }
                }
            }
        }


        private void OnApplicationIdle(object sender, EventArgs e)
        {
            while (IsApplicationIdle())
            {
                // Process several instructions before going to check the Windows message queue
                for (int i = 0; i < 512; i++)
                {
                    gba.Step();
                }
            }
        }


#if THREADED_RENDERER
        private void RenderThread()
        {
            var whiteBrush = new SolidBrush(Color.White);
            var redBrush = new SolidBrush(Color.Red);
            var amberBrush = new SolidBrush(Color.Orange);
            var greenBrush = new SolidBrush(Color.Green);
            var font = new Font("Verdana", 8);                                   

            while (exitThread == false)
            {
                if (drawFrame)
                {
                    framesDrawn++;

                    lock (gba.FrameBuffer)
                    {
                    
                        gfxBuffer.Graphics.DrawImage(gba.FrameBuffer.Bitmap, gameRect);


                        // Only show fps if we are dipping and then use a colour code
                        if (fps < 58)
                        {
                            var brush = redBrush;
                            if (fps >= 50) brush = greenBrush;
                            else if (fps >= 40) brush = amberBrush;

                            gfxBuffer.Graphics.FillRectangle(brush, fpsRect);
                            gfxBuffer.Graphics.DrawString(String.Format("{0:D2} fps", fps), font, whiteBrush, fpsPt);
                        }

                        gfxBuffer.Render();
                    }
                    drawFrame = false;
                }
            }
        }


        private void Draw()
        {
            if (timer.ElapsedMilliseconds - elapsedMs >= 1000)
            {
                elapsedMs = timer.ElapsedMilliseconds;

                fps = framesDrawn;
                framesDrawn = 0;
                seconds++;

                if(seconds % 120 == 0)
                {
                    gba.Rom.SaveSramData();
                }
            }

            // If the Bg viewer is open, then update it at a lower fps
            if (gfxInspectorWindow.Visible) // &&
                //timer.ElapsedMilliseconds - elapsedMsBgWin >= (200))
            {
                //elapsedMsBgWin = timer.ElapsedMilliseconds;
                gfxInspectorWindow.RenderTab();
            }

            // Wait for previous frame to finish drawing while also locking to 60fps
            while (drawFrame)
            {
            }
            drawFrame = true;
        }

#else
        private void Draw()
        {  
            framesDrawn++;                
            gfxBuffer.Graphics.DrawImage(gba.FrameBuffer, new Rectangle(0, 0, ClientRectangle.Width, ClientRectangle.Height));
         

            gfxBuffer.Graphics.FillRectangle(new SolidBrush(Color.White), new Rectangle(ClientRectangle.Width -75, 5, 55, 30));
            gfxBuffer.Graphics.DrawString(String.Format("{0:D2} fps", fps), new Font("Verdana", 8),  new SolidBrush(Color.Black), new Point(ClientRectangle.Width - 75, 10));

            gfxBuffer.Render();  
        }
#endif

        private void OnApplicationExit(object sender, EventArgs e)
        {
            if (gba.PoweredOn)
            {
                gba.Rom.PersistSaveData();
            }

            gba.Dispose();
            exitThread = true;
            Thread.Sleep(250);
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParameter;
            public IntPtr LParameter;
            public uint Time;
            public Point Location;
        }

        bool IsApplicationIdle()
        {
            NativeMessage result;
            return PeekMessage(out result, IntPtr.Zero, (uint)0, (uint)0, (uint)0) == 0;
        }

        [DllImport("user32.dll")]
        public static extern int PeekMessage(out NativeMessage message, IntPtr window, uint filterMin, uint filterMax, uint remove);
    }




}
