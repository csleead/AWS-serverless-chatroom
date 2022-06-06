using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsServerlessChatroom;
internal static class ServiceResponseUtils
{
    public static bool IsSuccess(this AmazonWebServiceResponse response) =>
        (int)response.HttpStatusCode is >= 200 and <= 299;
}
