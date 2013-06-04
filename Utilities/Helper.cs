﻿using Roslyn.Compilers.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
namespace Utilities
{
    public class Helper
    {
        public static SyntaxNode getNextNode(SyntaxNode node)
        {
            Debug.WriteLine("naber" + node);
            var parent = node.Parent;
            bool isOK = false;

            foreach (var n in parent.ChildNodes())
            {
                if (isOK)
                    return n;
                if (n == node)
                    isOK = true;
            }
            return null;
        }

        public static void WriteLogger(String file, String s)
        {
            using (var log = new StreamWriter(file, true))
            {
                log.Write(s);
            }
        }

        public static void WriteInstance(string file, string id, string ex )
        {
            Helper.WriteLogger(file, id + "\r\n\r\n" + ex + "\r\n------------------------\r\n");
        }
    }
}
