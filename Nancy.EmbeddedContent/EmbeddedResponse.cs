namespace Nancy.EmbeddedContent
{
    using Nancy.Helpers;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// The class responsible for returning responses from embedded content.
    /// </summary>
    public class EmbeddedResponse : Response
    {
        private static readonly byte[] ErrorText;

        private DateTime lastModifiedDate;

        static EmbeddedResponse()
        {
            ErrorText = Encoding.UTF8.GetBytes("NOT FOUND");
        }

        /// <summary>
        /// Creates an EmbeddedResponse
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded file with which to respond</param>
        /// <param name="resourcePath">The path to the embedded resource</param>
        /// <param name="name">The name of the resource</param>
        /// <param name="context">The Nancy context</param>
        public EmbeddedResponse(Assembly assembly, string resourcePath, string name, NancyContext context = null)
            : this(assembly, resourcePath, name, DateTime.UtcNow, context)
        { }

        /// <summary>
        /// Creates an EmbeddedResponse
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded file with which to respond</param>
        /// <param name="resourcePath">The path to the embedded resource</param>
        /// <param name="name">The name of the resource</param>
        /// <param name="lastModifiedDate">The date that will be compared against modified headers to determine if a NotModified response should be sent</param>
        /// <param name="context">The Nancy context</param>
        public EmbeddedResponse(Assembly assembly, string resourcePath, string name, DateTime lastModifiedDate, NancyContext context = null)
        {
            this.lastModifiedDate = lastModifiedDate;
            this.ContentType = MimeTypes.GetMimeType(name);
            this.StatusCode = HttpStatusCode.OK;

            var content = GetResourceContent(assembly, resourcePath, name);
            if (content != null)
            {
                var etag = GenerateETag(content);

                if (CacheHelpers.ReturnNotModified(etag, lastModifiedDate, context))
                {
                    this.StatusCode = HttpStatusCode.NotModified;
                    this.ContentType = null;
                    this.Contents = Response.NoBody;
                }
                else
                {
                    this.WithHeader("Last-Modified", this.lastModifiedDate.ToString("R"));
                    this.WithHeader("ETag", etag);
                    content.Seek(0, SeekOrigin.Begin);

                    if (context.Request.Headers.AcceptEncoding.Contains("gzip"))
                    {
                        this.WithHeader("Content-Encoding", "gzip");
                    }

                    this.Contents = stream =>
                    {
                        if (content != null)
                        {
                            if (context.Request.Headers.AcceptEncoding.Contains("gzip"))
                            {
                                using (GZipStream zs = new GZipStream(stream, CompressionMode.Compress, true))
                                {
                                    content.CopyTo(zs);
                                }
                            }
                            else
                            {
                                content.CopyTo(stream);
                            }
                        }
                        else
                        {
                            stream.Write(ErrorText, 0, ErrorText.Length);
                        }
                    };
                }
            }
            else
            {

            }
        }

        private Stream GetResourceContent(Assembly assembly, string resourcePath, string name)
        {
            var resourceName = assembly
                .GetManifestResourceNames()
                .Where(x => GetFileNameFromResourceName(resourcePath, x).Equals(name, StringComparison.OrdinalIgnoreCase))
                .Select(x => GetFileNameFromResourceName(resourcePath, x))
                .FirstOrDefault();

            resourceName =
                string.Concat(resourcePath, ".", resourceName);

            return assembly.GetManifestResourceStream(resourceName);
        }

        private static string GetFileNameFromResourceName(string resourcePath, string resourceName)
        {
            return resourceName.Replace(resourcePath, string.Empty).Substring(1);
        }

        private static string GenerateETag(Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(stream);
                return string.Concat("\"", ByteArrayToString(hash), "\"");
            }
        }

        private static string ByteArrayToString(byte[] data)
        {
            var output = new StringBuilder(data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                output.Append(data[i].ToString("X2"));
            }

            return output.ToString();
        }
    }
}
