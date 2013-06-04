#define LOG
//#define TEST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System.IO;
using System.Threading.Tasks;

using System.Threading;
using System.Collections;
using Roslyn.Compilers.Common;
using System.Reflection;

namespace TPLAnalyzer
{
    class Project
    {
        public static Dictionary<string, int> tplUsage;
        public static Dictionary<string, int> threadingUsage;
        public static Dictionary<string, int> colConUsage;
        public static Dictionary<string, int> plinqUsage;


        public static string[] blacklist = { "models.Models.", "ExecuteNonQuery", "_users.First", "blog.AddComment", "this.CreateView", "GetContainedCommands()", "propertyForEmptyClass", "modelBuilder.Configurations", "_settingService", "configurationTypes.Select" , "resultForWhere","SetState", "SingleTypeQuery", "queryClass.", "RegisterExecutor", "should_be", "CreateView", "models.", "result.", "CreateConventional", "comments.", "seed.", "Comments", "blogs.Single" ,".Games", ".Single", "games", "gameLibrary", "CreateCustomer", "users.", "Friends()", "handles.",  "Value.Count", "_channels.Single", "dc.Keys.Any","dc.ContainsKey" };
        public static string[] specificFingerprints = { "Task", "IProducerConsumerCollection", "TaskScheduler", "ITask" };

        public static string[] createTaskFingerprints = { "new Task", "StartNew", "ContinueWith", "FromAsync", ".Start", ".Result" };

        public static HashSet<string> fingerprints;
        public HashSet<string> setDocuments = new HashSet<string>();


        public static StreamWriter objWriter;



        public ushort nProjects;
        public ushort nLoadErrorP;
        public ushort nCompileErrorP;
        public ushort nCompiledProjects;

        public ushort nNet4Projects;
        public ushort nNetOtherProjects;


        public ushort nFiles;
        public ushort nSingleFiles;

        public ushort nApiF;
        public ushort nAnalyzedFiles;

        public ushort nHasTplFiles;
        public ushort nHasSysThreadingFiles;

        public ushort nCreateThreadFiles;
        public ushort nCreateTasksFiles;

        public int realLOC;

        public ushort numVolatileStatements;
        public ushort numLockStatements;

        public string folderName;
        public string author;
        public bool isGithub;

        public string sourceTreeLink;
        System.Timers.Timer timer = new System.Timers.Timer(10000);
        public bool isUseAsParallel;

        ~Project()
        {
            setDocuments = null;
            objWriter = null;

        }
        public Project(String name)
        {

            folderName = name;
            if (folderName.Contains(","))
            {
                isGithub = true;
                author = name.Split(',')[0];
                folderName = name.Split(',')[1];
            }
            else
                isGithub = false;

            GetSourceTreeLink();
            //            timer.Elapsed += new System.Timers.ElapsedEventHandler((arg1, arg2) =>
            //            {
            //                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\\Users\semih\Desktop\blacklist.txt", true))
            //                {
            //                    file.WriteLine(folderName.Replace(",","+")+","+MyWalker.error);
            //                }
            //                Environment.Exit(0);
            //            }
            //);
        }

        private void GetSourceTreeLink()
        {
            string[] lines = System.IO.File.ReadAllLines(GetDir() + @"\info.txt");
            sourceTreeLink = @"https://github.com" + lines[2].Replace("zipball", "blob") + "/";
        }

        public void Start()
        {

            StartAnalysis(GetDir());
        }
        public void Start(String path)
        {

            StartAnalysis(path);
        }

        internal string GetReport()
        {
            string line = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}",
                folderName.Replace(",", "+"),
                isGithub,
                GetDates(),
                nProjects,
                nLoadErrorP,
                nCompileErrorP,
                nCompiledProjects,
                nNet4Projects,
                nNetOtherProjects,
                nFiles,
                nSingleFiles,
                nApiF,
                nAnalyzedFiles,
                nHasSysThreadingFiles,
                nHasTplFiles,
                nCreateThreadFiles,
                nCreateTasksFiles,
                determineSize(),
                realLOC
                );

