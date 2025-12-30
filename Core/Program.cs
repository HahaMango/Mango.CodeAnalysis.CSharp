using Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System.Text;

MSBuildWorkspace workspace = MSBuildWorkspace.Create();
// 注意：这里应该使用实际的解决方案路径
var solution = await workspace.OpenSolutionAsync("E:\\czhworks\\TM.Scaffold\\Core6\\Core6.sln");

var classList = new List<ClassInfoDto>();

foreach(var project in solution.Projects)
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
            var comm = GetClassComments(classSyntax);

            var classDto = new ClassInfoDto
            {
                SourceCode = classSyntax.ToFullString(),
                Comments = comm,
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
                    Id = methodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                };
                classDto.Methods.Add(methodDto);
            }
            
            classList.Add(classDto);
            
            // Console.WriteLine($"命名空间: {namespaceName}");
            // Console.WriteLine($"类名: {className}");
            // Console.WriteLine($"类注释: {classComments}");
            // Console.WriteLine("---");
        }
    }
}

var i =1;

string GetNamespace(ClassDeclarationSyntax classSyntax)
{
    var namespaceDeclaration = classSyntax.Ancestors()
        .OfType<NamespaceDeclarationSyntax>()
        .FirstOrDefault();
        
    return namespaceDeclaration?.Name?.ToString() ?? "未找到命名空间";
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
