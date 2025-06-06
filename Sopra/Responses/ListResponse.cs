﻿using System.Collections.Generic;
using System.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sopra.Responses
{
    public class ListResponse<T>
    {
        public IEnumerable<T> Data { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }

        public ListResponse(IEnumerable<T> data, int total, int page)
        {
            Data = data;
            Total = total;
            Page = page;
        }
    }

	public class ListResponseProduct<T>
	{
		public IEnumerable<T> Data { get; set; }
		public int Total { get; set; }
		public int Page { get; set; }
		public IEnumerable<FilterGroup> Filters { get; set; }

		public ListResponseProduct(IEnumerable<T> data, int total, int page, IEnumerable<FilterGroup> filters)
		{
			Data = data;
			Total = total;
			Page = page;
			Filters = filters;
		}
	}

	public class ListResponseFilter<T>
	{
		public IEnumerable<FilterGroup> Filters { get; set; }

		public ListResponseFilter( IEnumerable<FilterGroup> filters)
		{
			Filters = filters;
		}


	}
	public class FilterInfo
	{
		public long? ID  { get; set; }
		public string? Name { get; set; }
		public decimal? Value { get; set; }
		public decimal? MinValue { get; set; }
		public decimal? MaxValue { get; set; }
		public long? Count { get; set; }

	}

	public class FilterGroup
	{
		public string GroupName { get; set; }
		public List<FilterInfo> Filter { get; set; }
	}
}
