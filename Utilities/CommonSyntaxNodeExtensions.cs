using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Formatting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Utilities
{
    public static class CommonSyntaxNodeExtensions
    {
        public static T Format<T>(this T node, Workspace workspace) where T : CommonSyntaxNode
        {

            return (T) Formatter.Format(node, workspace);
           
        }
    }
}
