using System.Drawing;
using SystemBase;

namespace EmulatorUI
{
    public interface IDisplayScaler
    {
        void Scale(RgbColor[] input, Size inputSize, RgbColor[] output, Size outputSize);
    }
}
