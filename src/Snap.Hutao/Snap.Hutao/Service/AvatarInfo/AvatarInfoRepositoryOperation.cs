﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Database;
using Snap.Hutao.Core.DependencyInjection.Abstraction;
using Snap.Hutao.Model.Entity.Database;
using Snap.Hutao.Model.Metadata;
using Snap.Hutao.Model.Primitive;
using Snap.Hutao.Service.AvatarInfo.Transformer;
using Snap.Hutao.ViewModel.User;
using Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate;
using Snap.Hutao.Web.Hoyolab.Takumi.GameRecord;
using Snap.Hutao.Web.Hoyolab.Takumi.GameRecord.Avatar;
using Snap.Hutao.Web.Response;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CalculateAvatar = Snap.Hutao.Web.Hoyolab.Takumi.Event.Calculate.Avatar;
using EnkaAvatarInfo = Snap.Hutao.Web.Enka.Model.AvatarInfo;
using EntityAvatarInfo = Snap.Hutao.Model.Entity.AvatarInfo;
using RecordCharacter = Snap.Hutao.Web.Hoyolab.Takumi.GameRecord.Avatar.Character;
using RecordPlayerInfo = Snap.Hutao.Web.Hoyolab.Takumi.GameRecord.PlayerInfo;

namespace Snap.Hutao.Service.AvatarInfo;

[ConstructorGenerated]
[Injection(InjectAs.Singleton)]
internal sealed partial class AvatarInfoRepositoryOperation
{
    private readonly IAvatarInfoRepository avatarInfoRepository;
    private readonly IServiceProvider serviceProvider;
    private readonly ITaskContext taskContext;

