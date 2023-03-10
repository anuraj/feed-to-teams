using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeedToTeams.Frontend.Models;

namespace FeedToTeams.Frontend.Utilities;
public interface IMsalAccountActivityStore
{
    Task UpsertMsalAccountActivity(MsalAccountActivityModel msalAccountActivity);

    Task<IEnumerable<MsalAccountActivityModel>> GetMsalAccountActivitesSince(DateTime lastActivityDate);

    Task<MsalAccountActivityModel> GetMsalAccountActivityForUser(string userPrincipalName);

    Task HandleIntegratedTokenAcquisitionFailure(MsalAccountActivityModel failedAccountActivity);
}
