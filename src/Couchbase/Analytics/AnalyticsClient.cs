using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Core;
using Couchbase.Core.DataMapping;
using Couchbase.Core.IO.HTTP;
using Couchbase.Core.IO.Serializers;
using Couchbase.Query;
using Couchbase.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Couchbase.Analytics
{
    internal class AnalyticsClient : HttpServiceBase, IAnalyticsClient
    {
        //private static readonly ILog Log = LogManager.GetLogger(typeof(AnalyticsClient));
        internal const string AnalyticsPriorityHeaderName = "Analytics-Priority";

        public AnalyticsClient(ClusterContext context) : this(
            new HttpClient(new AuthenticatingHttpClientHandler(context.ClusterOptions.UserName, context.ClusterOptions.Password)),
            new JsonDataMapper(new DefaultSerializer()), context)
        {
        }

        public AnalyticsClient(HttpClient client, IDataMapper dataMapper, ClusterContext context)
            : base(client, dataMapper, context)
        { }

        /// <summary>
        /// Queries the specified request.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public IAnalyticsResult<T> Query<T>(IAnalyticsRequest request)
        {
            return QueryAsync<T>(request, CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Queries the asynchronous.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryRequest">The query request.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        public async Task<IAnalyticsResult<T>> QueryAsync<T>(IAnalyticsRequest queryRequest, CancellationToken token)
        {
            // try get Analytics node
            var node = Context.GetRandomNodeForService(ServiceType.Analytics);
            var result = new AnalyticsResult<T>();

            string body;
            //using (ClientConfiguration.Tracer.BuildSpan(queryRequest, CouchbaseOperationNames.RequestEncoding).StartActive())
            //{
                body = queryRequest.GetFormValuesAsJson();
            //}

            using (var content = new StringContent(body, System.Text.Encoding.UTF8, MediaType.Json))
            {
                try
                {
                    //Log.Trace("Sending analytics query cid{0}: {1}", queryRequest.CurrentContextId, baseUri);

                    HttpResponseMessage response;
                    //using (ClientConfiguration.Tracer.BuildSpan(queryRequest, CouchbaseOperationNames.DispatchToServer).StartActive())
                    //{
                        var request = new HttpRequestMessage(HttpMethod.Post, node.AnalyticsUri)
                        {
                            Content = content
                        };

                        if (queryRequest is AnalyticsRequest req && req.PriorityValue != 0)
                        {
                            request.Headers.Add(AnalyticsPriorityHeaderName, new[] {req.PriorityValue.ToString()});
                        }

                        response = await HttpClient.SendAsync(request, token).ConfigureAwait(false);
                    //}

                    //using (var scope = ClientConfiguration.Tracer.BuildSpan(queryRequest, CouchbaseOperationNames.ResponseDecoding).StartActive())
                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        result = (await DataMapper.MapAsync<AnalyticsResultData<T>>(stream, token).ConfigureAwait(false))
                            .ToQueryResult(HttpClient, DataMapper);
                        //result.MetaData.Success = result.MetaData.Status == QueryStatus.Success || result.MetaData.Status == QueryStatus.Running;
                        //result.MetaData.HttpStatusCode = response.StatusCode;
                        //Log.Trace("Received analytics query cid{0}: {1}", result.ClientContextId, result.ToString());

                        //scope.Span.SetPeerLatencyTag(result.Metrics.ElaspedTime);
                    }
                    //uri.ClearFailed();analytu
                }
                catch (OperationCanceledException e)
                {
                    //var operationContext = OperationContext.CreateAnalyticsContext(queryRequest.CurrentContextId, Context.BucketName, uri?.Authority);
                    //if (queryRequest is AnalyticsRequest request)
                    //{
                    //    operationContext.TimeoutMicroseconds = request.TimeoutValue;
                    //}

                    //Log.Info(operationContext.ToString());
                    ProcessError(e, result);
                }
                catch (HttpRequestException e)
                {
                    //Log.Info("Failed analytics query cid{0}: {1}", queryRequest.CurrentContextId, baseUri);
                    //uri.IncrementFailed();
                    ProcessError(e, result);
                    //Log.Error(e);
                }
                catch (AggregateException ae)
                {
                    ae.Flatten().Handle(e =>
                    {
                        //Log.Info("Failed analytics query cid{0}: {1}", queryRequest.CurrentContextId, baseUri);
                        //Log.Error(e);
                        ProcessError(e, result);
                        return true;
                    });
                }
                catch (Exception e)
                {
                    //Log.Info("Failed analytics query cid{0}: {1}", queryRequest.CurrentContextId, baseUri);
                    //Log.Info(e);
                    ProcessError(e, result);
                }
            }

            UpdateLastActivity();

            return result;
        }

        private static void ProcessError<T>(Exception exception, AnalyticsResult<T> queryResult)
        {
            queryResult.MetaData.Status = AnalyticsStatus.Fatal;
            //queryResult.MetaData.HttpStatusCode = HttpStatusCode.BadRequest;
        }
    }
}

#region [ License information          ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2017 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/

#endregion
