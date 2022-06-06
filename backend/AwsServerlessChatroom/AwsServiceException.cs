using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsServerlessChatroom;
internal class AwsServiceException : Exception
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used")]
    private static string FormatErrorMessage(AmazonWebServiceResponse serviceResponse, string? message)
    {
        var sb = new StringBuilder();
        sb.AppendLine(message ?? "Something went wrong when calling an AWS service API");
        sb.AppendLine($"Response status: {serviceResponse.HttpStatusCode}");
        sb.AppendLine($"Request Id: '{serviceResponse.ResponseMetadata.RequestId}'");

        sb.AppendLine($"Response metadata: '{serviceResponse.ResponseMetadata.RequestId}'");
        foreach (var (key, value) in serviceResponse.ResponseMetadata.Metadata)
        {
            sb.Append($"'{key}': '{value}'");
        }

        return sb.ToString();
    }

    public AwsServiceException(AmazonWebServiceResponse serviceResponse, string? message) : base(FormatErrorMessage(serviceResponse, message))
    {

    }
}
