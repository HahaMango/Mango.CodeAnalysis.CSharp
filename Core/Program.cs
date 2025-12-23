using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System.Text;

MSBuildWorkspace workspace = MSBuildWorkspace.Create();
// 注意：这里应该使用实际的解决方案路径
var solution = await workspace.OpenSolutionAsync("D:\\work\\TM.Scaffold\\Core6\\Core6.sln");

foreach(var project in solution.Projects)
{
    Console.WriteLine($"项目名称: {project.Name}");
    var compilation = await project.GetCompilationAsync();

    foreach (var tree in compilation.SyntaxTrees)
    {
        // 解析命名空间、类名和类注释
        var root = tree.GetRoot();
        
        // 查找所有类声明
        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(node => node is ClassDeclarationSyntax);
            
        foreach (var classDecl in classDeclarations)
        {
            var classSyntax = classDecl;
            
            // 获取命名空间
            var namespaceName = GetNamespace(classSyntax);
            
            // 获取类名
            var className = classSyntax.Identifier.Text;
            
            // 获取类注释
            var classComments = GetClassComments(classSyntax);
            
            Console.WriteLine($"命名空间: {namespaceName}");
            Console.WriteLine($"类名: {className}");
            Console.WriteLine($"类注释: {classComments}");
            Console.WriteLine("---");
        }
    }
}

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
