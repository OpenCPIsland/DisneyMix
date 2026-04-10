using System;
using System.Linq;
using Disney.Mix.SDK.Internal.MixDomain;

namespace Disney.Mix.SDK.Internal
{
    public static class OfficialAccountGetter
    {
        public static void GetAllOfficialAccounts(
            AbstractLogger logger,
            IUserDatabase userDatabase,
            IMixWebCallFactory mixWebCallFactory,
            Action<OfficialAccountsResponse> successCallback,
            Action failureCallback)
        {
            try
            {
                BaseUserRequest request = new BaseUserRequest();
                IWebCall<BaseUserRequest, OfficialAccountsResponse> webCall = mixWebCallFactory.OfficialAccountAllPost(request);

                webCall.OnResponse += (sender, e) =>
                {
                    OfficialAccountsResponse response = e.Response;
                    if (ValidateResponse(response))
                    {
                        // update the db with the retrieved OA-IDs
                        userDatabase.InsertOrUpdateOfficialAccounts(response.OfficialAccountIds);
                        successCallback(response);
                    }
                    else
                    {
                        // enhanced logging to see what actually failed (if it even fails)
                        logger.Critical($"Failed to validate official accounts response: {JsonParser.ToJson(response)}");
                        failureCallback();
                    }
                };

                webCall.OnError += (sender, e) => failureCallback();

                webCall.Execute();
            }
            catch (Exception ex)
            {
                logger.Critical($"Unhandled exception in GetAllOfficialAccounts: {ex}");
                failureCallback();
            }
        }

        private static bool ValidateResponse(OfficialAccountsResponse response)
        {
            // ensure the response and the list itself isnt null
            if (response?.OfficialAccountIds == null)
            {
                return false;
            }

            // validate that EVERY account in the list is properly formed
            // all() should return true if every element meets the conditions required
            return response.OfficialAccountIds.All(oa =>
                oa != null &&
                oa.OaId != null &&
                oa.OaName != null &&
                oa.IsAvailable.HasValue &&
                oa.CanUnfollow.HasValue
            );
        }
    }
}