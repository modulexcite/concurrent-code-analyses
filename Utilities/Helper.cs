﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
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
#if DEBUG
            using (var log = new StreamWriter(file, true))
            {
                log.Write(s);
            }
#endif
        }

        public static void WriteInstance(string file, string id, string ex)
        {
            Helper.WriteLogger(file, id + "\r\n\r\n" + ex + "\r\n------------------------\r\n");
        }
    }
}