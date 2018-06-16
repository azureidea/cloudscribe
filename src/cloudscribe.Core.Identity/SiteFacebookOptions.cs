﻿// Copyright (c) Source Tree Solutions, LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Author:					Joe Audette
// Created:					2017-07-27
// Last Modified:			2018-06-06
// 

using cloudscribe.Core.Models;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace cloudscribe.Core.Identity
{
    public class SiteFacebookOptions : OptionsMonitor<FacebookOptions>
    {
        public SiteFacebookOptions(
            IOptionsFactory<FacebookOptions> factory,
            IEnumerable<IOptionsChangeTokenSource<FacebookOptions>> sources,
            IOptionsMonitorCache<FacebookOptions> cache,
            IOptions<MultiTenantOptions> multiTenantOptionsAccessor,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SiteFacebookOptions> logger
            ) : base(factory, sources, cache)
        {
            _multiTenantOptions = multiTenantOptionsAccessor.Value;
            _httpContextAccessor = httpContextAccessor;
            _factory = factory;
            _cache = cache;
            _log = logger;
           
        }

        private readonly IOptionsMonitorCache<FacebookOptions> _cache;
        private readonly IOptionsFactory<FacebookOptions> _factory;
        private readonly MultiTenantOptions _multiTenantOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger _log;
       
        public override FacebookOptions Get(string name)
        {
            var tenant = _httpContextAccessor.HttpContext.GetTenant<SiteContext>();
            var resolvedName = ResolveName(tenant, name);
            return _cache.GetOrAdd(resolvedName, () => CreateOptions(resolvedName, tenant));

        }

        private FacebookOptions CreateOptions(string name, SiteContext tenant)
        {
            var options = _factory.Create(name);
            options.AppId = "placeholder";
            options.AppSecret = "placeholder";
            ConfigureTenantOptions(tenant, options);

            return options;
        }

        private void ConfigureTenantOptions(SiteContext tenant, FacebookOptions options)
        {
            if (tenant == null)
            {
                _log.LogError("tenant was null");
                return;
            }
            var useFolder = !_multiTenantOptions.UseRelatedSitesMode
                                        && _multiTenantOptions.Mode == cloudscribe.Core.Models.MultiTenantMode.FolderName
                                        && !string.IsNullOrWhiteSpace(tenant.SiteFolderName);

            if (!string.IsNullOrWhiteSpace(tenant.FacebookAppId))
            {
                options.AppId = tenant.FacebookAppId;
                options.AppSecret = tenant.FacebookAppSecret;
                options.SignInScheme = IdentityConstants.ExternalScheme;

                if (useFolder)
                {
                    options.CallbackPath = "/" + tenant.SiteFolderName + "/signin-facebook";
                }
            }
        }

        private string ResolveName(SiteContext tenant, string name)
        {
            if (tenant == null)
            {
                _log.LogError("tenant was null");
                return name;
            }

            if (_multiTenantOptions.UseRelatedSitesMode)
            {
                return name;
            }

            return $"{name}-{tenant.SiteFolderName}";
        }

    }

}