using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBookStore.Application.Common.Utility
{
    public class ExceptionDetails
    {
        //public string? controllerName { get; set; }
        //public string? actionName { get; set; }
        public string? exceptionMessage { get; set; }
        //public string? stackTrace { get; set; }
        //public string? Source { get; set; }
        //public string? InnerExcepton { get; set; }
        //public string? HResult { get; set; }
        public int? StatusCode { get; set; }
    }
}
