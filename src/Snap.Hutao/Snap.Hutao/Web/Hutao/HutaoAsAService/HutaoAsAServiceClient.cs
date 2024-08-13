﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Service;
using Snap.Hutao.Service.Hutao;
using Snap.Hutao.Web.Endpoint.Hutao;
using Snap.Hutao.Web.Hutao.Response;
using Snap.Hutao.Web.Request.Builder;
using Snap.Hutao.Web.Request.Builder.Abstraction;
using System.Net.Http;

namespace Snap.Hutao.Web.Hutao.HutaoAsAService;

/// <summary>
/// HaaS Client
/// </summary>
[ConstructorGenerated(ResolveHttpClient = true)]
[HttpClient(HttpClientConfiguration.Default)]
internal sealed partial class HutaoAsAServiceClient
{
    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    private readonly IHutaoEndpointsFactory hutaoEndpointsFactory;
    private readonly ILogger<HutaoAsAServiceClient> logger;
    private readonly HutaoUserOptions hutaoUserOptions;
    private readonly CultureOptions cultureOptions;
    private readonly HttpClient httpClient;

    public async ValueTask<HutaoResponse<List<Announcement>>> GetAnnouncementListAsync(List<long> excluedeIds, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().Announcement(cultureOptions.LocaleName))
            .PostJson(excluedeIds);

        HutaoResponse<List<Announcement>>? resp = await builder
            .SendAsync<HutaoResponse<List<Announcement>>>(httpClient, logger, token)
            .ConfigureAwait(false);

        return Web.Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<HutaoResponse> UploadAnnouncementAsync(UploadAnnouncement uploadAnnouncement, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().AnnouncementUpload())
            .PostJson(uploadAnnouncement);

        await builder.TrySetTokenAsync(hutaoUserOptions).ConfigureAwait(false);

        HutaoResponse? resp = await builder
            .SendAsync<HutaoResponse>(httpClient, logger, token)
            .ConfigureAwait(false);

        return Web.Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<HutaoResponse> GachaLogCompensationAsync(int days, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().GachaLogCompensation(days))
            .Get();

        await builder.TrySetTokenAsync(hutaoUserOptions).ConfigureAwait(false);

        HutaoResponse? resp = await builder
            .SendAsync<HutaoResponse>(httpClient, logger, token)
            .ConfigureAwait(false);

        return Web.Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<HutaoResponse> GachaLogDesignationAsync(string userName, int days, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().GachaLogDesignation(userName, days))
            .Get();

        await builder.TrySetTokenAsync(hutaoUserOptions).ConfigureAwait(false);

        HutaoResponse? resp = await builder
            .SendAsync<HutaoResponse>(httpClient, logger, token)
            .ConfigureAwait(false);

        return Web.Response.Response.DefaultIfNull(resp);
    }
}