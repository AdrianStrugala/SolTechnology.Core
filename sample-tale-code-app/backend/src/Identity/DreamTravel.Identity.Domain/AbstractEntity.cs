using System;

namespace DreamTravel.Identity.Domain
{
    public abstract record AbstractEntity
    {
        public int Id { get; }

        public DateTime CreatedAt { get; }

        public DateTime ModifiedAt { get; }

        public bool MarkedToDeletion { get; private set; }
    }
}
