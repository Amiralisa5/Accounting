using System;
using System.Collections.Generic;
using System.Linq;
using BigBang.Common;
using BigBang.Metadata.Models;

namespace BigBang.App.Cloud.ERP.Accounting.Common.Helpers
{
    internal static class EntityHelper
    {
        public static Dictionary<string, string> GetProperties<TAggregate>(TAggregate aggregate) where TAggregate : IEntity, new()
        {
            var dictionary = new Dictionary<string, string>();

            if (aggregate is null)
                return dictionary;

            var properties = typeof(TAggregate).GetProperties()
                .Where(property => property.PropertyType != typeof(EntityState) &&
                    (property.PropertyType.IsPrimitive ||
                     property.PropertyType == typeof(string) ||
                     property.PropertyType == typeof(Guid) ||
                     property.PropertyType.BaseType == typeof(Enum)));
            foreach (var property in properties)
            {
                var propertyName = property.Name.ToCamelCase();
                var propertyValue = Convert.ToString(property.GetValue(aggregate));
                dictionary[propertyName] = propertyValue;
            }

            return dictionary;
        }
    }
}
