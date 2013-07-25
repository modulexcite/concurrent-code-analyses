using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Compilers.Common;
using Roslyn.Services;
using Roslyn.Services.Formatting;

namespace Utilities
{
    public static class CommonSyntaxNodeExtensions
    {
        public static T Format<T>(this T node) where T : CommonSyntaxNode
        {
            return (T)node.Format(FormattingOptions.GetDefaultOptions()).GetFormattedRoot();
        }
    }
}
