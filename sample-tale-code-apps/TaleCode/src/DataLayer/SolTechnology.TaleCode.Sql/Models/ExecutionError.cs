using System;
using System.Collections.Generic;

namespace SolTechnology.TaleCode.SqlData.Models
{
    public partial class ExecutionError
    {
        public int Id { get; set; }
        public string ReferenceType { get; set; }
        public int ReferenceId { get; set; }
        public string Message { get; set; }
        public bool Valid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
