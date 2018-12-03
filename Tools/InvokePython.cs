using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Python.Runtime;


namespace ssi {
    class InvokePython
    {

        public static void imageExplainer(string pathModell, string toExplain)
        {

            var pythonPath = @"C:\\Program Files\\Python36";
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

                dynamic testest = Py.Import("C:\\Users\\Alex Heimerl\\Desktop\\nn_mnist");


                dynamic test = Py.Import("testasdf");

                var tasdf = test.explain();

                //foreach (string fp in Directory.GetFiles("..\\..\\Scripts", "*.py", SearchOption.TopDirectoryOnly).Where(s => !s.Contains("__init__.py")))
                //{
                //    Console.WriteLine(fp);
                //    //importlib.import_module("..\\..\\Scripts\\"+fp);
                //    //dynamic test = Py.Import("C:\\Users\\Alex Heimerl\\Desktop\\nova\\Scripts\\test.py");
                //}
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

        public static float correlate(string measure, AnnoList anno1, AnnoList anno2)
        {
            var corr = 0;

            int nValues = Math.Min(anno1.Count, anno2.Count);

            var anno1Values = new double[nValues];
            var anno2Values = new double[nValues];
            for(int i = 0; i < nValues; i++)
            {
                anno1Values[i] = anno1[i].Score;
                anno2Values[i] = anno2[i].Score;
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
                dynamic spearman = Py.Import("SpearmanR");

                corr = spearman.correlate(anno1Values, anno2Values);
            }

            Console.WriteLine(corr);

            return 0.0f;
        }



    }
}
