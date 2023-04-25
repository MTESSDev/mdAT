# Output_expected

> Test output expected

## Case 1

Description

``````yaml
form:
  this: is
  a: test
byte: VW5pY29kZQ==
expected:
  generateExpectedData: ./Tests/Generated/output-expected.yml
  verify:
  - type: match
    allowAdditionalProperties: false
    data: {"form":{"this":"is","a":"test"},"bytes":"VW5pY29kZQ=="}
``````