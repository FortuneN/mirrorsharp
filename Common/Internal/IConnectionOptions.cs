using MirrorSharp.Advanced;

namespace MirrorSharp.Internal {
    internal interface IConnectionOptions {
        bool IncludeExceptionDetails { get; set; }
        IExceptionLogger? ExceptionLogger { get; set; }
    }
}