    public void UnsafeUpdateDbAvatarInfos(string uid, IEnumerable<EnkaAvatarInfo> webInfos)
    {
        List<EntityAvatarInfo> dbInfos = avatarInfoRepository.GetAvatarInfoListByUid(uid);
        EnsureItemsAvatarIdUnique(ref dbInfos, uid, out Dictionary<AvatarId, EntityAvatarInfo> dbInfoMap);

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (EnkaAvatarInfo webInfo in webInfos)
            {
                if (AvatarIds.IsPlayer(webInfo.AvatarId))
                {
                    continue;
                }

                EntityAvatarInfo? entity = dbInfoMap.GetValueOrDefault(webInfo.AvatarId);
                AddOrUpdateAvatarInfo(entity, uid, appDbContext, webInfo);
            }
        }
    }

    public async ValueTask<List<EntityAvatarInfo>> UpdateDbAvatarInfosByShowcaseAsync(string uid, IEnumerable<EnkaAvatarInfo> webInfos, CancellationToken token)
    {
        await taskContext.SwitchToBackgroundAsync();
        List<EntityAvatarInfo> dbInfos = avatarInfoRepository.GetAvatarInfoListByUid(uid);
        EnsureItemsAvatarIdUnique(ref dbInfos, uid, out Dictionary<AvatarId, EntityAvatarInfo> dbInfoMap);

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (EnkaAvatarInfo webInfo in webInfos)
            {
                if (AvatarIds.IsPlayer(webInfo.AvatarId))
                {
                    continue;
                }

                EntityAvatarInfo? entity = dbInfoMap.GetValueOrDefault(webInfo.AvatarId);
                AddOrUpdateAvatarInfo(entity, uid, appDbContext, webInfo);
            }

            return avatarInfoRepository.GetAvatarInfoListByUid(uid);
        }
    }

    public async ValueTask<List<EntityAvatarInfo>> UpdateDbAvatarInfosByGameRecordCharacterAsync(UserAndUid userAndUid, CancellationToken token)
    {
        await taskContext.SwitchToBackgroundAsync();
        string uid = userAndUid.Uid.Value;
        List<EntityAvatarInfo> dbInfos = avatarInfoRepository.GetAvatarInfoListByUid(uid);
        EnsureItemsAvatarIdUnique(ref dbInfos, uid, out Dictionary<AvatarId, EntityAvatarInfo> dbInfoMap);

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            IGameRecordClient gameRecordClient = serviceProvider
                .GetRequiredService<IOverseaSupportFactory<IGameRecordClient>>()
                .CreateFor(userAndUid);
            Response<RecordPlayerInfo> playerInfoResponse = await gameRecordClient
                .GetPlayerInfoAsync(userAndUid, token)
                .ConfigureAwait(false);

            if (!playerInfoResponse.IsOk())
            {
                goto Return;
            }

            Response<ListWrapper<Character>> charactersResponse = await gameRecordClient
                .GetCharacterListAsync(userAndUid, playerInfoResponse.Data, token)
                .ConfigureAwait(false);

            if (!charactersResponse.IsOk())
            {
                goto Return;
            }

            List<RecordCharacter> characters = charactersResponse.Data.List;

            AvatarInfoDetailedCharacterTransformer transformer = serviceProvider
                .GetRequiredService<AvatarInfoDetailedCharacterTransformer>();

            foreach (RecordCharacter character in characters)
            {
                if (AvatarIds.IsPlayer(character.Id))
                {
                    continue;
                }

                EntityAvatarInfo? entity = dbInfoMap.GetValueOrDefault(character.Id);
                AddOrUpdateAvatarInfo(entity, character.Id, uid, appDbContext, transformer, character);
            }
        }

    Return:
        return avatarInfoRepository.GetAvatarInfoListByUid(uid);
    }

    public async ValueTask<List<EntityAvatarInfo>> UpdateDbAvatarInfosByCalculateAvatarDetailAsync(UserAndUid userAndUid, CancellationToken token)
    {
        await taskContext.SwitchToBackgroundAsync();
        string uid = userAndUid.Uid.Value;
        List<EntityAvatarInfo> dbInfos = avatarInfoRepository.GetAvatarInfoListByUid(uid);
        EnsureItemsAvatarIdUnique(ref dbInfos, uid, out Dictionary<AvatarId, EntityAvatarInfo> dbInfoMap);

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            CalculateClient calculateClient = scope.ServiceProvider.GetRequiredService<CalculateClient>();
            List<CalculateAvatar> avatars = await calculateClient
                .GetAvatarsAsync(userAndUid, token)
                .ConfigureAwait(false);

            CalculateAvatarDetailAvatarInfoTransformer transformer = scope.ServiceProvider
                .GetRequiredService<CalculateAvatarDetailAvatarInfoTransformer>();

            foreach (CalculateAvatar avatar in avatars)
            {
                if (AvatarIds.IsPlayer(avatar.Id))
                {
                    continue;
                }

                Response<AvatarDetail> detailAvatarResponse = await calculateClient
                    .GetAvatarDetailAsync(userAndUid, avatar, token)
                    .ConfigureAwait(false);

                if (!detailAvatarResponse.IsOk())
                {
                    continue;
                }

                EntityAvatarInfo? entity = dbInfoMap.GetValueOrDefault(avatar.Id);
                AddOrUpdateAvatarInfo(entity, avatar.Id, uid, appDbContext, transformer, detailAvatarResponse.Data);
            }
        }

        return avatarInfoRepository.GetAvatarInfoListByUid(uid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddOrUpdateAvatarInfo(EntityAvatarInfo? entity, string uid, AppDbContext appDbContext, EnkaAvatarInfo webInfo)
    {
        if (entity is null)
        {
            entity = EntityAvatarInfo.From(uid, webInfo);
        }
        else
        {
            entity.Info = webInfo;
        }

        entity.ShowcaseRefreshTime = DateTimeOffset.UtcNow;
        appDbContext.AvatarInfos.UpdateAndSave(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddOrUpdateAvatarInfo(EntityAvatarInfo? entity, in AvatarId avatarId, string uid, AppDbContext appDbContext, CalculateAvatarDetailAvatarInfoTransformer transformer, AvatarDetail source)
    {
        if (entity is null)
        {
            EnkaAvatarInfo avatarInfo = new() { AvatarId = avatarId };
            transformer.Transform(ref avatarInfo, source);
            entity = EntityAvatarInfo.From(uid, avatarInfo);
        }
        else
        {
            EnkaAvatarInfo avatarInfo = entity.Info;
            transformer.Transform(ref avatarInfo, source);
            entity.Info = avatarInfo;
        }

        entity.CalculatorRefreshTime = DateTimeOffset.UtcNow;
        appDbContext.AvatarInfos.UpdateAndSave(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddOrUpdateAvatarInfo(EntityAvatarInfo? entity, in AvatarId avatarId, string uid, AppDbContext appDbContext, AvatarInfoDetailedCharacterTransformer transformer, Character source)
    {
        if (entity is null)
        {
            EnkaAvatarInfo avatarInfo = new() { AvatarId = avatarId };
            transformer.Transform(ref avatarInfo, source);
            entity = EntityAvatarInfo.From(uid, avatarInfo);
        }
        else
        {
            EnkaAvatarInfo avatarInfo = entity.Info;
            transformer.Transform(ref avatarInfo, source);
            entity.Info = avatarInfo;
        }

        entity.GameRecordRefreshTime = DateTimeOffset.UtcNow;
        appDbContext.AvatarInfos.UpdateAndSave(entity);
    }

    private void EnsureItemsAvatarIdUnique(ref List<EntityAvatarInfo> dbInfos, string uid, out Dictionary<AvatarId, EntityAvatarInfo> dbInfoMap)
    {
        dbInfoMap = [];
        foreach (ref readonly EntityAvatarInfo info in CollectionsMarshal.AsSpan(dbInfos))
        {
            if (!dbInfoMap.TryAdd(info.Info2.Base.Id, info))
            {
                avatarInfoRepository.RemoveAvatarInfoRangeByUid(uid);
                dbInfoMap.Clear();
                dbInfos.Clear();
            }
        }
    }
}