﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Collections.Concurrent;

namespace Snap.Hutao.Core.Windowing.Backdrop;

// https://github.com/microsoft/microsoft-ui-xaml/blob/winui3/release/1.5-stable/controls/dev/Materials/DesktopAcrylicBackdrop/DesktopAcrylicBackdrop.cpp
internal sealed class InputActiveDesktopAcrylicBackdrop : SystemBackdrop
{
    private readonly ConcurrentDictionary<ICompositionSupportsSystemBackdrop, DesktopAcrylicController> controllers = [];

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(target, xamlRoot);

        DesktopAcrylicController newController = new();
        SystemBackdropConfiguration configuration = GetDefaultSystemBackdropConfiguration(target, xamlRoot);

        configuration.IsInputActive = true;

        newController.AddSystemBackdropTarget(target);
        newController.SetSystemBackdropConfiguration(configuration);
        controllers.TryAdd(target, newController);
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop target)
    {
        base.OnTargetDisconnected(target);

        if (controllers.TryRemove(target, out DesktopAcrylicController? controller))
        {
            controller.RemoveSystemBackdropTarget(target);
            controller.Dispose();
        }
    }
}