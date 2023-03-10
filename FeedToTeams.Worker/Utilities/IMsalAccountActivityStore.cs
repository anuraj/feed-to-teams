using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FeedToTeams.Worker.Utilities;
public interface IMsalAccountActivityStore
{
    Task UpsertMsalAccountActivity(MsalAccountActivity msalAccountActivity);

    Task<IEnumerable<MsalAccountActivity>> GetMsalAccountActivitesSince(DateTime lastActivityDate);

    Task<MsalAccountActivity> GetMsalAccountActivityForUser(string userPrincipalName);

    Task HandleIntegratedTokenAcquisitionFailure(MsalAccountActivity failedAccountActivity);
}
