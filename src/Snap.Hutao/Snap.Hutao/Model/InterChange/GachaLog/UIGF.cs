﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Model.InterChange.GachaLog;

/// <summary>
/// 统一可交换祈愿格式
/// https://www.snapgenshin.com/development/UIGF.html
/// </summary>
public class UIGF
{
    /// <summary>
    /// 当前发行的版本
    /// </summary>
    public const string CurrentVersion = "v2.2";

    private static readonly ImmutableList<string> SupportedVersion = new List<string>()
    {
        "v2.1", CurrentVersion,
    }.ToImmutableList();

    /// <summary>
    /// 信息
    /// </summary>
    [JsonPropertyName("info")]
    public UIGFInfo Info { get; set; } = default!;

    /// <summary>
    /// 列表
    /// </summary>
    [JsonPropertyName("list")]
    public List<UIGFItem> List { get; set; } = default!;

    /// <summary>
    /// 确认当前UIGF对象的版本是否受支持
    /// </summary>
    /// <returns>当前UIAF对象是否受支持</returns>
    public bool IsCurrentVersionSupported()
    {
        return SupportedVersion.Contains(Info?.UIGFVersion ?? string.Empty);
    }
}