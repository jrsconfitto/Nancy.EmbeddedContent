using Nancy.Tests;
using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xunit;
using System.Reflection;
using Nancy.EmbeddedContent.Conventions;

namespace Nancy.Embedded.Tests.Unit
{
    public class EmbeddedResponseTests
    {
        [Fact]
        public void Returns_gzipped_content_when_gzip_is_accepted()
        {
            // Given
            var headers = new Dictionary<string, IEnumerable<string>> { { "Accept-Encoding", new[] { "gzip" } } };

            // When
            var response = GetEmbeddedStaticContentResponse("Foo", "embedded.txt", headers: headers);

            // Then
            response.StatusCode.ShouldEqual(HttpStatusCode.OK);
            response.Headers.ContainsKey("Content-Encoding").ShouldBeTrue();

            using (var stream = new MemoryStream())
            {
                response.Contents(stream);

                // Streams streams, all the way down
                using (MemoryStream output = new MemoryStream())
                using (MemoryStream ms = new MemoryStream(stream.GetBuffer()))
                using (GZipStream zs = new GZipStream(ms, CompressionMode.Decompress))
                {
                    zs.CopyTo(output);
                    var content = Encoding.UTF8.GetString(output.GetBuffer(), 0, (int)output.Length);
                    content.ShouldEqual("Embedded Text");
                }
            }
        }

        private static string GetEmbeddedStaticContent(string virtualDirectory, string requestedFilename, string root = null, IDictionary<string, IEnumerable<string>> headers = null)
        {
            var response = GetEmbeddedStaticContentResponse(virtualDirectory, requestedFilename, root, headers);

            if (response != null)
            {
                using (var stream = new MemoryStream())
                {
                    response.Contents(stream);
                    return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
                }
            }

            return null;
        }

        private static Response GetEmbeddedStaticContentResponse(string virtualDirectory, string requestedFilename, string root = null, IDictionary<string, IEnumerable<string>> headers = null)
        {
            var resource = string.Format("/{0}/{1}", virtualDirectory, requestedFilename);

            var context = GetContext(virtualDirectory, requestedFilename, headers);

            var assembly = Assembly.GetExecutingAssembly();

            var resolver = EmbeddedStaticContentConventionBuilder.AddDirectory(virtualDirectory, assembly, "Resources");

            return resolver.Invoke(context, null);
        }

        private static NancyContext GetContext(string virtualDirectory, string requestedFilename, IDictionary<string, IEnumerable<string>> headers = null)
        {
            var resource = string.Format("/{0}/{1}", virtualDirectory, requestedFilename);

            var request = new Request(
                "GET",
                new Url { Path = resource, Scheme = "http" },
                headers: headers ?? new Dictionary<string, IEnumerable<string>>());

            var context = new NancyContext { Request = request };
            return context;
        }
    }
}
