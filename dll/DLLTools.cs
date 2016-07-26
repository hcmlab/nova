using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ssi
{
    public class DLLTools
    {

#if DEBUG
        [DllImport(@"ssiuitoolsd.dll")]
#else
        [DllImport(@"ssiuitools.dll")]
#endif
        public static extern int InitDLL(UInt32 log_to_file);

#if DEBUG
        [DllImport(@"ssiuitoolsd.dll")]
#else
        [DllImport(@"ssiuitools.dll")]
#endif
        public static extern int ClearDLL(UInt32 log_to_file);    

        // resample
#if DEBUG
        [DllImport(@"ssiuitoolsd.dll")]
#else
        [DllImport(@"ssiuitools.dll")]
#endif
        public static extern int DownSample(UInt32 samples_in,
            UInt32 samples_out,
            UInt32 dimension,            
            [MarshalAs(UnmanagedType.LPArray)] float[,] inptr,            
            [MarshalAs(UnmanagedType.LPArray)] [In, Out] float[,] outptr          
         );        

        // wav
#if DEBUG
        [DllImport(@"ssiuitoolsd.dll")]
#else
        [DllImport(@"ssiuitools.dll")]
#endif
        public static extern int PeakWav(string filename, 
            ref double rate,
            ref UInt32 samples, 
            ref UInt32 dimension
        );

#if DEBUG
        [DllImport(@"ssiuitoolsd.dll")]
#else
        [DllImport(@"ssiuitools.dll")]
#endif
        public static extern int LoadWav(string filename,
            UInt32 samples_in, 
            UInt32 dimension_in, 
            [MarshalAs(UnmanagedType.LPArray)] [In, Out] float[,] ptr
        );

    }
}