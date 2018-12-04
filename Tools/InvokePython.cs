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

        public static Dictionary<string, double> correlate(AnnoList anno1, AnnoList anno2)
        {

            int nValues = Math.Min(anno1.Count, anno2.Count);

            List<double> anno1ValuesL = new List<double>();
            List<double> anno2ValuesL = new List<double>();
            for (int i = 0; i < nValues; i++)
            {
                anno1ValuesL.Add(anno1[i].Score);
                anno2ValuesL.Add(anno2[i].Score);
            }

            List<string> scriptsName = new List<string>();

            foreach (string fp in Directory.GetFiles("..\\..\\Scripts", "*.py", SearchOption.TopDirectoryOnly).Where(s => !s.Contains("__init__.py")))
            {
                Console.WriteLine(Path.GetFileNameWithoutExtension(fp));
                scriptsName.Add(Path.GetFileNameWithoutExtension(fp));
            }

            List<double> correlations = new List<double>();

            //Dictionary<string, dynamic> scripts = new Dictionary<string, dynamic>();
            Dictionary<string, double> measureValueDic= new Dictionary<string, double>();

            using (Py.GIL())
            {
                dynamic sys = PythonEngine.ImportModule("sys");
                dynamic spearman = Py.Import("SpearmanR");
                dynamic kendall = Py.Import("KendallTau");
                dynamic scriptType = Py.Import("EnumScriptType");
                //List<dynamic> scripts = new List<dynamic>();
                string typeT = scriptType.ScriptType.CORRELATION.name;

                dynamic os = Py.Import("os");
                Console.WriteLine(os.getcwd());


                foreach (string sc in scriptsName)
                {
                    dynamic temp = Py.Import(sc);

                    Console.WriteLine(sc);
                    Console.WriteLine(temp.getType());
                    Console.WriteLine(typeT);

                    if(typeT.Equals((string)temp.getType()))
                    {
                        //scripts[sc] = temp;
                        measureValueDic[sc] = temp.correlate(anno1ValuesL, anno2ValuesL);
                    }
                }

                //Only for cross debugging needed
                //AttachDebugger();
                
            }
            return measureValueDic;
        }



    }
}
