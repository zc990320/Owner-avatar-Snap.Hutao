﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Service.UIGF;

internal sealed class UIGFExportOptions
{
    public required string FilePath { get; set; }

    public required List<Guid> GachaArchiveIds { get; set; }

    public required List<Guid> ReservedAchievementArchiveIds { get; set; } 

    public required List<string> ReservedAvatarInfoUids { get; set; }

    public required List<Guid> ReservedCultivationProjectIds { get; set; }

    public required List<string> ReservedSpiralAbyssUids { get; set; }
}

internal sealed class UIGFImportOptions
{
    public Model.InterChange.UIGF UIGF { get; set; } = default!;

    public HashSet<string> GachaArchiveUids { get; set; } = default!;

    public HashSet<string> ReservedAchievementArchiveIdentities { get; set; } = default!;
}