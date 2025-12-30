using System;

namespace Core.Model;

/// <summary>
/// 类信息
/// </summary>
public class ClassInfoDto
{
    /// <summary>
    /// id（完全限定名）
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 注释
    /// </summary>
    public string Comments { get; set; }

    /// <summary>
    /// 类声明
    /// </summary>
    public string ClassDefinition { get; set; }

    /// <summary>
    /// 类源码
    /// </summary>
    public string SourceCode { get; set; }

    /// <summary>
    /// 类方法
    /// </summary>
    public List<MethodInfoDto> Methods { get; set; }
}
