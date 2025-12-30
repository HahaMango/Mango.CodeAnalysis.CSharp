using System;

namespace Core.Model;

public class MethodInfoDto
{
    /// <summary>
    /// 完全限定名
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 方法源码
    /// </summary>
    public string SourceCode { get; set; }

    /// <summary>
    /// 调用方法列表
    /// </summary>
    public List<MethodInfoDto> InvocationList { get; set; }

    /// <summary>
    /// 引用列表
    /// </summary>
    public List<MethodInfoDto> ReferenceList { get; set; }
}
