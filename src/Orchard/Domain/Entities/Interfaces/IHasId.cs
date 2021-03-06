//Copyright (c) Orchard, Inc. All Rights Reserved.
//License: https://raw.github.com/Orchard/Orchard/master/license.txt

namespace Orchard.Domain.Entities
{
    public interface IHasId<T>
    {
        T Id { get; }
    }
}