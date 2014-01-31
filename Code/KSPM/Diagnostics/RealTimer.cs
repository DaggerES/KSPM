﻿using System.Diagnostics;

namespace KSPM.Diagnostics
{
    /// <summary>
    /// Holds a stopwatch
    /// </summary>
    public class RealTimer
    {
        /// <summary>
        /// Performance stopwatch
        /// </summary>
        public static Stopwatch Clock = new Stopwatch();

        /// <summary>
        /// Return the current time in a preformatted string.
        /// </summary>
        /// <returns>The current date time with miliseconds as an string.</returns>
        public static string GetCurrentDateTime()
        {
            return System.DateTime.Now.ToString("[dd-MM-yyyy_HH:mm:ss:ffff]");
        }
    }
}
