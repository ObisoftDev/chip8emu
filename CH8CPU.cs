using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VisualOGE;

public class CH8CPU
{
    private int[] RAM;
    private int[] REG;
    private Stack<int> Stack;


    private int PC = 512;
    private int I = 0;
    private int DT = 0;
    private int ST = 0;


    //ExecuteRegisters
    public int OPCODE = 0;
    public int NNN => OPCODE & 0x0FFF;
    public int N => OPCODE & 0x000F;
    public int X => (OPCODE & 0x0F00) >> 8;
    public int Y => (OPCODE & 0x00F0) >> 4;
    public int Vx  { get { return REG[X];}set { REG[X] = value; } }
    public int Vy { get { return REG[Y]; } set { REG[Y] = value; } }
    public int NN => OPCODE & 0x00FF;
    public int REG15 { get { return REG[15]; } set { REG[15] = value; } }
    public int REG0 { get { return REG[0]; } set { REG[0] = value; } }


    //Graphics
    public Surface VRAM;
    public Color4 RenderColor = Color4.White;



    public CH8CPU(int resW = 64,int resH = 32)
    {
        VRAM = new Surface(resW, resH);
        Stack = new Stack<int>();
        RAM = new int[4096];
        REG = new int[16];
    }


    public void LoadProgram(string file)
    {
        byte[] data = File.ReadAllBytes(file);
        Array.Copy(data,0,RAM, PC, data.Length);
    }

    public void RunProgram()
    {
        Display display = new Display("CH8EMU",400,300,64,32);
        display.SetRenderTarget(VRAM);
        display.Start();
        Time.Init();
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        while (UpdateProgram())
        {
            Time.Update();
            stopwatch.Stop();
            Time.SetDelta(stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
            stopwatch.Restart();
            display.SetTitle($"CH8EMU FPS:{Time.FPS}");

            display.DoEvents();
        }
        VRAM.Dispose();
        display.Dispose();
    }

    public bool UpdateProgram()
    {
        if (PC+1 >= RAM.Length) PC = 512;

        OPCODE = ((RAM[PC] << 8) | RAM[PC+1]);
        int nibble = (OPCODE & 0xF000);
        string hex = OPCODE.ToString("X4");

        switch (nibble)
        {
            case 0x0000:
                if (OPCODE == 0x00E0)
                    VRAM.Clear(Color4.Gray);
                if (OPCODE == 0x00EE)
                    PC = Stack.Pop();
             break;
            case 0x1000:
                PC = NNN;
                break;
            case 0x2000:
                Stack.Push(PC);
                PC = NNN;
                break;
            case 0x3000:
                if (Vx == NN) PC += 2;
                break;
            case 0x4000:
                if (Vx != NN) PC += 2;
                break;
            case 0x5000:
                if(Vx == Vy) PC += 2; 
                break;
            case 0x6000:
                Vx = NN;
                break;
            case 0x7000:
                Vx += NN;
                break;
            case 0x8000:
                int nib2 = OPCODE & 0x000F;
                switch (nib2)
                {
                    case 0: Vx = Vy; break;
                    case 1: Vx = Vx | Vy; break;
                    case 2: Vx = Vx & Vy; break;
                    case 3: Vx = Vx ^ Vy; break;
                    case 4:
                        REG15 = Vx + Vy > 255 ? 1 : 0;
                        Vx = Vx + Vy & 0x00FF;
                    break;
                    case 5:
                        REG15 = Vx > Vy ? 1 : 0;
                        Vx = Vx - Vy & 0x00FF;
                   break;
                    case 6:
                        REG15 = Vx & 0x0001;
                        Vx = Vx >> 1;
                    break;
                    case 7:
                        REG15 = Vy > Vx ? 1 : 0;
                        Vx = Vy - Vx & 0x00FF;
                        break;
                    case 14:
                        REG15 = (Vx & 0x80) == 0x80 ? 1 : 0;
                        Vx = Vx << 1;
                    break;
                }
                break;

            case 0x9000:
                if (Vx != Vy) PC += 2;
            break;
            case 0xA000:
                I = NNN;
            break;
            case 0xB000:
                PC = NNN + REG0;
            break;
            case 0xC000:
                Random rnd = new Random(Environment.TickCount);
                Vx = (rnd.Next() & NN);
            break;
            case 0xD000:
                REG15 = 0;
                for(int i = 0; i < N; i++)
                {
                    int mem = RAM[I + i];
                    for (int j = 0; j < 8; j++)
                    {
                        int pixel = (mem >> (7 - j) & 0x01);
                        int index = X + j + ((Y + i) * 64);
                        if (pixel == 1 && VRAM[index] == RenderColor.ToArgb)
                            REG15 = 1;
                        VRAM[index] = RenderColor.ToArgb;
                    }
                }
            break;
            case 0xE000:
                if (NN == 0x009E)
                {
                    //Key Presed
                }
                if (NN == 0x00A1)
                {
                    //Key Release
                }
            break;
            case 0xF000:
               switch(NN)
                {
                    case 0x15:  DT = Vx;break;
                    case 0x1E: I += Vx; break;
                    case 0x29: I = Vx * 5; break;
                    case 0x33:
                        RAM[I] = Vx / 100;
                        RAM[I + 1] = (Vx % 100) / 10;
                        RAM[I + 2] = (Vx % 10);
                        break;
                    case 0x55:
                        for (int i = 0; i < Vx; i++)
                            RAM[I+i] = REG[i];
                        break;
                    case 0x65:
                        for (int i = 0; i < Vx; i++)
                            REG[i] = RAM[I + i];
                        break;
                }
                break;
        }
        PC+=2;

        return true;
    }
}

