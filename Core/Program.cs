using Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using System.Text;
using System.Text.Json;

MSBuildWorkspace workspace = MSBuildWorkspace.Create();
// 注意：这里应该使用实际的解决方案路径
var solution = await workspace.OpenSolutionAsync("D:\\work\\TM.Scaffold\\WebApiScaffold\\TM.Scaffold.Project.sln");

var classInfoDtos = await BuildClassInfoAsync(solution);
var d = await BuildInvokeMapAsync(solution, classInfoDtos);

var x = d;

string json = JsonSerializer.Serialize(x, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping});
await File.WriteAllTextAsync("test.json", json, Encoding.UTF8);

//构建项目类结构
async Task<List<ClassInfoDto>> BuildClassInfoAsync(Solution solution)
{
    var classList = new List<ClassInfoDto>();

    foreach (var project in solution.Projects)
    {
        Console.WriteLine($"项目名称: {project.Name}");
        var compilation = await project.GetCompilationAsync();

        foreach (var tree in compilation.SyntaxTrees)
        {
            // 假设你已经有一个SyntaxTree和SemanticModel
            var semanticModel = compilation.GetSemanticModel(tree);
            // 解析命名空间、类名和类注释
            var root = tree.GetRoot();

            // 查找所有类声明
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(node => node is ClassDeclarationSyntax);

            foreach (var classDecl in classDeclarations)
            {
                var classSyntax = classDecl;
                // 获取类的Symbol
                var classSymbol = semanticModel.GetDeclaredSymbol(classSyntax);

                var classDto = new ClassInfoDto
                {
                    SourceCode = classSyntax.ToFullString(),
                    Comments = StrCleaning(GetClassComments(classSyntax)),
                    ClassDefinition = FormatClassDeclarationComplete(classSyntax),
                    Id = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Methods = new List<MethodInfoDto>()
                };

                // 获取类中的所有方法
                var methodDeclarations = classSyntax.DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (var methodDecl in methodDeclarations)
                {
                    var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl);
                    var methodDto = new MethodInfoDto
                    {
                        SourceCode = methodDecl.ToFullString(),
                        Id = methodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                            .WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters)
                            .WithParameterOptions(SymbolDisplayParameterOptions.IncludeType)
                        ),
                        Comments = StrCleaning(GetMethodComments(methodDecl)),
                        MethodDefinition = FormatMethodDeclaration(methodDecl),
                        InvocationList = [],
                        ReferenceList = []
                    };
                    classDto.Methods.Add(methodDto);
                }

                classList.Add(classDto);
            }
        }
    }
    return classList;
}

