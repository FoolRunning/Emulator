/*
 * Copyright © 2003 Maxim Stepin (maxst@hiend3d.com)
 * 
 * Copyright © 2010 Cameron Zemek (grom@zeminvaders.net)
 * 
 * Copyright © 2011, 2012 Tamme Schichler (tamme.schichler@googlemail.com)
 * 
 * This file is part of hqxSharp.
 *
 * hqxSharp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * hqxSharp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with hqxSharp. If not, see <http://www.gnu.org/licenses/>.
 */

using System.Diagnostics;

namespace EmulatorUI.Scalers
{
    /// <summary>
    /// Contains the color-blending operations used internally by hqx.
    /// </summary>
    internal static class Interpolation
    {
        const int MaskGreen = 0x00ff00;
        const int MaskRedBlue = 0xff00ff;

        public static int Mix3To1(int c1, int c2)
        {
            return MixColours(3, 1, c1, c2);
        }

        public static int Mix2To1To1(int c1, int c2, int c3)
        {
            return MixColours(2, 1, 1, c1, c2, c3);
        }

        public static int Mix7To1(int c1, int c2)
        {
            return MixColours(7, 1, c1, c2);
        }

        public static int Mix2To7To7(int c1, int c2, int c3)
        {
            return MixColours(2, 7, 7, c1, c2, c3);
        }

        public static int MixEven(int c1, int c2)
        {
            return MixColours(1, 1, c1, c2);
        }

        public static int Mix5To2To1(int c1, int c2, int c3)
        {
            return MixColours(5, 2, 1, c1, c2, c3);
        }

        public static int Mix6To1To1(int c1, int c2, int c3)
        {
            return MixColours(6, 1, 1, c1, c2, c3);
        }

        public static int Mix5To3(int c1, int c2)
        {
            return MixColours(5, 3, c1, c2);
        }

        public static int Mix2To3To3(int c1, int c2, int c3)
        {
            return MixColours(2, 3, 3, c1, c2, c3);
        }

        public static int Mix14To1To1(int c1, int c2, int c3)
        {
            return MixColours(14, 1, 1, c1, c2, c3);
        }

        private static int MixColours(int w1, int w2, int c1, int c2)
        {
            Debug.Assert(w1 > 0 && w2 > 0);

            int totalPartsColour = w1;
            int totalGreen = (c1 & MaskGreen) * w1;
            int totalRedBlue = (c1 & MaskRedBlue) * w1;

            totalPartsColour += w2;
            totalGreen += (c2 & MaskGreen) * w2;
            totalRedBlue += (c2 & MaskRedBlue) * w2;

            totalGreen /= totalPartsColour;
            totalGreen &= MaskGreen;

            totalRedBlue /= totalPartsColour;
            totalRedBlue &= MaskRedBlue;

            return totalGreen | totalRedBlue;
        }

        private static int MixColours(int w1, int w2, int w3, int c1, int c2, int c3)
        {
            Debug.Assert(w1 > 0 && w2 > 0 && w3 > 0);

            int totalPartsColour = w1;
            int totalGreen = (c1 & MaskGreen) * w1;
            int totalRedBlue = (c1 & MaskRedBlue) * w1;

            totalPartsColour += w2;
            totalGreen += (c2 & MaskGreen) * w2;
            totalRedBlue += (c2 & MaskRedBlue) * w2;

            totalPartsColour += w3;
            totalGreen += (c3 & MaskGreen) * w3;
            totalRedBlue += (c3 & MaskRedBlue) * w3;

            totalGreen /= totalPartsColour;
            totalGreen &= MaskGreen;

            totalRedBlue /= totalPartsColour;
            totalRedBlue &= MaskRedBlue;

            return totalGreen | totalRedBlue;
        }
    }
}
