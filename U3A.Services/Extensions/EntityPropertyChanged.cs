using Eway.Rapid.Abstractions.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;

namespace U3A.Services
{
    public static partial class Extensions
    {
        public static (object originalValue,object newValue)?  EntityPropertyChanges(this object objSource, U3ADbContext dbc, string PropertyName)
        {
            (object originalValue,object newValue)? result = null;
            if (objSource != null)
            {
                var entry = dbc.Entry(objSource);
                var modified = entry.Properties
                        .Where(prop => prop.Metadata.Name.ToLower() == PropertyName.ToLower())
                        .FirstOrDefault();
                if (modified != null)
                {
                    result = new() { 
                        newValue = modified.CurrentValue, 
                        originalValue = modified.OriginalValue 
                    };
                }
            }
            return result;
        }
    }
}