async Task<List<ClassInfoDto>> BuildInvokeMapAsync(Solution solution, List<ClassInfoDto> classInfos)
{
    //解析方法调用关系
    foreach (var project in solution.Projects)
    {
        var compilation = await project.GetCompilationAsync();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(tree);

            var root = tree.GetRoot();
            // 查找所有类声明
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(node => node is ClassDeclarationSyntax);

            foreach (var classDecl in classDeclarations)
            {
                // 获取类中的所有方法
                var methodDeclarations = classDecl.DescendantNodes().OfType<MethodDeclarationSyntax>();

                var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
                //根据完全限定名，查找当前类dto
                var classDto = classInfos.FirstOrDefault(x => x.Id == classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                if (classDto == null)
                {
                    continue;
                }

                foreach (var methodDecl in methodDeclarations)
                {
                    //获取方法体内的调用的其他方法
                    var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl);

                    //根据完全限定名，查找当前方法dto
                    var methodDto = classDto?.Methods.FirstOrDefault(x => x.Id == methodSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters)
                            .WithParameterOptions(SymbolDisplayParameterOptions.IncludeType)));
                    if (methodDto == null || methodDto.Id.Contains("Dispose"))
                    {
                        continue;
                    }

                    //查找引用方法列表
                    methodDto.ReferenceList = await GetReferencesMethodAsync(classInfos, methodSymbol, new HashSet<string>());

                    //查询调用方法列表
                    var invocationExpressions = methodDecl.DescendantNodes()
                        .OfType<InvocationExpressionSyntax>()
                        .ToList();

                    foreach (var invocation in invocationExpressions)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(invocation.Expression);
                        if (symbolInfo.Symbol != null)
                        {
                            var invokedMethodSymbol = (symbolInfo.Symbol as IMethodSymbol)?.ReducedFrom
                                            ?? symbolInfo.Symbol;
                            // 查找被调用方法的完全限定名
                            //var invokedMethodSymbol = symbolInfo.Symbol;
                            var fullyQualifiedName = invokedMethodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                                .WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters)
                                .WithParameterOptions(SymbolDisplayParameterOptions.IncludeType)
                            );

                            if (methodDto.Id != fullyQualifiedName)
                            {
                                //调用方法名不等于当前方法名，防止循环引用
                                var invokedMethod = GetMethodById(classInfos, fullyQualifiedName);
                                if (invokedMethod != null)
                                {
                                    methodDto.InvocationList.Add(new InvocationMethodInfoDto
                                    {
                                        Id = invokedMethod.Id,
                                        Comments = invokedMethod.Comments,
                                        MethodDefinition = invokedMethod.MethodDefinition,
                                        SourceCode = invokedMethod.SourceCode,
                                        InvocationList = invokedMethod.InvocationList
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    return classInfos;
}

MethodInfoDto GetMethodById(List<ClassInfoDto> classInfoDtos, string id)
{
    foreach (var classInfo in classInfoDtos)
    {
        foreach (var methodInfo in classInfo.Methods)
        {
            if (methodInfo.Id == id)
            {
                return methodInfo;
            }
        }
    }
    return null;
}

async Task<List<ReferenceMethodInfoDto>> GetReferencesMethodAsync(List<ClassInfoDto> classInfos, IMethodSymbol methodSymbol, HashSet<string> visited = null)
{
    visited ??= new HashSet<string>();
    
    // 防止无限递归：如果方法已经访问过，直接返回空列表
    if (visited.Contains(methodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
    {
        return new List<ReferenceMethodInfoDto>();
    }
    
    // 添加当前方法到已访问集合
    visited.Add(methodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    
    System.Console.WriteLine("执行递归查询引用方法");
    //查找方法引用
    var references = await SymbolFinder.FindReferencesAsync(methodSymbol, solution);
    var result = new List<ReferenceMethodInfoDto>();
    foreach (var item in references)
    {
        foreach (var location in item.Locations)
        {
            var document = location.Document;
            var syntaxRoot = await document.GetSyntaxRootAsync();
            var referencesSemanticModel = await document.GetSemanticModelAsync();

            // 获取引用节点（通常是 InvocationExpressionSyntax 或 IdentifierNameSyntax）
            var node = syntaxRoot.FindNode(location.Location.SourceSpan);
            var referencesExpressions = node.Ancestors().OfType<InvocationExpressionSyntax>().ToList();
            foreach (var exp in referencesExpressions)
            {
                var containingMethod = exp.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                if (containingMethod != null)
                {
                    var referencesMethodSymbol = referencesSemanticModel?.GetDeclaredSymbol(containingMethod);
                    if (referencesMethodSymbol != null)
                    {
                        var referencesMethodName = referencesMethodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                            .WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters)
                            .WithParameterOptions(SymbolDisplayParameterOptions.IncludeType)
                        );

                        var referencesMethod = GetMethodById(classInfos, referencesMethodName);
                        if (referencesMethod != null)
                        {
                            result.Add(new ReferenceMethodInfoDto
                            {
                                Id = referencesMethod.Id,
                                Comments = referencesMethod.Comments,
                                MethodDefinition = referencesMethod.MethodDefinition,
                                SourceCode = referencesMethod.SourceCode,
                                ReferenceList = await GetReferencesMethodAsync(classInfos, referencesMethodSymbol, visited)
                            });
                        }
                    }
                }
            }
        }
    }
    return result;
}

string GetClassComments(ClassDeclarationSyntax classSyntax)
{
    var comments = new StringBuilder();

    // 获取类声明节点的前导注释
    var leadingTrivia = classSyntax.GetLeadingTrivia();
    foreach (var trivia in leadingTrivia)
    {
        if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
            trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
            trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
            trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
        {
            comments.AppendLine(trivia.ToFullString().Trim());
        }
    }

    return comments.ToString().Trim() ?? "无注释";
}

string FormatClassDeclarationComplete(ClassDeclarationSyntax classDecl)
{
    var parts = new List<string>();

    // 修饰符
    var modifiers = classDecl.Modifiers.Select(m => m.Text);
    parts.AddRange(modifiers);

    // class 关键字
    parts.Add("class");

    // 类名
    parts.Add(classDecl.Identifier.Text);

    // 基类
    if (classDecl.BaseList != null && classDecl.BaseList.Types.Count > 0)
    {
        var baseTypes = classDecl.BaseList.Types.Select(t => t.Type.ToString());
        parts.Add(": " + string.Join(", ", baseTypes));
    }

    return string.Join(" ", parts.Where(p => !string.IsNullOrEmpty(p)));
}

string GetMethodComments(MethodDeclarationSyntax methodDecl)
{
    var comments = new StringBuilder();

    // 获取方法声明节点的前导注释
    var leadingTrivia = methodDecl.GetLeadingTrivia();
    foreach (var trivia in leadingTrivia)
    {
        if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
            trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
            trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
            trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
        {
            comments.AppendLine(trivia.ToFullString().Trim());
        }
    }

    return comments.ToString().Trim() ?? "无注释";
}

string FormatMethodDeclaration(MethodDeclarationSyntax methodDecl)
{
    var parts = new List<string>();

    // 修饰符
    var modifiers = methodDecl.Modifiers.Select(m => m.Text);
    parts.AddRange(modifiers);

    // 返回类型
    if (methodDecl.ReturnType != null)
    {
        parts.Add(methodDecl.ReturnType.ToString());
    }

    // 方法名
    parts.Add(methodDecl.Identifier.Text);

    // 泛型参数（如果存在）
    if (methodDecl.TypeParameterList != null)
    {
        parts.Add(methodDecl.TypeParameterList.ToString());
    }

    // 参数列表
    if (methodDecl.ParameterList != null)
    {
        parts.Add(methodDecl.ParameterList.ToString());
    }

    return string.Join(" ", parts.Where(p => !string.IsNullOrEmpty(p)));
}

string StrCleaning(string comment)
{
    if (string.IsNullOrWhiteSpace(comment))
        return string.Empty;

    // 移除注释前的符号（如 ///, //, /*, */ 等）
    var result = System.Text.RegularExpressions.Regex.Replace(comment, @"^[\s\*/]+|[\s\*/]+$", "", System.Text.RegularExpressions.RegexOptions.Multiline);
    
    // 移除 XML 标签
    result = System.Text.RegularExpressions.Regex.Replace(result, @"<[^>]*>", string.Empty);
    
    // 移除多余的空白字符
    result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ").Trim();
    
    return result;
}
