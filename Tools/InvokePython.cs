using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Python.Runtime;
using System.Diagnostics;
using System.Threading;


namespace ssi {
    class InvokePython
    {

        public static void imageExplainer(string pathModell, string toExplain)
        {

            var pythonPath = @"C:\Program Files\Python36";
            var path = $"{pythonPath};{Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine)}";
            Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);
            PythonEngine.PythonHome += pythonPath;

            //TODO change path variable to relative 
            //TODO add scripts path in front of everything else
            PythonEngine.PythonPath += ";C:\\Users\\Alex Heimerl\\Desktop\\nova\\Scripts";
            //PythonEngine.PythonPath += @";python\scripts\correlations";
            PythonEngine.Initialize();

            using (Py.GIL())
            {
                dynamic os = Py.Import("os");
                dynamic importlib = Py.Import("importlib");
                //string[] fileEntries = Directory.GetFiles("C:\\Users\\Alex Heimerl\\Desktop\\test");
                //fileEntries = Directory.GetFiles("..\\");
                //TODO change Scripts path
                dynamic sys = Py.Import("sys");
                Console.WriteLine(os.getcwd());
                Console.WriteLine(sys.path + "\n");
                Console.WriteLine(sys.version);
                Console.WriteLine(os.__file__);

            }
        }

        //TODO
        private static void textExplainer()
        {

        }

        //TODO
        private static void audioExplainer()
        {

        }

        //Needed for debugging purposes
        static void AttachDebugger()
        {
            Console.WriteLine("waiting for .NET debugger to attach");
            while (!Debugger.IsAttached)
            {
                Thread.Sleep(100);
            }
            Console.WriteLine(".NET debugger is attached");

        }

        public static float correlate(string measure, AnnoList anno1, AnnoList anno2)
        {
            double corr = 0.0;
            double tau = 0.0;

            

            int nValues = Math.Min(anno1.Count, anno2.Count);

            double[] anno1Values = new double[nValues];
            double[] anno2Values = new double[nValues];
            for(int i = 0; i < nValues; i++)
            {
                anno1Values[i] = anno1[i].Score;
                anno2Values[i] = anno2[i].Score;
            }

            List<double> anno1ValuesL = new List<double>();
            List<double> anno2ValuesL = new List<double>();
            for (int i = 0; i < nValues; i++)
            {
                anno1ValuesL.Add(anno1[i].Score);
                anno2ValuesL.Add(anno2[i].Score);
            }

            //var pythonPath = @"C:\\Program Files\\Python36";
            //var path = $"{pythonPath};{Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine)}";
            //Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);
            //PythonEngine.PythonHome += pythonPath;

            ////TODO change path variable to relative 
            ////TODO add scripts path in front of everything else
            //PythonEngine.PythonPath += ";C:\\Users\\Alex Heimerl\\Desktop\\nova\\Scripts";
            //PythonEngine.Initialize();

            using (Py.GIL())
            {
                dynamic np = Py.Import("numpy");
                dynamic spearman = Py.Import("SpearmanR");
                dynamic kendall = Py.Import("KendallTau");


                dynamic sys = PythonEngine.ImportModule("sys");

                //Only for cross debugging needed
                //AttachDebugger();

                //var abc = spearman.print_test();
                //var testn = np.array(anno1Values);
                //var whatever = spearman.annoRet(anno1Values);
                //corr = spearman.correlatetest(anno1Values, anno2Values);
                //Console.WriteLine(whatever);
                tau = kendall.correlate(anno1ValuesL, anno2ValuesL);
                corr = spearman.correlate(anno1ValuesL, anno2ValuesL);
                


            }

            Console.WriteLine(corr);

            return 0.0f;
        }



    }
}
