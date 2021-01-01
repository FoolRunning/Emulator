using System.Diagnostics;
using System.Drawing;
using SystemBase;

namespace EmulatorUI.Scalers
{
    public class NearestScaler : IDisplayScaler
    {
        public void Scale(RgbColor[] input, Size inputSize, RgbColor[] output, Size outputSize)
        {
            Debug.Assert(outputSize.Width == inputSize.Width * 2 && outputSize.Height == inputSize.Height * 2);

            int inWidth = inputSize.Width;
            int outWidth = outputSize.Width;
            int outIndex = 0;
            int inIndex = 0;
            for (int y = 0; y < inputSize.Height; y++)
            {
                for (int x = 0; x < inWidth; x++)
                {
                    RgbColor inColor = input[inIndex++];
                    output[outIndex] = inColor;
                    output[outIndex + outWidth] = inColor;
                    outIndex++;

                    output[outIndex] = inColor;
                    output[outIndex + outWidth] = inColor;
                    outIndex++;
                }

                outIndex += outWidth;
            }
        }
    }
}
