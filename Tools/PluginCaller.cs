using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ssi
{   
    public class PluginCaller
    {
        object obj = null;
        Type type = null;

        public PluginCaller(string dllPath, string typeName)
        {
            if (!Path.IsPathRooted(dllPath))
            {
                dllPath = Environment.CurrentDirectory + "\\" + dllPath;
            }
            Assembly asm = Assembly.LoadFile(dllPath);
            
            if (asm != null)
            {
                foreach (Type type in asm.GetExportedTypes())
                {
                    if (type.Name == typeName)
                    {
                        this.type = type;
                        obj = Activator.CreateInstance(type);
                        break;
                    }
                }
            }
        }

        public bool call(string name, Dictionary<string,object> args)
        {
            if (obj == null || type == null)
            {
                return false;
            }

            Type[] varInfo = { args.GetType() };            
            MethodInfo methodInfo = type.GetMethod(name, varInfo);
            if (methodInfo == null)
            {
                return false;                               
            }

            type.InvokeMember(name, BindingFlags.InvokeMethod, null, obj, new object[] { args });

            return true;
        }
    }
}
