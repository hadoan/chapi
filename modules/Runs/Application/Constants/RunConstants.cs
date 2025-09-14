namespace Runs.Application.Constants;

/// <summary>
/// Constants used throughout the Runs module for test execution and file handling
/// </summary>
public static class RunConstants
{
    /// <summary>
    /// File storage and artifact constants
    /// </summary>
    public static class Storage
    {
        /// <summary>
        /// The container/bucket name used for storing run artifacts and test files
        /// </summary>
        public const string ArtifactsContainer = "chapi-artifacts";

        /// <summary>
        /// Test definition file name within RunPacks
        /// </summary>
        public const string TestsFileName = "tests.json";

        /// <summary>
        /// Summary file name for run results
        /// </summary>
        public const string SummaryFileName = "summary.json";

        /// <summary>
        /// Request artifact file suffix
        /// </summary>
        public const string RequestFileSuffix = "-request.json";

        /// <summary>
        /// Response artifact file suffix
        /// </summary>
        public const string ResponseFileSuffix = "-response.json";
    }

    /// <summary>
    /// URI schemes and protocols
    /// </summary>
    public static class Schemes
    {
        /// <summary>
        /// URI scheme for referencing RunPack files
        /// </summary>
        public const string RunPackScheme = "runpack://";
    }

    /// <summary>
    /// HTTP client and request constants
    /// </summary>
    public static class Http
    {
        /// <summary>
        /// Named HTTP client for test execution
        /// </summary>
        public const string ExecutorClientName = "runs-executor";

        /// <summary>
        /// Content-Type header name
        /// </summary>
        public const string ContentTypeHeader = "content-type";

        /// <summary>
        /// Default content type for JSON payloads
        /// </summary>
        public const string JsonContentType = "application/json";

        /// <summary>
        /// Default HTTP method for tests
        /// </summary>
        public const string DefaultMethod = "GET";

        /// <summary>
        /// Default expected status code pattern (2xx)
        /// </summary>
        public const string DefaultExpectedStatus = "2xx";

        /// <summary>
        /// Status code wildcard suffix pattern
        /// </summary>
        public const string StatusWildcardSuffix = "xx";
    }

    /// <summary>
    /// Path templates for file organization
    /// </summary>
    public static class Paths
    {
        /// <summary>
        /// Template for run artifact directory path
        /// Format: runs/{runId}/artifacts/{testIndex:000}-{sanitizedTestName}
        /// </summary>
        public const string ArtifactPrefixTemplate = "runs/{0}/artifacts/{1:000}-{2}";

        /// <summary>
        /// Template for run summary file path
        /// Format: runs/{runId}/summary.json
        /// </summary>
        public const string SummaryPathTemplate = "runs/{0}/summary.json";

        /// <summary>
        /// Template for run IR suite file path
        /// Format: runs/{runId}/suite.json
        /// </summary>
        public const string SuitePathTemplate = "runs/{0}/suite.json";
    }

    /// <summary>
    /// Error messages and status descriptions
    /// </summary>
    public static class Messages
    {
        /// <summary>
        /// Error message when RunPack is not found
        /// </summary>
        public const string RunPackNotFoundMessage = "RunPack with ID {0} not found";

        /// <summary>
        /// Error message when tests.json file is missing from RunPack
        /// </summary>
        public const string TestsFileNotFoundMessage = "No tests.json file found in the RunPack";

        /// <summary>
        /// Error message when IR deserialization fails
        /// </summary>
        public const string InvalidIrMessage = "Invalid IR: null";

        /// <summary>
        /// Status message template for failed tests
        /// </summary>
        public const string TestsFailedMessage = "One or more tests failed ({0}).";
    }

    /// <summary>
    /// Character used to replace invalid file name characters
    /// </summary>
    public static class FileSystem
    {
        /// <summary>
        /// Replacement character for invalid file name characters
        /// </summary>
        public const char InvalidCharReplacement = '-';
    }
}