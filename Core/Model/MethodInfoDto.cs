using System;

namespace Core.Model;

public class MethodInfoDto
{
    /// <summary>
    /// 完全限定名
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 注释
    /// </summary>
    public string Comments { get; set; }

    /// <summary>
    /// 类声明
    /// </summary>
    public string MethodDefinition { get; set; }

    /// <summary>
    /// 方法源码
    /// </summary>
    public string SourceCode { get; set; }

    /// <summary>
    /// 调用方法列表
    /// </summary>
    public List<InvocationMethodInfoDto> InvocationList { get; set; }

    /// <summary>
    /// 引用列表
    /// </summary>
    public List<ReferenceMethodInfoDto> ReferenceList { get; set; }
}

public class InvocationMethodInfoDto
{
    /// <summary>
    /// 完全限定名
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 注释
    /// </summary>
    public string Comments { get; set; }

    /// <summary>
    /// 类声明
    /// </summary>
    public string MethodDefinition { get; set; }

    /// <summary>
    /// 方法源码
    /// </summary>
    public string SourceCode { get; set; }

    /// <summary>
    /// 调用方法列表
    /// </summary>
    public List<InvocationMethodInfoDto> InvocationList { get; set; }
}

public class ReferenceMethodInfoDto
{
    /// <summary>
    /// 完全限定名
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 注释
    /// </summary>
    public string Comments { get; set; }

    /// <summary>
    /// 类声明
    /// </summary>
    public string MethodDefinition { get; set; }

    /// <summary>
    /// 方法源码
    /// </summary>
    public string SourceCode { get; set; }

    /// <summary>
    /// 引用列表
    /// </summary>
    public List<ReferenceMethodInfoDto> ReferenceList { get; set; }
}