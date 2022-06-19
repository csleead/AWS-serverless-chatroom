using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwsServerlessChatroom.Utils;

namespace AwsServerlessChatroom.DataAccess;
public record Message(Guid ChannelId, long Sequence, string Content, string FromConnection, DateTimeOffset Time) : RecordWithValidation
{
    protected override void Validate()
    {
        if (string.IsNullOrEmpty(Content))
        {
            throw new ArgumentException("Content cannot be null or empty");
        }
        if (string.IsNullOrEmpty(FromConnection))
        {
            throw new ArgumentException("FromConnection cannot be null or empty");
        }
    }
}
