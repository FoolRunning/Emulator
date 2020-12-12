using System;
using SystemBase;

namespace System_NES
{
    internal sealed class Controller : IBusComponent_16, IController
    {
        #region Button enumeration
        private static class Button
        {
            public const byte A      = (1 << 0);
            public const byte B      = (1 << 1);
            public const byte Select = (1 << 2);
            public const byte Start  = (1 << 3);
            public const byte Up     = (1 << 4);
            public const byte Down   = (1 << 5);
            public const byte Left   = (1 << 6);
            public const byte Right  = (1 << 7);
        }
        #endregion

        #region Member variables
        private readonly ushort registerAddress;
        private byte currentButtonStatus;
        private byte registerController;
        #endregion

        #region Constructor
        public Controller(ushort registerAddress)
        {
            this.registerAddress = registerAddress;
        }
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
        }
        #endregion

        #region IBusComponent_16 implementation
        public void Reset()
        {
            registerController = 0x00;
        }

        public void WriteDataFromBus(ushort address, byte data)
        {
            if (address == registerAddress)
                registerController = currentButtonStatus;
        }

        public byte ReadDataForBus(ushort address)
        {
            if (address == registerAddress)
            {
                byte data = (byte)(registerController & 0x01);
                registerController >>= 1;
                return data;
            }

            return 0;
        }
        #endregion

        #region IController implementation
        public void KeyboardKeyDown(ConsoleKey key)
        {
            byte button = ButtonForKey(key);
            if (button != 0xFF)
                currentButtonStatus.SetFlag(button);
        }

        public void KeyboardKeyUp(ConsoleKey key)
        {
            byte button = ButtonForKey(key);
            if (button != 0xFF)
                currentButtonStatus.ClearFlag(button);
        }
        #endregion

        #region Private helper methods
        private static byte ButtonForKey(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.X: return Button.A;
                case ConsoleKey.Z: return Button.B;
                case ConsoleKey.A: return Button.Start;
                case ConsoleKey.S: return Button.Select;
                case ConsoleKey.UpArrow: return Button.Up;
                case ConsoleKey.DownArrow: return Button.Down;
                case ConsoleKey.LeftArrow: return Button.Left;
                case ConsoleKey.RightArrow: return Button.Right;

                default: return 0xFF;
            }
        }
        #endregion
    }
}
