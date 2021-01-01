/*
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

using System.Runtime.CompilerServices;

namespace EmulatorUI.Scalers
{
    /// <summary>
    /// Provides cached RGB to YUV lookup without alpha support.
    /// </summary>
    /// <remarks>
    /// This class is public so a user can manually load and unload the lookup table.
    /// Looking up a color calculates the lookup table if not present.
    /// All methods except UnloadLookupTable should be thread-safe, although there will be a performance overhead if GetYUV is called while CreateLookup has not finished.
    /// </remarks>
    public static class RgbYuv
    {
        private static readonly int[] lookupTable;

        static RgbYuv()
        {
            lookupTable = CreateLookup();
        }

        /// <summary>
        /// Returns the 24bit YUV equivalent of the provided 24bit RGB color.
        /// <para>Any alpha component is dropped.</para>
        /// </summary>
        /// <param name="rgb">A 24bit rgb color.</param>
        /// <returns>The corresponding 24bit YUV color.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetYuv(int rgb)
        {
            return lookupTable[rgb];
        }

        public static void Initialize()
        {
            // Calls the static constructor to do the loading
        }

        /// <summary>
        /// Calculates the lookup table.
        /// </summary>
        private static unsafe int[] CreateLookup()
        {
            var lTable = new int[0x1000000]; // 256 * 256 * 256
            fixed (int* lookupP = lTable)
            {
                byte* lP = (byte*)lookupP;
                for (uint i = 0; i < lTable.Length; i++)
                {
                    float r = (i & 0xff0000) >> 16;
                    float g = (i & 0x00ff00) >> 8;
                    float b = (i & 0x0000ff);

                    lP++; //Skip alpha byte
                    *(lP++) = (byte)(.299 * r + .587 * g + .114 * b);
                    *(lP++) = (byte)((int)(-.169 * r - .331 * g + .5 * b) + 128);
                    *(lP++) = (byte)((int)(.5 * r - .419 * g - .081 * b) + 128);
                }
            }

            return lTable;
        }
    }
}