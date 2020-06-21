using System;
using System.Collections.Generic;
using System.IO;

namespace DeDup.Core
{
    public static class Log
    {
        public static string StringifyFileLength(this long fileLength)
        {
            const long OneKB = 1024; 
            const long OneMB = OneKB * 1024;
            const long OneGB = OneMB * 1024;
            const long OneTB = OneGB * 1024;
            var scales = new []{OneKB, OneMB, OneGB, OneTB};
            var suffices = new []{"KB", "MB", "GB", "TB"};

            long valRound = 0;
            for (var i = 0; i < scales.Length; i++)
            {
                var scale = scales[i];
                var suffix = suffices[i];
                var val = (double)fileLength / scale;
                if (val < 10)
                {
                    return $"{val:0.00} {suffix}";
                }
                else if (val < 100)
                {
                    return $"{val:00.0} {suffix}";
                }
                valRound = (long)Math.Round(val);
                if (valRound < 1000)
                {
                    return $"{valRound} {suffix}";
                }
            }
            return $"{valRound} {suffices[suffices.Length-1]}";
        }
    }
}
