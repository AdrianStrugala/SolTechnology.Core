using System;

namespace DreamTravel.Domain
{
    public abstract record AbstractEntity
    {
        public long Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ModifiedAt { get; set; }

        private bool MarkedToDeletion { get; set; }
    }
}
