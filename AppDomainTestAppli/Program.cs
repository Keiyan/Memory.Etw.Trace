using System;
using System.Collections.Generic;
using System.Linq;

namespace AppDomainTestAppli
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var appDomainCount = 4;
            if (args.Length > 0) appDomainCount = int.Parse(args[0]);
            if (args.Length > 1) Data.size = int.Parse(args[1]);

            var appD = new List<AppDomain>();
            for (int i = 0; i < appDomainCount; i++)
            {
                appD.Add(AppDomain.CreateDomain("ChildApp" + i));
            }

            var Handles = new List<Data>();
            for (int k = 0; ; k++)
            {
                Console.WriteLine("cycle " + k);
                for (int i = 0; i < appDomainCount; i++)
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        Handles.Add((Data)appD[i].CreateInstanceAndUnwrap("AppDomainTestAppli", "AppDomainTestAppli.Data"));
                    }
                }

                Handles.ForEach(d => d.Dispose());
                Handles.Clear();
                GC.Collect();
            }
        }
    }
}