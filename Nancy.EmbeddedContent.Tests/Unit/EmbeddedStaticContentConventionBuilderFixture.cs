namespace Nancy.EmbeddedContent.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Conventions;
    using Nancy.Tests;
    using Responses;
    using Xunit;
    using System.Globalization;

    public class EmbeddedStaticContentConventionBuilderFixture
    {
        [Fact]
        public void Return_not_modified_when_not_changed_and_conditional_request_on_etag_sent()
        {
            var result = GetEmbeddedStaticContentResponse("Foo", "Subfolder/embedded2.txt");
            var etag = result.Headers["ETag"];
            var headers = new Dictionary<string, IEnumerable<string>> { { "If-None-Match", new[] { etag } } };

            result = GetEmbeddedStaticContentResponse("Foo", "Subfolder/embedded2.txt", headers: headers);

            result.StatusCode.ShouldEqual(HttpStatusCode.NotModified);
        }

        [Fact]
        public void Return_not_modified_when_resource_not_changed_and_conditional_request_on_etag_sent()
        {
            var initialResult = GetEmbeddedStaticContentResponse("Foo", "Subfolder/embedded2.txt");
            var etag = initialResult.Headers["ETag"];
            var headers = new Dictionary<string, IEnumerable<string>> { { "If-None-Match", new[] { etag } } };

            var result = GetEmbeddedStaticContentResponse("Foo", "Subfolder/embedded2.txt", headers: headers);

            result.StatusCode.ShouldEqual(HttpStatusCode.NotModified);
        }

        [Fact]
        public void Return_not_modified_when_not_changed_and_conditional_request_on_modified_sent()
        {
            var initialResult = GetEmbeddedStaticContentResponse("Foo", "Subfolder/embedded2.txt");
            var moddedTime = initialResult.Headers["Last-Modified"];
            var headers = new Dictionary<string, IEnumerable<string>> { { "If-Modified-Since", new[] { moddedTime } } };

            var result = GetEmbeddedStaticContentResponse("Foo", "Subfolder/embedded2.txt", headers: headers);

            result.StatusCode.ShouldEqual(HttpStatusCode.NotModified);
        }

        [Fact]
        public void Return_full_response_when_changed_and_conditional_request_on_etag_sent()
        {
            var initialResult = GetEmbeddedStaticContentResponse("Foo", "Subfolder/embedded2.txt");
            var etag = initialResult.Headers["ETag"];
            var headers = new Dictionary<string, IEnumerable<string>> { { "If-None-Match", new[] { etag.Substring(1) } } };

            var result = GetEmbeddedStaticContentResponse("Foo", "Subfolder/embedded2.txt", headers: headers);

            result.StatusCode.ShouldEqual(HttpStatusCode.OK);
        }

        [Fact]
        public void Return_full_response_when_changed_and_conditional_request_on_modified_sent()
        {
            var initialResult = GetEmbeddedStaticContentResponse("Foo", "Subfolder/embedded2.txt");
            var moddedTimeString = initialResult.Headers["Last-Modified"];
            var moddedTime = DateTime.ParseExact(moddedTimeString, "R", CultureInfo.InvariantCulture, DateTimeStyles.None)
                                     .AddHours(-1);
            moddedTimeString = moddedTime.ToString("R", CultureInfo.InvariantCulture);
            var headers = new Dictionary<string, IEnumerable<string>> { { "If-Modified-Since", new[] { moddedTimeString } } };

            var result = GetEmbeddedStaticContentResponse("Foo", "Subfolder/embedded2.txt", headers: headers);

            result.StatusCode.ShouldEqual(HttpStatusCode.OK);
        }

        [Fact]
        public void Return_full_response_when_etag_matches_but_date_does_not()
        {
            var initialResult = GetEmbeddedStaticContentResponse("Foo", "Subfolder/embedded2.txt");
            var moddedTimeString = initialResult.Headers["Last-Modified"];
            var etag = initialResult.Headers["ETag"];

            var moddedTime = DateTime.ParseExact(moddedTimeString, "R", CultureInfo.InvariantCulture, DateTimeStyles.None)
                                     .AddHours(-1);
            moddedTimeString = moddedTime.ToString("R", CultureInfo.InvariantCulture);
            var headers = new Dictionary<string, IEnumerable<string>> { { "If-Modified-Since", new[] { moddedTimeString } }, { "If-None-Match", new[] { etag.Substring(1) } } };

            var result = GetEmbeddedStaticContentResponse("Foo", "Subfolder/embedded2.txt", headers: headers);

            result.StatusCode.ShouldEqual(HttpStatusCode.OK);
        }

        [Fact]
        public void Not_modified_response_has_no_body()
        {
            var initialResult = GetEmbeddedStaticContentResponse("Foo", "Subfolder/embedded2.txt");
            var etag = initialResult.Headers["ETag"];
            var headers = new Dictionary<string, IEnumerable<string>> { { "If-None-Match", new[] { etag } } };

            var result = GetEmbeddedStaticContent("Foo", "Subfolder/embedded2.txt", headers: headers);

            result.ShouldEqual(string.Empty);
        }

        [Fact]
        public void Embedded_response_has_last_modified_header()
        {
            // Given
            // When
            var response = GetEmbeddedStaticContentResponse("Foo", "Subfolder/embedded2.txt");

            // Then
            Assert.NotNull(response);
            Assert.True(response.Headers.ContainsKey("Last-Modified"));
        }

        [Fact]
        public void Retrieves_static_content_with_urlencoded_dot()
        {
            // Given
            // When
            var result = GetEmbeddedStaticContent("Foo", "embedded%2etxt");

            // Then
            result.ShouldEqual("Embedded Text");
        }

        [Fact]
        public void Retrieves_static_content_in_subfolder()
        {
            // Given
            // When
            var result = GetEmbeddedStaticContent("Foo", "Subfolder/embedded2.txt");

            // Then
            result.ShouldEqual("Embedded2 Text");
        }

        [Fact]
        public void Retrieves_static_content_with_hyphens_in_subfolder()
        {
            // Given
            // When
            var result = GetEmbeddedStaticContent("Foo", "Subfolder-with-hyphen/embedded3.txt");

            // Then
            result.ShouldEqual("Embedded3 Text");
        }

        [Fact]
        public void Retrieves_static_content_with_relative_path()
        {
            // Given
            // When
            var result = GetEmbeddedStaticContent("Foo", "Subfolder/../embedded.txt");

            // Then
            result.ShouldEqual("Embedded Text");
        }

        private static Response GetEmbeddedStaticContentResponse(string virtualDirectory, string requestedFilename, string root = null, IDictionary<string, IEnumerable<string>> headers = null)
        {
            var resource = string.Format("/{0}/{1}", virtualDirectory, requestedFilename);

            var context = GetContext(virtualDirectory, requestedFilename, headers);

            var assembly = Assembly.GetExecutingAssembly();

            var resolver = EmbeddedStaticContentConventionBuilder.AddDirectory(virtualDirectory, assembly, "Resources");

            return resolver.Invoke(context, null);
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