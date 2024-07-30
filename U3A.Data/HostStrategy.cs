// Copyright Finbuckle LLC, Andrew White, and Contributors.
// Refer to the solution LICENSE file for more information.

using Microsoft.AspNetCore.Http;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace U3A.Database
{
    public class HostStrategy
    {
        public string GetIdentifier(String Host)
        {
            string? identifier = null;
            // split on the dots
            char[] delimiters = ".".ToCharArray();
            var splits = Host.Split(delimiters);
            identifier = splits[0];
            return identifier;
        }
    }
}