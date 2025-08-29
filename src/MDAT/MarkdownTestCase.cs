using Xunit.Abstractions;
using Xunit.Sdk;

namespace MDAT
{
    public sealed class MarkdownTestCase : XunitTestCase
    {
        string? _customDisplayName;

        public MarkdownTestCase() { }

        public MarkdownTestCase(
            IMessageSink diagnosticMessageSink,
            TestMethodDisplay defaultMethodDisplay,
            TestMethodDisplayOptions defaultMethodDisplayOptions,
            ITestMethod testMethod,
            string displayName,
            object?[]? testMethodArguments)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
        {
            _customDisplayName = displayName;
        }

        protected override string GetDisplayName(IAttributeInfo factAttribute, string displayName)
        {
            return _customDisplayName ?? base.DisplayName;
        }

        public override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);
            data.AddValue("MDAT_DisplayName", _customDisplayName);
        }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            base.Deserialize(data);
            _customDisplayName = data.GetValue<string>("MDAT_DisplayName");
        }
    }
}
