using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwsServerlessChatroom;

namespace AwsServerlessChatroom.DataAccess;
public record Channel(Guid Id, string Name) : RecordWithValidation
{
    protected override void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("Name is required");
        }
    }
}
