using System;
using System.Collections.Generic;

namespace ProbetaControl.Models
{
    /// <summary>Which page of a list to load. Large tables are never loaded whole.</summary>
    public sealed class PageRequest
    {
        public const int DefaultSize = 100;
        public const int MaxSize = 500;

        public PageRequest(int offset = 0, int limit = DefaultSize)
        {
            Offset = Math.Max(0, offset);
            Limit = Math.Min(Math.Max(1, limit), MaxSize);
        }

        public int Offset { get; }

        public int Limit { get; }

        public static PageRequest First { get; } = new PageRequest();
    }

    public sealed class PagedResult<T>
    {
        public PagedResult(IReadOnlyList<T> items, int totalCount, PageRequest page)
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
        }

        public IReadOnlyList<T> Items { get; }

        /// <summary>Rows matching the filter, across all pages.</summary>
        public int TotalCount { get; }

        public PageRequest Page { get; }

        public bool HasMore => Page.Offset + Items.Count < TotalCount;
    }

    /// <summary>Common list filter: free text plus whether archived rows are included.</summary>
    public class ListQuery
    {
        public string Text { get; set; }

        public bool IncludeArchived { get; set; }

        public PageRequest Page { get; set; } = PageRequest.First;
    }

    public sealed class CompressionTestQuery : ListQuery
    {
        public int? ClientId { get; set; }

        public DateTime? DateFrom { get; set; }

        public DateTime? DateTo { get; set; }

        /// <summary>Only tests with untested specimens whose rupture date is today or earlier.</summary>
        public bool OnlyDue { get; set; }
    }

    /// <summary>Specimen filter inside one test (legacy "Filtrar probetas"): mold dates and/or ID ranges.</summary>
    public sealed class SpecimenFilter
    {
        public DateTime? MoldDateFrom { get; set; }

        public DateTime? MoldDateTo { get; set; }

        /// <summary>Inclusive ID ranges, e.g. from "1-5, 8".</summary>
        public IReadOnlyList<IdRange> IdRanges { get; set; } = Array.Empty<IdRange>();

        public bool IncludeArchived { get; set; }

        public static SpecimenFilter None { get; } = new SpecimenFilter();
    }

    public struct IdRange
    {
        public IdRange(int from, int to)
        {
            From = Math.Min(from, to);
            To = Math.Max(from, to);
        }

        public int From { get; }

        public int To { get; }
    }
}
