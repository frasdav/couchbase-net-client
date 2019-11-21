namespace Couchbase.Management.Views
{
    public class DesignDocumentAlreadyExistsException : CouchbaseException
    {
        public DesignDocumentAlreadyExistsException(string bucketName, string viewName)
            : base($"Design document already exist {bucketName}/{viewName}")
        {

        }
    }
}