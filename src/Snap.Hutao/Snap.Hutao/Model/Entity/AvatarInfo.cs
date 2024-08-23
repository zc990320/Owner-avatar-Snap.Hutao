﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Abstraction;
using Snap.Hutao.Web.Hoyolab.Takumi.GameRecord.Avatar;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Hutao.Model.Entity;

[Table("avatar_infos")]
internal sealed class AvatarInfo : IMappingFrom<AvatarInfo, string, DetailedCharacter>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid InnerId { get; set; }

    public string Uid { get; set; } = default!;

    [Obsolete("Info is replaced by Info2")]
    public Web.Enka.Model.AvatarInfo Info { get; set; } = default!;

    public DetailedCharacter Info2 { get; set; } = default!;

    public DateTimeOffset ShowcaseRefreshTime { get; set; }

    public DateTimeOffset GameRecordRefreshTime { get; set; }

    public DateTimeOffset CalculatorRefreshTime { get; set; }

    public static AvatarInfo From(string uid, DetailedCharacter info)
    {
        return new() { Uid = uid, Info2 = info };
    }
}