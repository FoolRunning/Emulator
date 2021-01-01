using System.Diagnostics;
using System.Drawing;
using SystemBase;

namespace EmulatorUI.Scalers
{
    public class CRTScaler : IDisplayScaler
    {
        private readonly RgbColor black = new RgbColor(0, 0, 0);

        public void Scale(RgbColor[] input, Size inputSize, RgbColor[] output, Size outputSize)
        {
            Debug.Assert(outputSize.Width == inputSize.Width * 2 && outputSize.Height == inputSize.Height * 2);

            int outIndex = 0;
            int inIndex = 0;
            for (int y = 0; y < inputSize.Height; y++)
            {
                for (int x = 0; x < inputSize.Width; x++)
                {
                    int inColor = input[inIndex++].RGB;
                    output[outIndex] = new RgbColor(inColor & 0xFF00);
                    output[outIndex + outputSize.Width] = new RgbColor(inColor & 0xFF);
                    outIndex++;

                    output[outIndex] = new RgbColor(inColor & 0xFF0000);
                    output[outIndex + outputSize.Width] = new RgbColor(inColor);
                    outIndex++;
                }

                outIndex += outputSize.Width;
            }
        }
    }
}
