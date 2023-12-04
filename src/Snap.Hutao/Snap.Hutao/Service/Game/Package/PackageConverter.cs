﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core;
using Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Core.ExceptionService;
using Snap.Hutao.Core.IO;
using Snap.Hutao.Core.IO.Hashing;
using Snap.Hutao.Core.IO.Http.Sharding;
using Snap.Hutao.Service.Game.Scheme;
using Snap.Hutao.Web.Hoyolab.SdkStatic.Hk4e.Launcher;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using static Snap.Hutao.Service.Game.GameConstants;

namespace Snap.Hutao.Service.Game.Package;

/// <summary>
/// 游戏文件包转换器
/// </summary>
[HighQuality]
[ConstructorGenerated(ResolveHttpClient = true)]
[HttpClient(HttpClientConfiguration.Default)]
internal sealed partial class PackageConverter
{
    private const string PackageVersion = "pkg_version";
    private readonly JsonSerializerOptions options;
    private readonly RuntimeOptions runtimeOptions;
    private readonly HttpClient httpClient;
    private readonly ILogger<PackageConverter> logger;

    public async ValueTask<bool> EnsureGameResourceAsync(LaunchScheme targetScheme, GameResource gameResource, string gameFolder, IProgress<PackageReplaceStatus> progress)
    {
        // 以 国服 => 国际 为例
        // 1. 下载国际服的 pkg_version 文件，转换为索引字典
        //    获取本地对应 pkg_version 文件，转换为索引字典
        //
        // 2. 对比两者差异，
        //    国际服有 & 国服没有的 为新增
        //    国际服有 & 国服也有的 为替换
        //    剩余国际服没有 & 国服有的 为备份
        //    生成对应的操作信息项，对比文件的尺寸与MD5
        //
        // 3. 根据操作信息项，提取其中需要下载的项进行缓存对比或下载
        //    若缓存中文件的尺寸与MD5与操作信息项中的一致则直接跳过
        //    每个文件下载后需要验证文件文件的尺寸与MD5
        //    若出现下载失败的情况，终止转换进程，此时国服文件尚未替换
        //
        // 4. 全部资源下载完成后，根据操作信息项，进行文件替换
        //    处理顺序：备份/替换/新增
        //    替换操作等于 先备份国服文件，随后新增国际服文件

        // 准备下载链接
        string scatteredFilesUrl = gameResource.Game.Latest.DecompressedPath;
        string pkgVersionUrl = $"{scatteredFilesUrl}/{PackageVersion}";

        PackageConvertContext context = new(targetScheme.IsOversea, runtimeOptions.DataFolder, gameFolder, scatteredFilesUrl);

        // Step 1
        progress.Report(new(SH.ServiceGamePackageRequestPackageVerion));
        Dictionary<string, VersionItem> remoteItems = await GetRemoteItemsAsync(pkgVersionUrl).ConfigureAwait(false);
        Dictionary<string, VersionItem> localItems = await GetLocalItemsAsync(gameFolder).ConfigureAwait(false);

        // Step 2
        List<ItemOperationInfo> diffOperations = GetItemOperationInfos(remoteItems, localItems).ToList();
        diffOperations.SortBy(i => i.Type);

        // Step 3
        await PrepareCacheFilesAsync(diffOperations, context, progress).ConfigureAwait(false);

        // Step 4
        return await ReplaceGameResourceAsync(diffOperations, context, progress).ConfigureAwait(false);
    }

