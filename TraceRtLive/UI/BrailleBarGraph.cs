using System.Text;

namespace TraceRtLive.UI
{
    public static class BrailleBarGraph
    {
        /// <summary>
        /// Create a graph based on Braille unicode characters.
        /// The charted <paramref name="values"/> are scaled to 0 to 4 dots, with
        /// 4 dots corresponding to at least <paramref name="highestValue"/> (or higher, if a passed is higher).
        /// </summary>
        /// <param name="values">Values to graph</param>
        /// <param name="highestValue">Initial highest value</param>
        /// <param name="alignRight">If true, the dots are aligned to the right rather than the left</param>
        public static string CreateGraph(int[] values, int highestValue = 4, bool alignRight = false)
        {
            if (!(values?.Length > 0)) return string.Empty;

            highestValue = Math.Max(highestValue, values.Max()); // at least maxValue, at most the highest value of values[]
            const int lowestValue = 0;
            var isOdd = values.Length % 2 == 1;

            // calculate scaled values
            var scaled = values.Select(v => Scale0to4(v, lowestValue, highestValue));

            // enumerate
            var enumerator = scaled.GetEnumerator();

            var result = new StringBuilder();

            // put in blank if right-aligned
            if (alignRight && isOdd)
            {
                if (!enumerator.MoveNext()) return String.Empty;
                result.Append(GetGraphChar(0, enumerator.Current));
            }

            while (enumerator.MoveNext())
            {
                var a = enumerator.Current;
                var b = enumerator.MoveNext() ? enumerator.Current : 0;

                result.Append(GetGraphChar(a, b));
            }

            return result.ToString();
        }

        /// <summary>
        /// Scale proportionally within 0 to 4, to fit our Braille patterns.
        /// </summary>
        /// <param name="value">Value to scale</param>
        /// <param name="minValue">Minimum value measured (corresponding to 0)</param>
        /// <param name="maxValue">Maximum value measured (corresponding to 4)</param>
        private static int Scale0to4(int value, double minValue, double maxValue)
            // formula: (value - minValue) / (maxValue - minValue) * (rangeMax - rangeMin) + rangeMin
            // where rangeMax = 4, and rangeMin = 0.
            => (int)Math.Round((value - minValue) / (maxValue - minValue) * 4d, 0);

        /// <summary>
        /// Convert two values to Braille graph characters.
        /// Supports only values 0 through 4.
        /// </summary>
        /// <param name="value1">The value for the first half of the character</param>
        /// <param name="value2">The value for the last half of the character</param>
        /// <returns></returns>
        private static char GetGraphChar(int value1, int value2)
            => (((value1 & 0x7) << 4) | (value2 & 0x7)) switch
            {
                0x00 => '\u2800', //  ⠀  Braille Pattern Blank
                0x01 => '\u2880', //  ⢀  Braille Pattern Dots-8
                0x02 => '\u28A0', //  ⢠  Braille Pattern Dots-68
                0x03 => '\u28B0', //  ⢰  Braille Pattern Dots-568
                0x04 => '\u28B8', //  ⢸  Braille Pattern Dots-4568

                0x10 => '\u2840', //  ⡀  Braille Pattern Dots-7
                0x11 => '\u28C0', //  ⣀  Braille Pattern Dots-78
                0x12 => '\u28E0', //  ⣠  Braille Pattern Dots-678
                0x13 => '\u28F0', //  ⣰  Braille Pattern Dots-5678
                0x14 => '\u28F8', //  ⣸  Braille Pattern Dots-45678

                0x20 => '\u2844', //  ⡄  Braille Pattern Dots-37
                0x21 => '\u28C4', //  ⣄  Braille Pattern Dots-378
                0x22 => '\u28E4', //  ⣤  Braille Pattern Dots-3678
                0x23 => '\u28F4', //  ⣴  Braille Pattern Dots-35678
                0x24 => '\u28FC', //  ⣼  Braille Pattern Dots-345678

                0x30 => '\u2846', //  ⡆  Braille Pattern Dots-237
                0x31 => '\u28C6', //  ⣆  Braille Pattern Dots-2378
                0x32 => '\u28E6', //  ⣦  Braille Pattern Dots-23678
                0x33 => '\u28F6', //  ⣶  Braille Pattern Dots-235678
                0x34 => '\u28FE', //  ⣾  Braille Pattern Dots-2345678

                0x40 => '\u2847', //  ⡇  Braille Pattern Dots-1237
                0x41 => '\u28C7', //  ⣇  Braille Pattern Dots-12378
                0x42 => '\u28E7', //  ⣧  Braille Pattern Dots-123678
                0x43 => '\u28F7', //  ⣷  Braille Pattern Dots-1235678
                0x44 => '\u28FF', //  ⣿  Braille Pattern Dots-12345678

                _ => '?',
            };
    }
}
