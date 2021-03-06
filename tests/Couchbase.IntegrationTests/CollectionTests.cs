using System;
using System.Threading.Tasks;
using Couchbase.Core.Exceptions;
using Couchbase.Core.Exceptions.KeyValue;
using Couchbase.IntegrationTests.Fixtures;
using Couchbase.KeyValue;
using Couchbase.Management.Collections;
using Xunit;

namespace Couchbase.IntegrationTests
{
    public class CollectionTests : IClassFixture<ClusterFixture>
    {
        private readonly ClusterFixture _fixture;

        public CollectionTests(ClusterFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Test_Collection_Exists()
        {
            var bucket = await _fixture.Cluster.BucketAsync("default").ConfigureAwait(false);
            var collectionManager = (CollectionManager)bucket.Collections;

            const string scopeName = "my_scope", collectionName = "my_collection";
            var scopeSpec = new ScopeSpec(scopeName);
            var collectionSpec = new CollectionSpec(scopeName, collectionName);

            try
            {
                // create scope
                //await collectionManager.CreateScopeAsync(scopeSpec);

                // create collection
                // await collectionManager.CreateCollectionAsync(collectionSpec);

                var collection = bucket.Scope(scopeName).Collection(collectionName);
                var result = await collection.UpsertAsync("key3", new { });

                var result2 = await collection.UpsertAsync("key3", new { boo="bee"}, new UpsertOptions().Expiry(TimeSpan.FromMilliseconds(100000)));


            }
            catch
            {
                // ???
            }
            finally
            {
                // drop collection
                //await collectionManager.DropCollectionAsync(collectionSpec);
                //await collectionManager.DropScopeAsync(scopeName);
            }
        }

        [Fact]
        public async Task InsertByteArray_DefaultConverter_UnsupportedException()
        {
            const string key = nameof(InsertByteArray_DefaultConverter_UnsupportedException);

            var bucket = await _fixture.Cluster.BucketAsync("default").ConfigureAwait(false);
            var collection = bucket.DefaultCollection();

            try
            {
                await Assert.ThrowsAsync<UnsupportedException>(
                    () => collection.InsertAsync(key, new byte[] { 1, 2, 3 }));
            }
            finally
            {
                try
                {
                    await collection.RemoveAsync(key);
                }
                catch (DocumentNotFoundException)
                {
                }
            }
        }

        [Fact]
        public async Task CollectionIdChanged_RetriesAutomatically()
        {
            const string scopeName = "CollectionIdChanged";
            const string collectionName = "coll";
            const string key = nameof(CollectionIdChanged_RetriesAutomatically);

            var bucket = await _fixture.GetDefaultBucket().ConfigureAwait(false);
            var collectionManager = bucket.Collections;

            try
            {
                await collectionManager.CreateScopeAsync(new ScopeSpec(scopeName));
                await collectionManager.CreateCollectionAsync(new CollectionSpec(scopeName, collectionName));

                await Task.Delay(500);
                await ((CouchbaseBucket) bucket).RefreshManifestAsync();

                ICouchbaseCollection collection = bucket.Scope(scopeName).Collection(collectionName);

                await collection.UpsertAsync(key, new {name = "mike"}).ConfigureAwait(false);

                await collectionManager.DropCollectionAsync(new CollectionSpec(scopeName, collectionName));
                await collectionManager.CreateCollectionAsync(new CollectionSpec(scopeName, collectionName));

                await Task.Delay(500);

                await collection.UpsertAsync(key, new {name = "mike"}).ConfigureAwait(false);
            }
            finally
            {
                await collectionManager.DropScopeAsync(scopeName);
            }
        }
    }
}
