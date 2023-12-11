﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Factory.QrCode;

internal interface IQrCodeFactory
{
    byte[] CreateByteArr(string source);
}
