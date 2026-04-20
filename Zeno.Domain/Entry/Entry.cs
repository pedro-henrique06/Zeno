using System;
using System.Collections.Generic;
using System.Text;
using Zeno.Domain.Enum;

namespace Zeno.Domain.Entry
{
    public class Entry
    {
        public Guid? Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public decimal Value { get; set; }

        public EntryType Type  { get; set; }

        public string Description { get; set; } = string.Empty;

        public Category Category { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public Guid? WalletId { get; set; }

        public Entry() { }
    }
}
