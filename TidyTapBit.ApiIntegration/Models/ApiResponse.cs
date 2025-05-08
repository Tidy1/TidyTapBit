using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TidyTrader.ApiIntegration.Models
{
    public class ApiResponse<T>
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccessStatusCode => ((int)StatusCode >= 200 && (int)StatusCode < 300);
        public string ErrorMessage { get; set; }
        public Exception ErrorException { get; set; }
        public T Data { get; set; }
    }
}