            return line;
        }

        internal string GetOverallUsageReport()
        {
            string line = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                folderName.Replace(",", "/"),
                isGithub,
                GetDates(),
                nHasSysThreadingFiles,
                nHasTplFiles,
                threadingUsage.Sum(a => a.Value),
                plinqUsage.Sum(a => a.Value),
                tplUsage.Sum(a => a.Value),
                determineSize(),
                realLOC
                );
            return line;
        }

        internal string GetDetailedUsageReport()
        {
            String line = folderName + ",";
            foreach (var pair in threadingUsage)
            {
                line += pair.Value + ",";

            }
            foreach (var pair in tplUsage)
            {
                line += pair.Value + ",";
            }
            foreach (var pair in plinqUsage)
            {
                line += pair.Value + ",";
            }
            foreach (var pair in colConUsage)
            {
                line += pair.Value + ",";
            }

            return line;
        }


        private string GetDates()
        {
            string[] lines = System.IO.File.ReadAllLines(GetDir() + @"\info.txt");
            if (isGithub)
                return lines[3].Replace("\n", "") + "," + lines[4].Replace("\n", "");
            else
                return lines[2].Replace("\n", "") + "," + lines[3].Replace("\n", "");
        }

        private string GetDir()
        {
            if (isGithub)
                return @"D:\\a\" + author + "," + folderName;
            return @"D:\\a\" + folderName;
        }



        public void StartAnalysis(string dir)
        {

            if (!dir.EndsWith("tags") && !dir.EndsWith("branch"))
            {
                foreach (string f in Directory.GetFiles(dir, "*.csproj"))
                {

                    ++nProjects;
                    compileProject(f);
                }
                foreach (string d in Directory.GetDirectories(dir))
                {
                    StartAnalysis(d);
                }
            }

        }

        public void compileProject(String path)
        {
            IProject project = null;

            try
            {
                project = Roslyn.Services.Solution.LoadStandAloneProject(path);

            }
            catch (Microsoft.Build.Exceptions.InvalidProjectFileException e)
            {
                nLoadErrorP++;
                //Console.WriteLine("*******Project Not Found Exception*********");

            }
            catch (System.FormatException e)
            {
                nLoadErrorP++;
                // Console.WriteLine("*******Project Format Error Exception*********");
            }


            if (project != null)
            {
                ICompilation compilation = null;
                try
                {

                    compilation = project.GetCompilation();

                }
                catch (Microsoft.Build.Exceptions.InvalidProjectFileException e)
                {
                    // Console.WriteLine(project.DisplayName);    
                }


                if (compilation == null)
                {
                    // Console.WriteLine("Compilation NULL");
                    nCompileErrorP++;
                }
                else
                {
                    nCompiledProjects++;

                    String versionNumber = "";
                    try
                    {
                        versionNumber = compilation.GetTypeByMetadataName("System.Object").ContainingAssembly.AssemblyName.Version.ToString();
                    }
                    catch (Exception e)
                    {

                    }

                    if (versionNumber.StartsWith("4"))
                    {
                        nNet4Projects++;
                    }
                    else
                    {
                        nNetOtherProjects++;
                    }
                    processProject(project, compilation);
                }
            }
        }


        private void processProject(IProject project, ICompilation compilation)
        {

            nFiles += ushort.Parse(project.Documents.Count() + "");


            foreach (var document in project.Documents)
            {

                if (!setDocuments.Contains(document.Id.FileName))
                {
                    nSingleFiles++;
                    setDocuments.Add(document.Id.FileName);
                    processDocument(document, compilation);
                }
            }
        }

        public void processDocument(IDocument document, ICompilation compilation)
        {

            var syntaxTree = document.GetSyntaxTree();

            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var walker = new MyWalker();

            string path = document.Id.FileName;
            path = path.Substring(path.IndexOf("src\\") + 4, path.Length - path.IndexOf("src") - 4);
            walker.sourceTreeLink = sourceTreeLink + path;
            walker.isGithub = isGithub;
            walker.semanticModel = semanticModel;
            walker.root = ((SyntaxTree)syntaxTree).Root;
            walker.fileName = document.DisplayName;

            walker.folderName = folderName;
            walker.timer = timer;
            walker.sourceCode = document.GetText();
            walker.Visit(((SyntaxTree)syntaxTree).Root);



            if (!walker.isApiFile)
            {
                nAnalyzedFiles++;

                if (walker.hasTPL)
                {
                    nHasTplFiles++;
                }
                if (walker.hasThreading)
                {
                    nHasSysThreadingFiles++;
                }

                //isUseAsParallel |= walker.isUseAsParallel;
                //numLockStatements += walker.lockStatements;
                //numVolatileStatements += walker.volatileStatements;
            }
            else
            {
                nApiF++;
            }


            realLOC += walker.loc;
        }

        public String determineSize()
        {
            if (realLOC == 0)
                return "EMPTY";
            else if (realLOC < 1000)
                return "TOY";
            else if (realLOC < 10000)
                return "SMALL";
            else if (realLOC < 100000)
                return "MEDIUM";
            else
                return "BIG";

        }





        internal class MyWalker : SyntaxWalker
        {
            public ISemanticModel semanticModel;
            public SyntaxNode root;
            public IText sourceCode;

            public string folderName;
            public string fileName;
            public string sourceTreeLink;

            public int loc;

            public bool isApiFile = false;

            public bool hasTPL = false;
            public bool hasThreading = false;
            public bool isGithub;

            public ushort lockStatements = 0;
            public ushort volatileStatements = 0;

            public bool isUseAsParallel = false;

            private string methodName;
            public System.Timers.Timer timer;
            public static string error;


            ~MyWalker()
            {
                semanticModel = null;
            }


            protected override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                if (node.Name.GetText().StartsWith("System.Threading"))
                {
                    isApiFile = true;
                }
                base.VisitNamespaceDeclaration(node);
            }

            //protected override void VisitToken(SyntaxToken token)
            //{
            //    if (token.TrailingTrivia.Where(n => n.Kind == SyntaxKind.EndOfLineTrivia).Count() > 0)
            //    {
            //        loc++;
            //    }
            //}
            //protected override void VisitUsingDirective(UsingDirectiveSyntax node)
            //{
            //    if (!isApiFile)
            //    {
            //        if (node.Name.GetText() == "System.Threading.Tasks")
            //        {
            //            hasTPL = true;
            //        }
            //        else if (node.Name.GetText() == "System.Threading")
            //        {
            //            hasThreading = true;
            //        }
            //    }
            //    base.VisitUsingDirective(node);
            //}

            //protected override void VisitMethodDeclaration(MethodDeclarationSyntax node)
            //{
            //    methodName = node.Identifier.GetText();
            //    base.VisitMethodDeclaration(node);
            //}


            //protected override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            //{
            //    if (!isApiFile)
            //    {
            //        //if (node.Name.GetText() == "AsParallel")
            //        //{

            //        //    objWriter = new System.IO.StreamWriter(@"X:\\TaskProjects\AsParallelPattern.txt", true);

            //        //    if (node.Ancestors().OfType<InvocationExpressionSyntax>().Any(n => n.GetText().Contains(".Sum") || n.GetText().Contains("Aggregate")))
            //        //        objWriter.WriteLine("aggregate");
            //        //    else
            //        //        objWriter.WriteLine("forloop");

            //        //    objWriter.Close();
            //        //}
            //        //else if (node.Expression.GetText() == "TaskCreationOptions")
            //        //{
            //        //    objWriter = new System.IO.StreamWriter(@"X:\\TaskProjects\taskCreationOptions.txt", true);
            //        //    objWriter.WriteLine(node.Name);
            //        //    objWriter.Close();
            //        //}
            //        //else if (node.Expression.GetText() == "TaskContinuationOptions")
            //        //{
            //        //    objWriter = new System.IO.Stre amWriter(@"X:\\TaskProjects\taskContinuationOptions.txt", true);
            //        //    objWriter.WriteLine(node.Name);
            //        //    objWriter.Close();
            //        //}
            //    }
            //    base.VisitMemberAccessExpression(node);
            //}


            //protected override void VisitBaseList(BaseListSyntax node)
            //{
            //    if (!isApiFile)
            //    {
            //        //foreach (var a in node.Types)
            //        //{

            //        //    if (specificFingerprints.Any(b => a.PlainName == b))
            //        //    {

            //        //        objWriter = new System.IO.StreamWriter(@"X:\\TaskProjects\extending.txt", true);
            //        //        objWriter.WriteLine(a.GetText() + "," + fileName + "," + folderName);
            //        //        objWriter.Close();
            //        //    }

            //        //}
            //    }


            //    base.VisitBaseList(node);
            //}


            protected override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                
                if (!isApiFile)
                {
                    IsParallelConstruct(node);

                    //IsTaskPattern(node);
                    //IsParallelInvokeMethod(node);
                    //IsTaskCreated(node);
                    //IsAsParallel(node);
                    //IsWaitAll(node);
                    //isEnqueue(node);
                    //IsTaskCompletionSource(node);
                }
                base.VisitInvocationExpression(node);
            }

            protected override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {

                if (!isApiFile)
                {
                    //try
                    //{
                    //    ISemanticInfo s = null;
                    //    if (node.GetText().Contains("BlockingCollection"))
                    //        s = semanticModel.GetSemanticInfo(node);
                    //    if (s != null && s.Type != null && s.Type.Name == "BlockingCollection")
                    //    {
                    //        var methods = node.Ancestors().OfType<MethodDeclarationSyntax>();
                    //        if (methods.Count() > 0 && createTaskFingerprints.Any(a => methods.First().GetText().Contains(a)))
                    //        {
                    //            objWriter = new System.IO.StreamWriter(@"X:\\TaskProjects\pipeline.txt", true);
                    //            objWriter.WriteLine(AddSignature());
                    //            objWriter.WriteLine(methods.First().GetText());
                    //            objWriter.Close();
                    //        }

                    //    }
                    //}
                    //catch (System.ArgumentOutOfRangeException e)
                    //{
                    //}
                    //catch (System.NullReferenceException e)
                    //{
                    //}
                    //catch (System.Exception e)
                    //{
                    //}
                    IsParallelConstruct(node);
                    //IsTaskCreated(node);
                    //IsTaskCompletionSource(node);
                }


                base.VisitObjectCreationExpression(node);
            }

            //protected override void VisitIdentifierName(IdentifierNameSyntax node)
            //{
            //    try
            //    {
            //        if (!isApiFile)
            //        {
            //            error = node.GetText();

            //            timer.Start();
            //            if (blacklist.Any(n => n.Split(',')[0] == folderName.Replace(",", "+") && ( node.GetText().Contains(n.Split(',')[1]) || n.Split(',')[1].Contains(node.GetText()))))
            //                return;

            //            var a = semanticModel.GetSemanticInfo(node);
            //            foreach (var loc in a.Symbol.Locations)
            //            {
            //                string txt = loc.SourceTree.Text.GetLineFromPosition(loc.SourceSpan.Start).GetText();
            //                if (txt.Contains("volatile"))
            //                    volatileStatements++;
            //            }
            //            timer.Stop();
            //            timer.Interval = 10000;
            //        }
            //    }
            //    catch (System.Exception e)
            //    {

            //    }

            //    base.VisitIdentifierName(node);
            //}
            //protected override void VisitLockStatement(LockStatementSyntax node)
            //{
            //    if (!isApiFile)
            //    {
            //        lockStatements++;
            //    }
            //    base.VisitLockStatement(node);
            //}


            /*
             *  Helpers
            */


            public void IsParallelConstruct(SyntaxNode node)
            {

                ISemanticInfo semanticInfo = null;
                try
                {
                    if (node is InvocationExpressionSyntax)
                    {
                        var s = node.ChildNodes();
                        InvocationExpressionSyntax invoc = (InvocationExpressionSyntax)node;

                        if (fingerprints.Any((n) => invoc.Expression.GetText().Contains(n)) && !blacklist.Any(n => invoc.Expression.GetText().Contains(n)))
                        {

                            //Console.WriteLine(invoc.Expression.GetText());                   

                            semanticInfo = semanticModel.GetSemanticInfo(((InvocationExpressionSyntax)node));
                        }


                    }
                    else if (node is ObjectCreationExpressionSyntax)
                    {
                        ObjectCreationExpressionSyntax create = (ObjectCreationExpressionSyntax)node;
                        if (fingerprints.Any((n) => create.Type.PlainName == n))
                        {
                            semanticInfo = semanticModel.GetSemanticInfo((ObjectCreationExpressionSyntax)node);
                        }
                    }


                    if (semanticInfo != null)
                    {
                        Symbol sym = (Symbol)semanticInfo.Symbol;

                        if (sym == null)
                        {
                            try
                            {
                                sym = (Symbol)semanticInfo.CandidateSymbols.First(a => a.ContainingNamespace.ToDisplayString().Contains("System.Threading") || a.ContainingNamespace.ToDisplayString().Contains("System.Linq") || a.ContainingNamespace.ToDisplayString().Contains("System.Collections.Concurrent"));
                            }
                            catch (System.InvalidOperationException e) { }
                        }
                        Dictionary<string, int> list = null;
                        if (sym != null && sym.Kind != SymbolKind.ErrorType)
                        {
                            var ns = sym.ContainingNamespace.ToDisplayString();

                            if (ns == "System.Threading.Tasks")
                                list = tplUsage;
                            else if (ns == "System.Threading")
                                list = threadingUsage;
                            else if (ns == "System.Linq")
                                list = plinqUsage;
                            else if (ns == "System.Collections.Concurrent")
                                list = colConUsage;


                            if (list != null && list.ContainsKey(sym.OriginalDefinition.ToString()))
                            {
                                list[sym.OriginalDefinition.ToString()]++;
                                var className = sym.OriginalDefinition.ToString().Replace("System.Threading.Tasks.", "").Replace("System.Threading.", "").Replace("System.Linq.", "").Replace("System.Collections.Concurrent.", "").Split('.')[0].Replace("<","(").Replace(">",")");
                                var methodName = sym.OriginalDefinition.ToString().Replace("System.Threading.Tasks.", "").Replace("System.Threading.", "").Replace("System.Linq.", "").Replace("System.Collections.Concurrent.", "").Split('.')[1].Split('<')[0].Split('(')[0];
                                if (isGithub)
                                {
                                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\\Users\\semih\\Desktop\\newExamples\\"+ className +"-" +methodName+"-" + Program.getIndex(sym.OriginalDefinition.ToString()) + ".html", true))
                                    {


                                        int startLineNumber = sourceCode.GetLineNumberFromPosition(node.FullSpan.Start) + 1;

                                        int endLineNumber = sourceCode.GetLineNumberFromPosition(node.FullSpan.End) + 1;


                                        string link = sourceTreeLink + "#L" + startLineNumber + "-" + endLineNumber;

                                        file.WriteLine(String.Format("<tr>\n    <td>{0}</td>\n    <td>{1}</td>\n    <td>{2}</td>\n    <td><a href=\"{3}\">Link To Source File</a></td>\n</tr>\n", folderName, fileName, node.ToString(), link));



                                    }
                                }

                            }
                           

                        }
                    }



                }
                catch (System.ArgumentOutOfRangeException e)
                {
                }
                catch (System.NullReferenceException e)
                {
                }
                catch (System.ArgumentException e)
                {
                }
                catch (System.InvalidCastException e)
                {
                }
                catch (System.IndexOutOfRangeException e)
                {
                }


            }

            private string AddSignature()
            {
                return "------------------\n" + folderName + "," + fileName + "," + methodName + "\n\n\n";
            }

            private bool FindSpecificMethod(InvocationExpressionSyntax node, string[] methodName, string namespaceName)
            {
                ISemanticInfo semanticInfo = null;

                try
                {

                    var s = node.ChildNodes();
                    if (s != null && s.First() is MemberAccessExpressionSyntax && methodName.Any<string>((n) => (s.First() as MemberAccessExpressionSyntax).Name.GetText() == n))
                    {

                        semanticInfo = semanticModel.GetSemanticInfo((InvocationExpressionSyntax)node);

                    }
                    if (semanticInfo != null)
                    {
                        Symbol sym = (Symbol)semanticInfo.Symbol;

                        if (sym == null)
                        {
                            sym = (Symbol)semanticInfo.CandidateSymbols.First(a => a.ContainingNamespace.ToDisplayString() == namespaceName);
                        }
                        if (sym != null && sym.Kind != SymbolKind.ErrorType)
                        {

                            var ns = sym.ContainingNamespace.ToDisplayString();
                            if (ns == namespaceName)
                            {
                                return true;
                            }
                        }
                    }



                }
                catch (System.ArgumentOutOfRangeException e)
                {
                    //Console.WriteLine("*******Argument Exception");

                }
                catch (System.NullReferenceException e)
                {
                    Console.WriteLine("*******NullReference Exception");

                }
                catch (System.Exception e)
                {
                    //Console.WriteLine("*******Exception");
                }

                return false;
            }




            /*
             *  Patterns
            */

            public void IsTaskPattern(InvocationExpressionSyntax node)
            {
                string[] waitName = { "WaitAll", "Wait" };
                if (FindSpecificMethod(node, waitName, "System.Threading.Tasks"))
                {
                    IsForkJoin(node);
                    if (node.GetText().Contains("WaitAll"))
                    {



                        objWriter = new System.IO.StreamWriter(@"X:\\TaskProjects\waitAll.txt", true);
                        if (node.Ancestors().OfType<MethodDeclarationSyntax>().Count() > 0)
                        {
                            objWriter.WriteLine(AddSignature());
                            objWriter.WriteLine(node.Ancestors().OfType<MethodDeclarationSyntax>().First().GetText());

                        }
                        objWriter.Close();
                    }
                    else
                    {

                        objWriter = new System.IO.StreamWriter(@"X:\\TaskProjects\wait.txt", true);
                        if (node.Ancestors().OfType<MethodDeclarationSyntax>().Count() > 0)
                        {
                            objWriter.WriteLine(AddSignature());
                            objWriter.WriteLine(node.Ancestors().OfType<MethodDeclarationSyntax>().First().GetText());
                        }
                        objWriter.Close();
                    }


                    //var parent = node.Parent;
                    //var block = parent.Parent;
                    //SyntaxNode tmp = null;
                    //SyntaxNode prevNode = null;
                    //foreach (var exp in block.ChildNodes())
                    //{
                    //    if (exp == parent)
                    //    {
                    //        prevNode = tmp;
                    //        break;
                    //    }
                    //    tmp = exp;
                    //}

                    //if (prevNode.DescendentNodes().OfType<InvocationExpressionSyntax>().Where(a => a.Ancestors().OfType<ParenthesizedLambdaExpressionSyntax>().Count() == 0)
                    //    .All(a =>
                    //    a.DescendentNodes().OfType<IdentifierNameSyntax>().Any(
                    //    b => b.PlainName == "StartNew" || b.PlainName == "Start")))
                    //{
                    //    Console.WriteLine(prevNode.GetFullText());
                    //    Console.WriteLine("-----");
                    //    //var objWriter = new System.IO.StreamWriter(@"C:\\catched.txt", true);
                    //    //objWriter.WriteLine(prevNode.GetFullText()+"\n\n\n"+ node.GetFullText());
                    //    // objWriter.WriteLine("------------\n");
                    //    // objWriter.Close();

                    //}
                }



            }

            private void IsForkJoin(InvocationExpressionSyntax node)
            {
                IEnumerable<SyntaxNode> methods = node.Ancestors().OfType<MethodDeclarationSyntax>();


                if (methods.Count() == 0)
                {
                    methods = node.Ancestors().OfType<ConstructorDeclarationSyntax>();
                }


                if (methods.Count() > 0)
                {
                    var block = methods.First().ChildNodes().OfType<BlockSyntax>().First();


                    SyntaxNode start = null;
                    foreach (var stmt in block.ChildNodes())
                    {
                        if (stmt.DescendentNodes().OfType<InvocationExpressionSyntax>().Any(a => a == node))
                        {
                            if (start == null)
                            {
                                if (createTaskFingerprints.Any(a => stmt.GetText().Contains(a)))
                                {
                                    start = stmt;
                                }
                                else
                                {

                                    objWriter = new System.IO.StreamWriter(@"X:\\TaskProjects\notTaskPattern.txt", true);
                                    objWriter.WriteLine(AddSignature());
                                    objWriter.WriteLine(block.GetText());
                                    objWriter.Close();
                                }
                            }

                            if (start != null) // previous method can change start!
                            {
                                IsConvertible2Invoke(start, stmt, block);
                                FindTaskPatternType(start, stmt, block);
                            }
                            break;
                        }
                        else
                        {
                            if (createTaskFingerprints.Any(a => stmt.GetText().Contains(a)) && start == null)
                            {
                                start = stmt;
                            }
                        }


                    }



                }

            }

            private void FindTaskPatternType(SyntaxNode start, SyntaxNode stmt, BlockSyntax block)
            {
                string methodName = "";
                if (block.Parent is MethodDeclarationSyntax)
                {
                    methodName = ((MethodDeclarationSyntax)block.Parent).Identifier.GetText();
                }

                bool isPrint = false;
                bool isDynamicPattern = false;
                bool isFuturePattern = false;
                bool isPipeline = false;
                string printLine = AddSignature();
                foreach (var tmp in block.ChildNodes())
                {
                    if (start == tmp)
                        isPrint = true;
                    if (isPrint)
                    {
                        printLine += tmp.GetText();

                        //TODO look at parallel.invoke for dynamic tasks 

                        // check whether it is Dynamic Task Par!

                        // continuewith, startnew!
                        var createTaskInvocs = tmp.DescendentNodes().OfType<InvocationExpressionSyntax>().Where(a => a.Expression.GetText().Contains("StartNew") || a.Expression.GetText().Contains("ContinueWith"));

                        if (createTaskInvocs.Any(invoc => invoc.DescendentNodes().OfType<InvocationExpressionSyntax>().Any(a => a.Expression.GetText() == methodName)))
                            isDynamicPattern = true;



                        // new Task
                        var createTaskCons = tmp.DescendentNodes().OfType<ObjectCreationExpressionSyntax>().Where(a => a.Type.GetText().Contains("Task"));

                        if (createTaskCons.Any(invoc => invoc.DescendentNodes().OfType<InvocationExpressionSyntax>().Any(a => a.Expression.GetText() == methodName)))
                            isDynamicPattern = true;


                        //check whether it is future

                        if (tmp.GetText().Contains("Task<") || tmp.GetText().Contains(".Result") || tmp.GetText().Contains("ContinueWith<") || tmp.GetText().Contains("StartNew<"))
                            isFuturePattern = true;


                        // check pipeline


                        if (tmp.GetText().Contains("BlockingCollection"))
                            isPipeline = true;



                    }
                    if (stmt == tmp)
                        isPrint = false;
                }

                objWriter = new System.IO.StreamWriter(@"X:\\TaskProjects\taskPattern.txt", true);
                objWriter.WriteLine(printLine);
                objWriter.Close();

                if (isFuturePattern)
                {
                    objWriter = new System.IO.StreamWriter(@"X:\\TaskProjects\futurePattern.txt", true);
                    objWriter.WriteLine(printLine);
                    objWriter.Close();
                }
                if (isDynamicPattern)
                {
                    objWriter = new System.IO.StreamWriter(@"X:\\TaskProjects\dynamicPattern.txt", true);
                    objWriter.WriteLine(printLine);
                    objWriter.Close();
                }

                if (isPipeline)
                {
                    objWriter = new System.IO.StreamWriter(@"X:\\TaskProjects\pipelineForkJoin.txt", true);
                    objWriter.WriteLine(printLine);
                    objWriter.Close();
                }

            }


            /*
             *  Complexity
            */

            private void IsConvertible2Invoke(SyntaxNode start, SyntaxNode stmt, BlockSyntax block)
            {
                SyntaxNode prev = stmt;
                bool isPrint = false;
                foreach (var tmp in block.ChildNodes())
                {
                    if (start == tmp)
                        isPrint = true;
                    if (isPrint)
                    {
                        if (tmp.GetText().Contains("TaskCreationOptions") || tmp.GetText().Contains("TaskContinuationOptions") || tmp.GetText().Contains(".Result") ||
                            tmp.GetText().Contains("ContinueWith") || tmp.GetText().Contains("new Task<") || tmp.GetText().Contains("StartNew<"))
                            return;

                    }
                    if (tmp == stmt)
                        break;
                    prev = tmp;
                }

                var invocations = prev.DescendentNodes().OfType<InvocationExpressionSyntax>();

                if (invocations.Any(a => a.Expression.GetText().Contains(".Start")))
                {
                    objWriter = new System.IO.StreamWriter(@"X:\\TaskProjects\convertibleInvoke.txt", true);
                    objWriter.WriteLine(AddSignature());
                    objWriter.WriteLine(block.GetText());
                    objWriter.Close();
                }
            }



            /*
             *  Misusage
            */

            private void IsParallelForEach(InvocationExpressionSyntax node)
            {
                string[] asparallel = { "ForEach" };

                if (FindSpecificMethod(node, asparallel, "System.Threading.Tasks"))
                {
                    var block = node.FirstAncestorOrSelf<BlockSyntax>();

                    if (block != null)
                    {
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\\Users\semih\Desktop\parallelForEach.txt", true))
                        {
                            file.WriteLine("\n-------------------- :" + folderName + "\n" + block.GetText());
                        }
                    }
                }
            }


            private void IsAsParallel(InvocationExpressionSyntax node)
            {
                string[] asparallel = { "AsParallel" };

                if (FindSpecificMethod(node, asparallel, "System.Linq"))
                {

                    isUseAsParallel = true;

                    //var anc = node.FirstAncestorOrSelf<BlockSyntax>();

                    //using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\\Users\semih\Desktop\asParallel.txt", true))
                    //{
                    //    file.WriteLine("\n-------------------- :" + folderName + "\n" + anc.Parent);
                    //}  
                }
            }


            private void IsTaskCompletionSource(ObjectCreationExpressionSyntax node)
            {
                try
                {
                    ISemanticInfo s = null;
                    if (node.GetText().Contains("TaskCompletionSource"))
                        s = semanticModel.GetSemanticInfo(node);
                    if (s != null && s.Type != null && s.Type.Name == "TaskCompletionSource")
                    {

                        var anc = node.FirstAncestorOrSelf<BlockSyntax>();

                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\\Users\semih\Desktop\taskCompletionSource.txt", true))
                        {
                            file.WriteLine("\n-------------------- :" + folderName + "\n" + anc.Parent);
                        }
                    }
                }
                catch (System.ArgumentOutOfRangeException e)
                {
                }
                catch (System.NullReferenceException e)
                {
                }
                catch (System.Exception e)
                {
                }





            }


            private void IsWaitAll(InvocationExpressionSyntax node)
            {
                string[] asparallel = { "WaitAll" };

                if (FindSpecificMethod(node, asparallel, "System.Threading.Tasks"))
                {
                    var tmp = node.Ancestors().Where(a => a is ParenthesizedLambdaExpressionSyntax);

                    if (tmp.Count() > 0)
                    {
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\\Users\semih\Desktop\waitAllUnderTask.txt", true))
                        {
                            file.WriteLine("\n-------------------- :" + folderName + "\n" + tmp.First().FirstAncestorOrSelf<BlockSyntax>().Parent);
                        }
                    }
                }
            }

            private void isEnqueue(InvocationExpressionSyntax node)
            {
                string[] asparallel = { "Enqueue" };

                if (FindSpecificMethod(node, asparallel, "System.Collections.Concurrent"))
                {

                    var anc = node.FirstAncestorOrSelf<BlockSyntax>();

                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\\Users\semih\Desktop\concurrentQueue.txt", true))
                    {
                        file.WriteLine("\n-------------------- :" + folderName + "\n" + anc.Parent);
                    }
                }

            }

            public void IsTaskCreated(SyntaxNode node)
            {
                if (node is InvocationExpressionSyntax)
                {
                    string[] taskCreateFng = { "StartNew" }; //, "ContinueWith" };

                    if (FindSpecificMethod(node as InvocationExpressionSyntax, taskCreateFng, "System.Threading.Tasks"))
                    {
                        CheckMisusageLocalValue(node);
                    }
                }
                else if (node is ObjectCreationExpressionSyntax)
                {
                    try
                    {
                        ISemanticInfo s = null;
                        if (node.GetText().Contains("new Task"))
                            s = semanticModel.GetSemanticInfo(node);
                        if (s != null && s.Type != null && s.Type.Name == "Task")
                        {
                            CheckMisusageLocalValue(node);
                        }
                    }
                    catch (System.ArgumentOutOfRangeException e)
                    {
                    }
                    catch (System.NullReferenceException e)
                    {
                    }
                    catch (System.Exception e)
                    {
                    }
                }
            }

            public void CheckMisusageLocalValue(SyntaxNode node)
            {
                var lambdas = node.DescendentNodes().OfType<ParenthesizedLambdaExpressionSyntax>();

                var forstatements = node.Parent.Ancestors().OfType<ForEachStatementSyntax>();

                foreach (var forstmt in forstatements)
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\\Users\semih\Desktop\complexity\foreachloopTask.txt", true))
                    {
                        file.WriteLine("\n-------------------- :" + folderName + "\n" + forstmt.ToString());
                    }
                    //var initialize = forstmt.Initializers.Select(a => { if (a is BinaryExpressionSyntax) return (a as BinaryExpressionSyntax).Left; else return null; });
                    //foreach (var tmp in initialize)
                    //{
                    //    if (tmp != null && tmp is IdentifierNameSyntax)
                    //    {
                    //        IdentifierNameSyntax id = tmp as IdentifierNameSyntax;
                    //        ISemanticInfo semanticInfo = semanticModel.GetSemanticInfo(id);
                    //        Symbol sym = null;
                    //        if (semanticInfo != null)
                    //        {
                    //            sym = (Symbol)semanticInfo.Symbol;
                    //        }

                    //        if (lambdas.Count() > 0)
                    //        {
                    //            var lambda = lambdas.First();
                    //            var dataflow = semanticModel.AnalyzeRegionDataFlow(lambda.Span);


                    //            foreach (var dataIn in dataflow.DataFlowsIn)
                    //            {
                    //                if (dataIn == sym)
                    //                {
                    //                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\\Users\semih\Desktop\misusage\forloopTaskmisusage.txt", true))
                    //                    {
                    //                        file.WriteLine("\n-------------------- :" + folderName + "\n" + forstmt.ToString());
                    //                    }
                    //                }
                    //            }

                    //        }
                    //    }
                    //}
                }
            }

            public void IsParallelInvokeMethod(InvocationExpressionSyntax node)
            {
                string[] waitName = { "Invoke" };

                if (FindSpecificMethod(node, waitName, "System.Threading.Tasks"))
                {
                    objWriter = new System.IO.StreamWriter(@"C:\\Users\semih\Desktop\misusage\parallelInvoke.txt", true);
                    if (node.GetText().Contains("Parallel.Invoke"))
                    {
                        if (node.ArgumentList.Arguments.Count == 1)
                            objWriter.WriteLine("\n-------------------- :" + folderName + "\n" + node);
                    }
                    else
                    {
                        objWriter.WriteLine("Error");
                    }
                    objWriter.Close();
                }
            }

        }





    }
}