    public async ValueTask EnsureDeprecatedFilesAndSdkAsync(GameResource resource, string gameFolder)
    {
        string sdkDllBackup = Path.Combine(gameFolder, YuanShenData, "Plugins\\PCGameSDK.dll.backup");
        string sdkDll = Path.Combine(gameFolder, YuanShenData, "Plugins\\PCGameSDK.dll");

        string sdkVersionBackup = Path.Combine(gameFolder, "sdk_pkg_version.backup");
        string sdkVersion = Path.Combine(gameFolder, "sdk_pkg_version");

        // Only bilibili's sdk is not null
        if (resource.Sdk is not null)
        {
            // TODO: verify sdk md5
            if (File.Exists(sdkDllBackup) && File.Exists(sdkVersionBackup))
            {
                FileOperation.Move(sdkDllBackup, sdkDll, false);
                FileOperation.Move(sdkVersionBackup, sdkVersion, false);
            }
            else
            {
                using (Stream sdkWebStream = await httpClient.GetStreamAsync(resource.Sdk.Path).ConfigureAwait(false))
                {
                    using (ZipArchive zip = new(sdkWebStream))
                    {
                        foreach (ZipArchiveEntry entry in zip.Entries)
                        {
                            // skip folder entry.
                            if (entry.Length != 0)
                            {
                                string targetPath = Path.Combine(gameFolder, entry.FullName);
                                string? directory = Path.GetDirectoryName(targetPath);
                                ArgumentException.ThrowIfNullOrEmpty(directory);
                                Directory.CreateDirectory(directory);
                                entry.ExtractToFile(targetPath, true);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            // backup
            FileOperation.Move(sdkDll, sdkDllBackup, true);
            FileOperation.Move(sdkVersion, sdkVersionBackup, true);
        }

        if (resource.DeprecatedFiles is not null)
        {
            foreach (NameMd5 file in resource.DeprecatedFiles)
            {
                string filePath = Path.Combine(gameFolder, file.Name);
                FileOperation.Move(filePath, $"{filePath}.backup", true);
            }
        }
    }

    private static IEnumerable<ItemOperationInfo> GetItemOperationInfos(Dictionary<string, VersionItem> remote, Dictionary<string, VersionItem> local)
    {
        foreach ((string remoteName, VersionItem remoteItem) in remote)
        {
            if (local.TryGetValue(remoteName, out VersionItem? localItem))
            {
                if (!remoteItem.Md5.Equals(localItem.Md5, StringComparison.OrdinalIgnoreCase))
                {
                    // 本地发现了同名且不同 MD5 的项，需要替换为服务器上的项
                    yield return new(ItemOperationType.Replace, remoteItem, localItem);
                }

                // 同名同MD5，跳过
                local.Remove(remoteName);
            }
            else
            {
                // 本地没有发现同名项
                yield return new(ItemOperationType.Add, remoteItem, remoteItem);
            }
        }

        foreach ((_, VersionItem localItem) in local)
        {
            yield return new(ItemOperationType.Backup, localItem, localItem);
        }
    }

    [GeneratedRegex("^(?:YuanShen_Data|GenshinImpact_Data)(?=/)")]
    private static partial Regex DataFolderRegex();

    private async ValueTask<Dictionary<string, VersionItem>> GetVersionItemsAsync(Stream stream)
    {
        Dictionary<string, VersionItem> results = [];
        using (StreamReader reader = new(stream))
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { Length: > 0 } row)
            {
                VersionItem? item = JsonSerializer.Deserialize<VersionItem>(row, options);
                ArgumentNullException.ThrowIfNull(item);
                item.RelativePath = DataFolderRegex().Replace(item.RelativePath, "{0}");
                results.Add(item.RelativePath, item);
            }
        }

        return results;
    }

    private async ValueTask<Dictionary<string, VersionItem>> GetRemoteItemsAsync(string pkgVersionUrl)
    {
        try
        {
            using (Stream remoteSteam = await httpClient.GetStreamAsync(pkgVersionUrl).ConfigureAwait(false))
            {
                return await GetVersionItemsAsync(remoteSteam).ConfigureAwait(false);
            }
        }
        catch (IOException ex)
        {
            throw ThrowHelper.PackageConvert(SH.ServiceGamePackageRequestPackageVerionFailed, ex);
        }
    }

    private async ValueTask<Dictionary<string, VersionItem>> GetLocalItemsAsync(string gameFolder)
    {
        using (FileStream localSteam = File.OpenRead(Path.Combine(gameFolder, PackageVersion)))
        {
            return await GetVersionItemsAsync(localSteam).ConfigureAwait(false);
        }
    }

    private async ValueTask PrepareCacheFilesAsync(List<ItemOperationInfo> operations, PackageConvertContext context, IProgress<PackageReplaceStatus> progress)
    {
        foreach (ItemOperationInfo info in operations)
        {
            switch (info.Type)
            {
                case ItemOperationType.Backup:
                    continue;
                case ItemOperationType.Replace:
                case ItemOperationType.Add:
                    await SkipOrDownloadAsync(info, context, progress).ConfigureAwait(false);
                    break;
            }
        }
    }

    private async ValueTask SkipOrDownloadAsync(ItemOperationInfo info, PackageConvertContext context, IProgress<PackageReplaceStatus> progress)
    {
        // 还原正确的远程地址
        string remoteName = string.Format(CultureInfo.CurrentCulture, info.Remote.RelativePath, context.ToDataFolderName);
        string cacheFile = context.GetServerCacheTargetFilePath(remoteName);

        if (File.Exists(cacheFile))
        {
            if (info.Remote.FileSize == new FileInfo(cacheFile).Length)
            {
                if (info.Remote.Md5.Equals(await MD5.HashFileAsync(cacheFile).ConfigureAwait(false), StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            // Invalid file, delete it
            File.Delete(cacheFile);
        }

        // Cache no matching item, download
        string? directory = Path.GetDirectoryName(cacheFile);
        ArgumentException.ThrowIfNullOrEmpty(directory);
        Directory.CreateDirectory(directory);

        string remoteUrl = context.GetScatteredFilesUrl(remoteName);
        HttpShardCopyWorkerOptions<PackageReplaceStatus> options = new()
        {
            HttpClient = httpClient,
            SourceUrl = remoteUrl,
            DestinationFilePath = cacheFile,
            StatusFactory = (bytesRead, totalBytes) => new(remoteName, bytesRead, totalBytes),
        };

        using (HttpShardCopyWorker<PackageReplaceStatus> worker = await HttpShardCopyWorker<PackageReplaceStatus>.CreateAsync(options).ConfigureAwait(false))
        {
            try
            {
                await worker.CopyAsync(progress).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // System.IO.IOException: The response ended prematurely.
                // System.IO.IOException: Received an unexpected EOF or 0 bytes from the transport stream.
                ThrowHelper.PackageConvert(SH.FormatServiceGamePackageRequestScatteredFileFailed(remoteName), ex);
            }
        }

        if (!string.Equals(info.Remote.Md5, await MD5.HashFileAsync(cacheFile).ConfigureAwait(false), StringComparison.OrdinalIgnoreCase))
        {
            ThrowHelper.PackageConvert(SH.FormatServiceGamePackageRequestScatteredFileFailed(remoteName));
        }
    }

    private async ValueTask<bool> ReplaceGameResourceAsync(List<ItemOperationInfo> operations, PackageConvertContext context, IProgress<PackageReplaceStatus> progress)
    {
        // 执行下载与移动操作
        foreach (ItemOperationInfo info in operations)
        {
            (bool moveToBackup, bool moveToTarget) = info.Type switch
            {
                ItemOperationType.Backup => (true, false),
                ItemOperationType.Replace => (true, true),
                ItemOperationType.Add => (false, true),
                _ => (false, false),
            };

            // 先备份
            if (moveToBackup)
            {
                string localFileName = string.Format(CultureInfo.CurrentCulture, info.Local.RelativePath, context.FromDataFolderName);
                progress.Report(new(SH.FormatServiceGamePackageConvertMoveFileBackupFormat(localFileName)));

                string localFilePath = context.GetGameFolderFilePath(localFileName);
                string cacheFilePath = context.GetServerCacheBackupFilePath(localFileName);
                string? cacheFileDirectory = Path.GetDirectoryName(cacheFilePath);
                ArgumentException.ThrowIfNullOrEmpty(cacheFileDirectory);
                Directory.CreateDirectory(cacheFileDirectory);

                logger.LogInformation("Backing file from:{Src} to:{Dst}", localFilePath, cacheFilePath);
                FileOperation.Move(localFilePath, cacheFilePath, true);
            }

            // 后替换
            if (moveToTarget)
            {
                string targetFileName = string.Format(CultureInfo.CurrentCulture, info.Remote.RelativePath, context.ToDataFolderName);
                progress.Report(new(SH.FormatServiceGamePackageConvertMoveFileRestoreFormat(targetFileName)));

                string targetFilePath = context.GetGameFolderFilePath(targetFileName);
                string? targetFileDirectory = Path.GetDirectoryName(targetFilePath);
                string cacheFilePath = context.GetServerCacheTargetFilePath(targetFileName);
                ArgumentException.ThrowIfNullOrEmpty(targetFileDirectory);
                Directory.CreateDirectory(targetFileDirectory);

                logger.LogInformation("Restoring file from:{Src} to:{Dst}", cacheFilePath, targetFilePath);
                FileOperation.Move(cacheFilePath, targetFilePath, true);
            }
        }

        // 重命名 _Data 目录
        try
        {
            progress.Report(new(SH.FormatServiceGamePackageConvertMoveFileRenameFormat(context.FromDataFolderName, context.ToDataFolderName)));
            DirectoryOperation.Move(context.FromDataFolder, context.ToDataFolder);
        }
        catch (IOException ex)
        {
            // Access to the path is denied.
            // When user install the game in special folder like 'Program Files'
            throw ThrowHelper.GameFileOperation(SH.ServiceGamePackageRenameDataFolderFailed, ex);
        }

        // 重新下载所有 *pkg_version 文件
        await ReplacePackageVersionFilesAsync(context).ConfigureAwait(false);
        return true;
    }

    private async ValueTask ReplacePackageVersionFilesAsync(PackageConvertContext context)
    {
        foreach (string versionFilePath in Directory.EnumerateFiles(context.GameFolder, "*pkg_version"))
        {
            string versionFileName = Path.GetFileName(versionFilePath);

            if (versionFileName == "sdk_pkg_version")
            {
                // Skipping the sdk_pkg_version file,
                // it can't be claimed from remote.
                continue;
            }

            using (FileStream versionFileStream = File.Create(versionFilePath))
            {
                using (Stream webStream = await httpClient.GetStreamAsync(context.GetScatteredFilesUrl(versionFileName)).ConfigureAwait(false))
                {
                    await webStream.CopyToAsync(versionFileStream).ConfigureAwait(false);
                }
            }
        }
    }
}