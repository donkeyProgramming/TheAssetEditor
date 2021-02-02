using System;
using System.Collections.Generic;
using System.Text;

namespace View3D.Input
{
    public class InputSystems
    {
        public Mouse Mouse { get; private set; }
        public Keyboard Keyboard { get; private set; }

        public InputSystems(Mouse mouse, Keyboard keyboard)
        {
            Mouse = mouse;
            Keyboard = keyboard;
        }
    }
}
