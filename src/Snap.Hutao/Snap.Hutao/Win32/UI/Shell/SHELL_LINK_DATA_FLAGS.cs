﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Win32.UI.Shell;

[Flags]
internal enum SHELL_LINK_DATA_FLAGS
{
    SLDF_DEFAULT = 0,
    SLDF_HAS_ID_LIST = 1,
    SLDF_HAS_LINK_INFO = 2,
    SLDF_HAS_NAME = 4,
    SLDF_HAS_RELPATH = 8,
    SLDF_HAS_WORKINGDIR = 0x10,
    SLDF_HAS_ARGS = 0x20,
    SLDF_HAS_ICONLOCATION = 0x40,
    SLDF_UNICODE = 0x80,
    SLDF_FORCE_NO_LINKINFO = 0x100,
    SLDF_HAS_EXP_SZ = 0x200,
    SLDF_RUN_IN_SEPARATE = 0x400,
    SLDF_HAS_DARWINID = 0x1000,
    SLDF_RUNAS_USER = 0x2000,
    SLDF_HAS_EXP_ICON_SZ = 0x4000,
    SLDF_NO_PIDL_ALIAS = 0x8000,
    SLDF_FORCE_UNCNAME = 0x10000,
    SLDF_RUN_WITH_SHIMLAYER = 0x20000,
    SLDF_FORCE_NO_LINKTRACK = 0x40000,
    SLDF_ENABLE_TARGET_METADATA = 0x80000,
    SLDF_DISABLE_LINK_PATH_TRACKING = 0x100000,
    SLDF_DISABLE_KNOWNFOLDER_RELATIVE_TRACKING = 0x200000,
    SLDF_NO_KF_ALIAS = 0x400000,
    SLDF_ALLOW_LINK_TO_LINK = 0x800000,
    SLDF_UNALIAS_ON_SAVE = 0x1000000,
    SLDF_PREFER_ENVIRONMENT_PATH = 0x2000000,
    SLDF_KEEP_LOCAL_IDLIST_FOR_UNC_TARGET = 0x4000000,
    SLDF_PERSIST_VOLUME_ID_RELATIVE = 0x8000000,
    SLDF_VALID = 0xFFFF7FF,
    SLDF_RESERVED = unchecked((int)0x80000000),
}