﻿using System;
using System.Collections.Specialized;
using Nancy;
using WorldDomination.Web.Authentication;
using WorldDomination.Web.Authentication.Facebook;
using WorldDomination.Web.Authentication.Google;
using WorldDomination.Web.Authentication.Twitter;
using WorldDomination.Web.IntegrationTest.NancyFX.Model;

namespace WorldDomination.Web.IntegrationTest.NancyFX.Modules
{
    public class HomeModule : NancyModule
    {
      
        private const string SessionGuidKey = "GUIDKey";

        public HomeModule(IAuthenticationService authenticationService)
        {
           

            Get["/"] = parameters => View["login"];

            Get["/RedirectToAuthenticate/{providerKey}"] = parameters =>
                                                           {
                                                               // State key.
                                                               Session[SessionGuidKey] = Guid.NewGuid();

                                                               // TODO: What happens if an invalid providerKey is provided?
                                                               var authenticationServiceSettings = AuthenticationServiceSettingsFactory.
                                                                   GetAuthenticateServiceSettings(parameters.providerKey);
                                                               authenticationServiceSettings.State =
                                                                   Session[SessionGuidKey].ToString();
                                                               
                                                               Uri uri =
                                                                   authenticationService.
                                                                       RedirectToAuthenticationProvider(authenticationServiceSettings);
                                                               
                                                               return Response.AsRedirect(uri.AbsoluteUri);
                                                           };


            Get["/AuthenticateCallback"] = parameters =>
                                           {
                                               if (string.IsNullOrEmpty(Request.Query.providerKey))
                                               {
                                                   throw new ArgumentNullException("providerKey");
                                               }

                                               // It's possible that a person might hit this resource directly, before any session value
                                               // has been set. As such, we should just fake some state up, which will not match the
                                               // CSRF check.
                                               var existingState = (Guid)(Session[SessionGuidKey] ?? Guid.NewGuid());

                                               var model = new AuthenticateCallbackViewModel();

                                               var querystringParameters = new NameValueCollection();
                                               foreach (var item in Request.Query)
                                               {
                                                   querystringParameters.Add(item, Request.Query[item]);
                                               }

                                               try
                                               {
                                                   model.AuthenticatedClient =
                                                       authenticationService.CheckCallback(Request.Query.providerKey,
                                                                                           querystringParameters,
                                                                                           existingState.ToString());
                                               }
                                               catch (Exception exception)
                                               {
                                                   model.Exception = exception;
                                               }


                                               return View["AuthenticateCallback", model];
                                           };
        }
    }
}