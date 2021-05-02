using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Metrics
{
    public class DiskMetricsResponse : BaseResponse
    {
        public bool Full { get; set; }
        public string Allocated { get; set; }
        public string Available { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        private DiskMetricsResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public DiskMetricsResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="allocated"></param>
        /// <param name="available"></param>
        public DiskMetricsResponse(bool full, string allocated, string available) : base(ResponseCode.Success)
        {
            Full = full;
            Allocated = allocated;
            Available = available;
        }
    }
}
