﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DapperExtensions;

namespace Orchard.Data.Dapper.Extensions
{
    internal static class SortingExtensions
    {
        public static List<ISort> ToSortable<T>(this Expression<Func<T, object>>[] sortingExpression, bool ascending = true)
        {

            var sortList = new List<ISort>();
            sortingExpression.ToList().ForEach(sortExpression =>
            {
                MemberInfo sortProperty = ReflectionHelper.GetProperty(sortExpression);
                sortList.Add(new Sort { Ascending = ascending, PropertyName = sortProperty.Name });
            });

            return sortList;
        }
    }
}
