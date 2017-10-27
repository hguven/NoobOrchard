﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Orchard {
    public static class ReadOnlyCollectionExtensions {
        public static IList<T> ToReadOnlyCollection<T>(this IEnumerable<T> enumerable) {
            return new ReadOnlyCollection<T>(enumerable.ToList());
        }
    }
}