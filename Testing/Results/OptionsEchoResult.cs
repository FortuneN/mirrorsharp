using System.Collections.Generic;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace MirrorSharp.Testing.Results {
    public class OptionsEchoResult {
        public IDictionary<string, string> Options { get; } = new Dictionary<string, string>();
    }
}
