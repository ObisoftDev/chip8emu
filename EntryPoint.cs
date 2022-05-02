using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ch8Emu
{
    class EntryPoint
    {
        [STAThread]
        static void Main()
        {
            CH8CPU emu = new CH8CPU();
            emu.LoadProgram("program.ch8");
            emu.RunProgram();
        }
    }
}
