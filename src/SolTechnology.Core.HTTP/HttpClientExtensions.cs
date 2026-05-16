﻿using SolTechnology.Core.HTTP;

// ReSharper disable once CheckNamespace
namespace System.Net.Http;

public static class HttpClientExtensions
{
    /// <summary>
    /// Begins constructing a request message for submission.
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="path"></param>
    /// <returns><see cref="RequestBuilder"/> to use in constructing additional request details.</returns>
    public static RequestBuilder CreateRequest(this HttpClient httpClient, string path)
    {
        return new RequestBuilder(httpClient, path);
    }

    /// <summary>
    /// Begins constructing a request message that carries <paramref name="policy"/>
    /// through <see cref="HttpRequestMessage.Options"/>. Downstream code
    /// (resilience pipeline, error formatter) reads the policy via
    /// <see cref="RequestBuilder.PolicyOptionsKey"/> instead of pulling it from DI.
    /// <para>
    /// Use this overload when a single request needs to override the client-wide
    /// policy (e.g. <c>IncludeResponseBodyInException = true</c> for a one-off
    /// debug call) — typed-client implementations should pull
    /// <c>IOptionsMonitor&lt;HttpPolicyConfiguration&gt;.Get(clientName)</c> and
    /// pass it here.
    /// </para>
    /// </summary>
    public static RequestBuilder CreateRequest(this HttpClient httpClient, string path, HttpPolicyConfiguration policy)
    {
        return new RequestBuilder(httpClient, path, policy);
    }
}

