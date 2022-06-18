using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwsServerlessChatroom;

namespace AwsServerlessChatroom.Utils;
public abstract record RecordWithValidation
{
    protected RecordWithValidation()
    {
        Validate();
    }

    protected virtual void Validate()
    {
    }
}
